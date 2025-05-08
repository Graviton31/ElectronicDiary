using ElectronicDiaryWeb.Models;
using ElectronicDiaryWeb.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using static ElectronicDiaryApi.Controllers.ScheduleController;

namespace ElectronicDiaryWeb.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly HttpClient _httpClient;

        public ScheduleController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7123/api/");
        }

        public async Task<IActionResult> Index(DateTime? date)
        {
            var targetDate = date ?? DateTime.Today;
            var weekStart = targetDate.AddDays(-(int)targetDate.DayOfWeek + (int)DayOfWeek.Monday);

            try
            {
                var response = await _httpClient.GetAsync($"schedule/{targetDate:yyyy-MM-dd}");
                response.EnsureSuccessStatusCode();

                var schedule = await response.Content.ReadFromJsonAsync<UnifiedScheduleResponse>();
                var model = new UnifiedScheduleViewModel
                {
                    Schedule = schedule,
                    CurrentWeekStart = weekStart,
                    PreviousWeekStart = weekStart.AddDays(-7),
                    NextWeekStart = weekStart.AddDays(7)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel
                {
                    Message = "Ошибка при получении данных расписания",
                });
            }
        }
    }
}