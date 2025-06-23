using ElectronicDiaryWeb.Controllers;
using ElectronicDiaryWeb.Models.Auth;
using ElectronicDiaryWeb.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;

[Route("Register")]
public class RegisterController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<HomeController> _logger;

    public RegisterController(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<HomeController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    //[Authorize(Roles = "администратор")]
    [HttpGet("RegisterEmployee")]
    public async Task<IActionResult> RegisterEmployee()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["_secure_at"]);

            var response = await client.GetAsync($"{_config["ApiBaseUrl"]}/api/users/posts");

            if (response.IsSuccessStatusCode)
            {
                var posts = await response.Content.ReadFromJsonAsync<List<PostDto>>();
                ViewBag.Posts = posts?.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                }) ?? new List<SelectListItem>();
            }
            else
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка загрузки должностей: {response.StatusCode}");
                ViewBag.Posts = new List<SelectListItem>();
            }
        }
        catch (Exception ex)
        {
            // Логирование исключения
            Console.WriteLine($"Исключение при загрузке должностей: {ex.Message}");
            ViewBag.Posts = new List<SelectListItem>();
        }

        return View("RegisterEmployee");
    }

    //[Authorize(Roles = "администратор")]
    [HttpPost("RegisterEmployee")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterEmployee(RegisterEmployeeModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["_secure_at"]);

        var response = await client.PostAsJsonAsync($"{_config["ApiBaseUrl"]}/api/users/register-employee", new
        {
            model.Login,
            model.Password,
            model.Name,
            model.Surname,
            model.Patronymic,
            BirthDate = DateOnly.FromDateTime(model.BirthDate),
            model.Phone,
            model.Role,
            PostId = model.PostId
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Ошибка при регистрации сотрудника");
            return View(model);
        }

        return RedirectToAction("Index", "Users");
    }

    //[Authorize(Roles = "администратор")]
    [HttpGet("RegisterChild")]
    public async Task<IActionResult> RegisterChild()
    {
        return View("RegisterChild");
    }

    //[Authorize(Roles = "администратор")]
    [HttpPost("RegisterChild")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterChild(RegisterChildWithParentsModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["_secure_at"]);

        var response = await client.PostAsJsonAsync($"{_config["ApiBaseUrl"]}/api/users/register-child", new
        {
            model.Login,
            model.Password,
            model.Name,
            model.Surname,
            model.Patronymic,
            BirthDate = DateOnly.FromDateTime(model.BirthDate),
            model.Phone,
            model.EducationName,
            model.ParentIds,
            model.ParentRole
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Ошибка при регистрации ребенка");
            return View(model);
        }

        return RedirectToAction("Index", "Users");
    }

    [HttpGet("RegisterParent")]
    public IActionResult RegisterParent()
    {
        return View();
    }

    [HttpPost("RegisterParent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterParent(RegisterParentWithChildrenModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("RegisterParent", model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Request.Cookies["_secure_at"]);

            var requestData = new
            {
                model.Login,
                model.Password,
                model.Name,
                model.Surname,
                model.Patronymic,
                BirthDate = DateOnly.FromDateTime(model.BirthDate),
                model.Phone,
                model.Workplace,
                Children = model.Children.Select(c => new
                {
                    c.Login,
                    c.Password,
                    c.Name,
                    c.Surname,
                    c.Patronymic,
                    BirthDate = DateOnly.FromDateTime(c.BirthDate),
                    c.Phone,
                    c.EducationName,
                    c.ParentRole // Добавляем роль родителя
                }).ToList()
            };

            var response = await client.PostAsJsonAsync(
                $"{_config["ApiBaseUrl"]}/api/users/register-parent-with-children",
                requestData);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", "Ошибка при регистрации: " + errorContent);
                return View("RegisterParent", model);
            }

            return RedirectToAction("Index", "Users");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Ошибка: " + ex.Message);
            return View("RegisterParent", model);
        }
    }
}