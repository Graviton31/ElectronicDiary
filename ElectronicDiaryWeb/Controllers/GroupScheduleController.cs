using ElectronicDiaryWeb.Models;
using ElectronicDiaryApi.ModelsDto.Shedule;
using ElectronicDiaryApi.ModelsDto.Group;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Json;
using System.Text.Json;
using ElectronicDiaryApi.Controllers;
using static ElectronicDiaryApi.Controllers.StandardScheduleController;

namespace ElectronicDiaryWeb.Controllers
{
    [Route("GroupSchedule")]
    public class GroupScheduleController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public GroupScheduleController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7123/api/");
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? groupId, DateTime? date, int? subjectId)
        {
            var model = new CombinedScheduleViewModel();
            model.SubjectId = subjectId ?? 1;

            try
            {
                // Загрузка групп
                var groupsResponse = await _httpClient.GetAsync($"groups/{model.SubjectId}/groups");
                groupsResponse.EnsureSuccessStatusCode();
                model.Groups = await groupsResponse.Content.ReadFromJsonAsync<List<GroupNameDto>>(_jsonOptions);

                // Автовыбор первой группы
                if (model.Groups?.Any() == true && !groupId.HasValue)
                {
                    groupId = model.Groups[0].IdGroup;
                    return RedirectToAction("Index", new { groupId, date, subjectId = model.SubjectId });
                }

                if (groupId.HasValue)
                {
                    // Загрузка расписания
                    var targetDate = date ?? DateTime.Today;
                    var scheduleResponse = await _httpClient.GetAsync(
                        $"schedule/group/{groupId}/week/{targetDate:yyyy-MM-dd}");
                    scheduleResponse.EnsureSuccessStatusCode();

                    model.Schedule = await scheduleResponse.Content.ReadFromJsonAsync<UnifiedScheduleResponseDto>(_jsonOptions);
                    model.SelectedGroupId = groupId;
                    model.CurrentWeekStart = model.Schedule.WeekStartDate.ToDateTime(TimeOnly.MinValue);
                    model.PreviousWeekStart = model.CurrentWeekStart.AddDays(-7);
                    model.NextWeekStart = model.CurrentWeekStart.AddDays(7);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка загрузки данных: {ex.Message}");
            }

            return View(model);
        }

        // GET /GroupSchedule/CreateStandardForm
        [HttpGet("CreateStandardForm")] 
        public async Task<IActionResult> CreateStandardForm(int groupId)
        {
            try
            {
                var model = new EditStandardScheduleViewModel
                {
                    GroupId = groupId,
                    StartTime = TimeOnly.FromDateTime(DateTime.Now),
                    EndTime = TimeOnly.FromDateTime(DateTime.Now.AddHours(1))
                };
                return PartialView("_StandardScheduleForm", model);
            }
            catch (Exception ex)
            {
                return PartialView("_Error", ex.Message);
            }
        }

        [HttpPost("CreateStandard")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStandard(EditStandardScheduleViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return PartialView("_StandardScheduleForm", model);

                var response = await _httpClient.PostAsJsonAsync(
                    "api/standard-schedules",
                    new StandardScheduleController.StandardScheduleRequest
                    {
                        GroupId = model.GroupId,
                        WeekDay = model.WeekDay,
                        StartTime = model.StartTime,
                        EndTime = model.EndTime,
                        Classroom = model.Classroom
                    });

                response.EnsureSuccessStatusCode();
                return Content("<script>window.location.reload();</script>", "text/html");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка создания: {ex.Message}");
                return PartialView("_StandardScheduleForm", model);
            }
        }

        [HttpGet("EditStandard/{id}")]
        public async Task<IActionResult> EditStandard(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/standard-schedules/{id}");
                response.EnsureSuccessStatusCode();
                
                var schedule = await response.Content.ReadFromJsonAsync<StandardScheduleResponse>(_jsonOptions);
                
                var model = new EditStandardScheduleViewModel
                {
                    Id = schedule.Id,
                    GroupId = schedule.GroupId,
                    WeekDay = schedule.WeekDay,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    Classroom = schedule.Classroom
                };

                return PartialView("_StandardScheduleForm", model);
            }
            catch (Exception ex)
            {
                return PartialView("_Error", ex.Message);
            }
        }

        [HttpPost("EditStandard/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStandard(int id, EditStandardScheduleViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return PartialView("_StandardScheduleForm", model);

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/standard-schedules/{id}",
                    new StandardScheduleController.StandardScheduleRequest
                    {
                        GroupId = model.GroupId,
                        WeekDay = model.WeekDay,
                        StartTime = model.StartTime,
                        EndTime = model.EndTime,
                        Classroom = model.Classroom
                    });

                response.EnsureSuccessStatusCode();
                return Content("<script>window.location.reload();</script>", "text/html");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка сохранения: {ex.Message}");
                return PartialView("_StandardScheduleForm", model);
            }
        }

        [HttpGet("CreateChangeForm")]
        public async Task<IActionResult> CreateChangeForm(int groupId)
        {
            try
            {
                var model = new EditScheduleChangeViewModel 
                { 
                    GroupId = groupId,
                    NewDate = DateOnly.FromDateTime(DateTime.Now)
                };

                // Загрузка стандартных занятий
                var schedulesResponse = await _httpClient.GetAsync($"standard-schedules/group/{groupId}");
                schedulesResponse.EnsureSuccessStatusCode();
                
                var schedules = await schedulesResponse.Content
                    .ReadFromJsonAsync<List<StandardScheduleResponse>>(_jsonOptions);

                ViewBag.StandardSchedules = schedules.Select(s => new SelectListItem 
                {
                    Value = s.Id.ToString(),
                    Text = $"{GetDayName(s.WeekDay)} {s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm}"
                });

                return PartialView("_ScheduleChangeForm", model);
            }
            catch (Exception ex)
            {
                return PartialView("_Error", ex.Message);
            }
        }

        [HttpPost("CreateChange")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChange(EditScheduleChangeViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return PartialView("_ScheduleChangeForm", model);

                var response = await _httpClient.PostAsJsonAsync(
                    "api/schedule-changes",
                    new ScheduleChangeController.ScheduleChangeRequest
                    {
                        GroupId = model.GroupId,
                        ChangeType = model.ChangeType,
                        OldDate = model.OldDate,
                        NewDate = model.NewDate,
                        NewStartTime = model.NewStartTime,
                        NewEndTime = model.NewEndTime,
                        NewClassroom = model.NewClassroom,
                        StandardScheduleId = model.StandardScheduleId
                    });

                response.EnsureSuccessStatusCode();
                return Content("<script>window.location.reload();</script>", "text/html");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка создания: {ex.Message}");
                return PartialView("_ScheduleChangeForm", model);
            }
        }

        [HttpGet("EditChange/{id}")]
        public async Task<IActionResult> EditChange(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/schedule-changes/{id}");
                response.EnsureSuccessStatusCode();
                
                var change = await response.Content.ReadFromJsonAsync<ScheduleChangeController.ScheduleChangeResponse>(_jsonOptions);

                var model = new EditScheduleChangeViewModel
                {
                    Id = change.Id,
                    GroupId = change.GroupId,
                    ChangeType = change.ChangeType,
                    OldDate = change.OldDate,
                    NewDate = change.NewDate,
                    NewStartTime = change.NewStartTime,
                    NewEndTime = change.NewEndTime,
                    NewClassroom = change.NewClassroom,
                    StandardScheduleId = change.StandardScheduleId
                };

                // Повторная загрузка стандартных занятий
                var schedulesResponse = await _httpClient.GetAsync($"api/standard-schedules/group/{model.GroupId}");
                schedulesResponse.EnsureSuccessStatusCode();
                
                var schedules = await schedulesResponse.Content
                    .ReadFromJsonAsync<List<StandardScheduleResponse>>(_jsonOptions);

                ViewBag.StandardSchedules = schedules.Select(s => new SelectListItem 
                {
                    Value = s.Id.ToString(),
                    Text = $"{GetDayName(s.WeekDay)} {s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm}"
                });

                return PartialView("_ScheduleChangeForm", model);
            }
            catch (Exception ex)
            {
                return PartialView("_Error", ex.Message);
            }
        }

        [HttpPost("EditChange/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditChange(int id, EditScheduleChangeViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return PartialView("_ScheduleChangeForm", model);

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/schedule-changes/{id}",
                    new ScheduleChangeController.ScheduleChangeRequest
                    {
                        GroupId = model.GroupId,
                        ChangeType = model.ChangeType,
                        OldDate = model.OldDate,
                        NewDate = model.NewDate,
                        NewStartTime = model.NewStartTime,
                        NewEndTime = model.NewEndTime,
                        NewClassroom = model.NewClassroom,
                        StandardScheduleId = model.StandardScheduleId
                    });

                response.EnsureSuccessStatusCode();
                return Content("<script>window.location.reload();</script>", "text/html");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка сохранения: {ex.Message}");
                return PartialView("_ScheduleChangeForm", model);
            }
        }

        private string GetDayName(sbyte weekDay)
        {
            return weekDay switch
            {
                1 => "Понедельник",
                2 => "Вторник",
                3 => "Среда",
                4 => "Четверг",
                5 => "Пятница",
                6 => "Суббота",
                7 => "Воскресенье",
                _ => "Неизвестный день"
            };
        }
    }
}