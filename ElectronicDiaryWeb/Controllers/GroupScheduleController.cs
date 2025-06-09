using ElectronicDiaryWeb.Models;
using ElectronicDiaryApi.ModelsDto.Shedule;
using ElectronicDiaryApi.ModelsDto.Group;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Json;
using System.Text.Json;
using ElectronicDiaryApi.Controllers;
using static ElectronicDiaryApi.Controllers.StandardScheduleController;
using static ElectronicDiaryApi.Controllers.ScheduleChangeController;
using ElectronicDiaryWeb.Models.Shedule;
using Microsoft.AspNetCore.Authorization;

namespace ElectronicDiaryWeb.Controllers
{   [Authorize]
    [Route("GroupSchedule")]
    public class GroupScheduleController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly ILogger<GroupsController> _logger;

        public GroupScheduleController(IHttpClientFactory httpClientFactory, ILogger<GroupsController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7123/api/");
            _logger = logger;
        }

        // Controllers/GroupScheduleController.cs
        
        [HttpGet("GetStandardSchedules")]
        public async Task<IActionResult> GetStandardSchedules(int groupId, DateTime date)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"StandardScheduleApi/GetStandardSchedules?groupId={groupId}&date={date:yyyy-MM-dd}");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode);
                }

                var schedules = await response.Content.ReadFromJsonAsync<List<StandardScheduleResponse>>();
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error communicating with API");
            }
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
                        $"Schedule/group/{groupId}/week/{targetDate:yyyy-MM-dd}");
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
                return PartialView("_StandardSchedulePartial", model);
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
                    return PartialView("_StandardSchedulePartial", model);

                var response = await _httpClient.PostAsJsonAsync(
                    "StandardSchedules",
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
                return PartialView("_StandardSchedulePartial", model);
            }
        }

        [HttpGet("EditStandard/{id}")]
        public async Task<IActionResult> EditStandard(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"StandardSchedules/{id}");
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

                return PartialView("_StandardSchedulePartial", model);
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
                    return PartialView("_StandardSchedulePartial", model);

                var response = await _httpClient.PutAsJsonAsync(
                    $"StandardSchedules/{id}",
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
                return PartialView("_StandardSchedulePartial", model);
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
                var schedulesResponse = await _httpClient.GetAsync($"StandardSchedules/group/{groupId}");
                schedulesResponse.EnsureSuccessStatusCode();

                var schedules = await schedulesResponse.Content
                    .ReadFromJsonAsync<List<StandardScheduleResponse>>(_jsonOptions);

                ViewBag.StandardSchedules = schedules.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{GetDayName(s.WeekDay)} {s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm}"
                });

                return PartialView("_ScheduleChangePartial", model);
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
            if (!ModelState.IsValid)
                return PartialView("_ScheduleChangePartial", model);

            try
            {
                if (model.ChangeType is "отмена" or "перенос")
                {
                    if (!model.StandardScheduleId.HasValue || !model.OldDate.HasValue)
                    {
                        ModelState.AddModelError("", "Необходимо выбрать дату и время занятия");
                        return PartialView("_ScheduleChangePartial", model);
                    }

                    var scheduleResponse = await _httpClient.GetAsync($"StandardSchedules/{model.StandardScheduleId}");
                    if (!scheduleResponse.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError("", "Выбранное занятие не существует");
                        return PartialView("_ScheduleChangePartial", model);
                    }

                    var schedule = await scheduleResponse.Content.ReadFromJsonAsync<StandardScheduleResponse>();

                    // Корректная конвертация дня недели
                    var selectedDateWeekday = model.OldDate.Value.DayOfWeek switch
                    {
                        DayOfWeek.Sunday => 7,
                        _ => (int)model.OldDate.Value.DayOfWeek
                    };

                    if (schedule.WeekDay != selectedDateWeekday)
                    {
                        ModelState.AddModelError("", "Выбранная дата не соответствует дню недели занятия");
                        return PartialView("_ScheduleChangePartial", model);
                    }
                }

                var changeResponse = await _httpClient.PostAsJsonAsync(
                    "ScheduleChanges",
                    new ScheduleChangeRequest
                    {
                        GroupId = model.GroupId,
                        ChangeType = model.ChangeType,
                        OldDate = Convert.ToString(model.OldDate),
                        NewDate = Convert.ToString(model.NewDate),
                        NewStartTime = Convert.ToString(model.NewStartTime),
                        NewEndTime = Convert.ToString(model.NewEndTime),
                        NewClassroom = model.NewClassroom,
                        StandardScheduleId = model.StandardScheduleId
                    });

                changeResponse.EnsureSuccessStatusCode();
                return Content("<script>window.location.reload();</script>", "text/html");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка: {ex.Message}");
                return PartialView("_ScheduleChangePartial", model);
            }
        }

        [HttpGet("EditChange/{id}")]
        public async Task<IActionResult> EditChange(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"ScheduleChanges/{id}");
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
                var schedulesResponse = await _httpClient.GetAsync($"StandardSchedules/group/{model.GroupId}");
                schedulesResponse.EnsureSuccessStatusCode();

                var schedules = await schedulesResponse.Content
                    .ReadFromJsonAsync<List<StandardScheduleResponse>>(_jsonOptions);

                ViewBag.StandardSchedules = schedules.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{GetDayName(s.WeekDay)} {s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm}"
                });

                return PartialView("_ScheduleChangePartial", model);
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
                    return PartialView("_ScheduleChangePartial", model);

                var response = await _httpClient.PutAsJsonAsync(
                    $"ScheduleChanges/{id}",
                    new ScheduleChangeController.ScheduleChangeRequest
                    {
                        GroupId = model.GroupId,
                        ChangeType = model.ChangeType,
                        OldDate = Convert.ToString(model.OldDate),
                        NewDate = Convert.ToString(model.NewDate),
                        NewStartTime = Convert.ToString(model.NewStartTime),
                        NewEndTime = Convert.ToString(model.NewEndTime),
                        NewClassroom = model.NewClassroom,
                        StandardScheduleId = model.StandardScheduleId
                    });

                response.EnsureSuccessStatusCode();
                return Content("<script>window.location.reload();</script>", "text/html");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка сохранения: {ex.Message}");
                return PartialView("_ScheduleChangePartial", model);
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