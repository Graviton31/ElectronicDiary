using ElectronicDiaryApi.ModelsDto.Journal;
using ElectronicDiaryApi.ModelsDto.Subject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ElectronicDiaryWeb.Controllers
{
    [Authorize(Roles = "администратор, руководитель, учитель")]
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
                // Безопасное получение ID пользователя
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    ViewBag.ErrorMessage = "Не удалось определить ваш идентификатор пользователя";
                    return View(new List<SubjectDto>());
                }

                // Для администраторов и руководителей показываем все предметы
                if (User.IsInRole("администратор") || User.IsInRole("руководитель"))
                {
                    var response = await _httpClient.GetAsync("/api/Journals/Subjects");
                    response.EnsureSuccessStatusCode();
                    var subjects = await response.Content.ReadFromJsonAsync<List<SubjectDto>>();
                    return View(subjects);
                }
                // Для учителей показываем только их предметы
                else if (User.IsInRole("учитель"))
                {
                    var response = await _httpClient.GetAsync($"/api/Journals/Subjects?teacherId={userId}");
                    response.EnsureSuccessStatusCode();
                    var subjects = await response.Content.ReadFromJsonAsync<List<SubjectDto>>();

                    if (subjects == null || !subjects.Any())
                    {
                        ViewBag.ErrorMessage = "Вы не ведёте ни одного предмета. Если это ошибка, обратитесь к администратору.";
                        ViewBag.HideInterface = true; // Флаг для скрытия интерфейса
                    }

                    return View(subjects ?? new List<SubjectDto>());
                }
                else
                {
                    ViewBag.ErrorMessage = "У вас нет доступа к этой странице";
                    ViewBag.HideInterface = true;
                    return View(new List<SubjectDto>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Ошибка при загрузке предметов";
                ViewBag.HideInterface = true;
                return View(new List<SubjectDto>());
            }
        }
    }
}