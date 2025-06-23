using ElectronicDiaryApi.ModelsDto.Responses;
using ElectronicDiaryWeb.Models;
using ElectronicDiaryWeb.Models.Shedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace ElectronicDiaryWeb.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ScheduleController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7123/api/");
            _httpContextAccessor = httpContextAccessor;
        }

        [Authorize]
        public async Task<IActionResult> Index(DateTime? date, bool? showAll)
        {
            try
            {
                var accessToken = _httpContextAccessor.HttpContext?.Request.Cookies["_secure_at"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", accessToken);
                }

                var targetDate = date ?? DateTime.Today;
                var weekStart = targetDate.AddDays(-(int)targetDate.DayOfWeek + (int)DayOfWeek.Monday);

                var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
                var userRole = User.FindFirstValue(ClaimTypes.Role);

                // Определяем режим отображения
                bool personal;
                if (userRole == "родитель")
                {
                    // Для родителя принудительно показываем личное расписание, если не нажата кнопка "Показать все"
                    personal = !(showAll ?? false);

                    // Проверяем, есть ли вообще данные для родителя
                    var checkResponse = await _httpClient.GetAsync($"schedule/{targetDate:yyyy-MM-dd}/true");
                    if (checkResponse.IsSuccessStatusCode)
                    {
                        var checkData = await checkResponse.Content.ReadFromJsonAsync<UnifiedScheduleResponseDto>();
                        if (checkData?.HasPersonalSchedule == false)
                        {
                            // Если нет личного расписания, показываем общее
                            personal = false;
                        }
                    }
                }
                else
                {
                    // Для других ролей стандартная логика
                    personal = isAuthenticated && !(showAll ?? false);
                }

                // Получаем данные
                var apiUrl = $"schedule/{targetDate:yyyy-MM-dd}/{personal}";
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API returned {response.StatusCode}");
                }

                var schedule = await response.Content.ReadFromJsonAsync<UnifiedScheduleResponseDto>();
                var hasPersonalData = schedule?.Days?.Any(d => d.Lessons.Any()) ?? false;

                // Определяем, показывать ли кнопку переключения
                bool showPersonalToggle;
                if (userRole == "родитель")
                {
                    // Для родителя кнопка показывается всегда, если есть личное расписание
                    showPersonalToggle = schedule?.HasPersonalSchedule == true;
                }
                else
                {
                    // Для других ролей стандартная логика
                    showPersonalToggle = isAuthenticated &&
                        ((userRole == "учитель" && schedule?.HasPersonalSchedule == true) ||
                         (userRole == "студент"));
                }

                return View(new UnifiedScheduleViewModel
                {
                    Schedule = schedule,
                    CurrentWeekStart = weekStart,
                    PreviousWeekStart = weekStart.AddDays(-7),
                    NextWeekStart = weekStart.AddDays(7),
                    ShowPersonalToggle = showPersonalToggle,
                    UserRole = userRole,
                    HasPersonalData = hasPersonalData
                });
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel
                {
                    Message = "Ошибка при загрузке расписания",
                    Details = ex.Message
                });
            }
        }
    }
}