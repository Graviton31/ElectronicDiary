using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto;
using ElectronicDiaryWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace ElectronicDiaryWeb.Controllers
{
    public class SubjectsController : Controller
    {
        private readonly HttpClient _apiClient;

        public SubjectsController(IHttpClientFactory httpClientFactory)
        {
            _apiClient = httpClientFactory.CreateClient("ApiClient");
            _apiClient.BaseAddress = new Uri("https://localhost:7123");
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _apiClient.GetAsync("/api/Subjects");

                if (response.IsSuccessStatusCode)
                {
                    // Используем правильный DTO
                    var subjects = await response.Content.ReadFromJsonAsync<List<SubjectListItemDto>>();
                    return View(subjects);
                }

                ViewBag.ErrorMessage = "Ошибка при загрузке данных";
                return View(new List<SubjectListItemDto>());
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ошибка: {ex.Message}";
                return View(new List<SubjectListItemDto>());
            }
        }
    }
}