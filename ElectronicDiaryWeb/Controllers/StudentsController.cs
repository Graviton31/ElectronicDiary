using ElectronicDiaryApi.ModelsDto.UsersView;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ElectronicDiaryWeb.Controllers
{
    // MVC Controller (StudentsController.cs)
    public class StudentsController : Controller
    {
        private readonly HttpClient _httpClient;

        public StudentsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7123");
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                // Восстановление состояния
                var savedState = HttpContext.Session.GetString("UserListState");
                if (!string.IsNullOrEmpty(savedState))
                {
                    ViewBag.ReturnUrl = Url.Action("Index", "Users") +
                                      JsonConvert.DeserializeObject<Dictionary<string, string>>(savedState)["query"];
                }
                else
                {
                    ViewBag.ReturnUrl = Url.Action("Index", "Users");
                }

                // Остальная логика
                var response = await _httpClient.GetAsync($"/api/Students/Details/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var student = await response.Content.ReadFromJsonAsync<StudentDto>();
                return View(student);
            }
            catch
            {
                return StatusCode(500, "Ошибка при получении данных");
            }
        }
    }
}
