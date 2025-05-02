using ElectronicDiaryApi.ModelsDto;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicDiaryWeb.Controllers
{
    // MVC Controller (StudentsController.cs)
    public class StudentsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(
            IHttpClientFactory httpClientFactory,
            ILogger<StudentsController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7123");
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Students/Details/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var student = await response.Content.ReadFromJsonAsync<StudentDto>();
                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении студента {id}");
                return StatusCode(500, "Ошибка при загрузке данных");
            }
        }
    }
}
