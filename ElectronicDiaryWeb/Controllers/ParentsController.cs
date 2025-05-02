using ElectronicDiaryApi.ModelsDto;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicDiaryWeb.Controllers
{
    public class ParentsController : Controller
    {
        private readonly HttpClient _httpClient;

        public ParentsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7123"); // Адрес вашего API
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Parents/Details/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var parent = await response.Content.ReadFromJsonAsync<ParentDto>();
                return View(parent);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении данных: {ex.Message}");
            }
        }
    }
}
