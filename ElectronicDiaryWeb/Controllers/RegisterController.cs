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

    public RegisterController(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
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

        return RedirectToAction("Index", "Home");
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

        return RedirectToAction("Index", "Home");
    }

    [HttpGet("RegisterParent")]
    public IActionResult RegisterParent()
    {
        return View();
    }

    [HttpPost("RegisterParent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterParent(RegisterParentModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("RegisterParent");
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["_secure_at"]);

        var response = await client.PostAsJsonAsync($"{_config["ApiBaseUrl"]}/api/users/register-parent", new
        {
            model.Login,
            model.Password,
            model.Name,
            model.Surname,
            model.Patronymic,
            BirthDate = DateOnly.FromDateTime(model.BirthDate),
            model.Phone,
            model.Workplace
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Ошибка при регистрации родителя");
            return View(model);
        }

        return RedirectToAction("Index", "Home");
    }
}