using ElectronicDiaryApi.ModelsDto.UsersView;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ElectronicDiaryWeb.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly HttpClient _httpClient;

        public EmployeesController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7123");
        }

        // GET: Employees/Details/5
        [Authorize(Roles = "администратор, руководитель, учитель")]
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
                var response = await _httpClient.GetAsync($"/api/Employees/Details/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var employee = await response.Content.ReadFromJsonAsync<EmployeeDto>();
                return View(employee);
            }
            catch
            {
                return StatusCode(500, "Ошибка при получении данных");
            }
        }
    }
}
