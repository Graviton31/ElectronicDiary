using ElectronicDiaryApi.ModelsDto.Journal;
using ElectronicDiaryApi.ModelsDto.Subject;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicDiaryWeb.Controllers
{
    public class JournalsController : Controller
    {
        private readonly HttpClient _httpClient;

        public JournalsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7123");
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Journals/Subjects");
                response.EnsureSuccessStatusCode();
                var subjects = await response.Content.ReadFromJsonAsync<List<SubjectDto>>();
                return View(subjects);
            }
            catch
            {
                return View("Error", new { message = "Ошибка при загрузке предметов" });
            }
        }
    }
}