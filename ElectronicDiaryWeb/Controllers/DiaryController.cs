using ElectronicDiaryApi.ModelsDto.Diary;
using ElectronicDiaryWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ElectronicDiaryWeb.Controllers
{
    [Authorize]
    public class DiaryController : Controller
    {
        private readonly ILogger<DiaryController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DiaryController(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DiaryController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7123/api/");
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private async Task AddAuthHeader()
        {
            // Получаем токен из куков
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["_secure_at"];
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<IActionResult> Index(DateTime? date, int? studentId = null)
        {
            try
            {
                // Проверяем аутентификацию
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Account");
                }

                await AddAuthHeader();

                var targetDate = date ?? DateTime.Today;
                var url = $"diary/week/{targetDate:yyyy-MM-dd}";

                if (User.IsInRole("родитель") && studentId.HasValue)
                {
                    url += $"?studentId={studentId}";
                }

                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Пытаемся обновить токен
                    var refreshResponse = await RefreshToken();
                    if (!refreshResponse)
                    {
                        await HttpContext.SignOutAsync();
                        return RedirectToAction("Login", "Account");
                    }

                    // Повторяем запрос с новым токеном
                    await AddAuthHeader();
                    response = await _httpClient.GetAsync(url);
                }

                if (!response.IsSuccessStatusCode)
                {
                    return View(CreateEmptyViewModel(targetDate));
                }

                var data = await response.Content.ReadFromJsonAsync<DiaryWeekResponseDto>();
                var isParent = User.IsInRole("родитель");

                return View(new DiaryWeekViewModel
                {
                    WeekStartDate = data.WeekStartDate,
                    WeekEndDate = data.WeekEndDate,
                    PreviousWeekStart = data.WeekStartDate.AddDays(-7),
                    NextWeekStart = data.WeekStartDate.AddDays(7),
                    Days = data.Days ?? new List<DiaryDayDto>(),
                    AvailableChildren = data.AvailableChildren ?? new List<ChildDto>(),
                    SelectedChildId = isParent ? data.SelectedChildId : null,
                    TotalLessons = data.Days?.Sum(d => d.Lessons?.Count ?? 0) ?? 0,
                    CompletedLessons = data.Days?.Sum(d => d.Lessons?.Count(l => !string.IsNullOrEmpty(l.VisitStatus)) ?? 0) ?? 0,
                    AbsentCount = data.Days?.Sum(d => d.Lessons?.Count(l => l.VisitStatus == "н") ?? 0) ?? 0,
                    HasMultipleChildren = isParent && (data.AvailableChildren?.Count ?? 0) > 1
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в DiaryController");
                return View("Error", new ErrorViewModel { Message = "Произошла ошибка при загрузке дневника" });
            }
        }

        private async Task<bool> RefreshToken()
        {
            try
            {
                var refreshResponse = await _httpClient.PostAsync("/Account/RefreshToken", null);
                return refreshResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении токена");
                return false;
            }
        }

        [HttpGet("GetDiaryData")]
        public async Task<IActionResult> GetDiaryData(DateTime date, int? studentId = null)
        {
            try
            {
                await AddAuthHeader();

                // Формируем URL для запроса к API
                var url = $"https://localhost:7123/api/diary/week/{date:yyyy-MM-dd}";
                if (User.IsInRole("родитель") && studentId.HasValue)
                {
                    url += $"?studentId={studentId}";
                }

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Ошибка загрузки данных" });
                }

                var data = await response.Content.ReadFromJsonAsync<DiaryWeekResponseDto>();
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        days = data.Days,
                        availableChildren = data.AvailableChildren,
                        selectedChildId = data.SelectedChildId,
                        weekStartDate = data.WeekStartDate,
                        weekEndDate = data.WeekEndDate,
                        totalLessons = data.Days?.Sum(d => d.Lessons?.Count ?? 0) ?? 0,
                        completedLessons = data.Days?.Sum(d => d.Lessons?.Count(l => !string.IsNullOrEmpty(l.VisitStatus)) ?? 0) ?? 0,
                        absentCount = data.Days?.Sum(d => d.Lessons?.Count(l => l.VisitStatus == "н") ?? 0) ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке данных дневника");
                return Json(new { success = false, message = "Произошла ошибка" });
            }
        }

        // Создает пустую модель для случая ошибки
        private DiaryWeekViewModel CreateEmptyViewModel(DateTime date)
        {
            var weekStart = DateOnly.FromDateTime(date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday));
            var weekEnd = weekStart.AddDays(6);
            var isParent = User.IsInRole("родитель");

            return new DiaryWeekViewModel
            {
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd,
                PreviousWeekStart = weekStart.AddDays(-7),
                NextWeekStart = weekStart.AddDays(7),
                Days = new List<DiaryDayDto>(),
                AvailableChildren = new List<ChildDto>(),
                SelectedChildId = null,
                HasMultipleChildren = false
            };
        }
    }
}