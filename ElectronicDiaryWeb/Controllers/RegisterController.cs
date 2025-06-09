using ElectronicDiaryWeb.Models.Auth;
using ElectronicDiaryWeb.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;

public class RegisterController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public RegisterController(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    [Authorize(Roles = "администратор")]
    [HttpGet("employee")]
    public async Task<IActionResult> RegisterEmployee()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["accessToken"]);

        var postsResponse = await client.GetAsync($"{_config["ApiBaseUrl"]}/api/posts");
        if (postsResponse.IsSuccessStatusCode)
        {
            var posts = await postsResponse.Content.ReadFromJsonAsync<List<PostDto>>();
            ViewBag.Posts = posts.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            });
        }

        return View();
    }

    [Authorize(Roles = "администратор")]
    [HttpPost("employee")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterEmployee(RegisterEmployeeModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["accessToken"]);

        var response = await client.PostAsJsonAsync($"{_config["ApiBaseUrl"]}/api/users/register-employee", new
        {
            model.Login,
            model.Password,
            model.Name,
            model.Surname,
            model.Patronymic,
            model.BirthDate,
            model.Phone,
            model.Role,
            model.PostId
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Ошибка при регистрации сотрудника");
            return View(model);
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = "администратор")]
    [HttpGet("parent")]
    public IActionResult RegisterParent() => View();

    [Authorize(Roles = "администратор")]
    [HttpPost("parent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterParent(RegisterParentModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync($"{_config["ApiBaseUrl"]}/api/users/register-parent", new
        {
            model.Login,
            model.Password,
            model.Name,
            model.Surname,
            model.Patronymic,
            model.BirthDate,
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