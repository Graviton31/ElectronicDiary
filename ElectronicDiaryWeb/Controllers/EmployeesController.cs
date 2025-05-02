using ElectronicDiaryApi.ModelsDto;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicDiaryWeb.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly HttpClient _httpClient;

        public EmployeesController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7123"); // Адрес вашего API
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
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
