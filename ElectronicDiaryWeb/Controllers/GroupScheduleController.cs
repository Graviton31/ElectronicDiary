//// Controllers/GroupScheduleController.cs
//using ElectronicDiaryApi.ModelsDto.Shedule;
//using ElectronicDiaryWeb.Models;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Net.Http;
//using System.Threading.Tasks;
//using static ElectronicDiaryApi.Controllers.GroupScheduleController;

//namespace ElectronicDiaryWeb.Controllers
//{
//    [Route("GroupSchedule")]
//    public class GroupScheduleController : Controller
//    {
//        private readonly HttpClient _httpClient;

//        public GroupScheduleController(IHttpClientFactory httpClientFactory)
//        {
//            _httpClient = httpClientFactory.CreateClient();
//            _httpClient.BaseAddress = new Uri("https://localhost:7123/api/");
//        }

//        [HttpGet("Manage/{groupId}")]
//        public async Task<IActionResult> Manage(int groupId, DateTime? date)
//        {
//            try
//            {
//                var targetDate = date ?? DateTime.Today;

//                // Получение информации о группе
//                var groupResponse = await _httpClient.GetAsync($"groups/{groupId}/info");
//                groupResponse.EnsureSuccessStatusCode();
//                var group = await groupResponse.Content.ReadFromJsonAsync<GroupInfoDto>();

//                // Получение расписания
//                var scheduleResponse = await _httpClient.GetAsync(
//                    $"GroupSchedule/{groupId}?date={targetDate:yyyy-MM-dd}");
//                scheduleResponse.EnsureSuccessStatusCode();
//                var schedule = await scheduleResponse.Content.ReadFromJsonAsync<GroupScheduleResponseDto>();

//                var model = new GroupScheduleManagementViewModel
//                {
//                    Group = group,
//                    Schedule = schedule,
//                    CurrentDate = targetDate,
//                    PreviousWeek = targetDate.AddDays(-7),
//                    NextWeek = targetDate.AddDays(7)
//                };

//                return View(model);
//            }
//            catch
//            {
//                return View("Error", new ErrorViewModel
//                {
//                    Message = "Ошибка загрузки данных группы"
//                });
//            }
//        }

//        [HttpGet("EditStandard/{scheduleId}")]
//        public async Task<IActionResult> EditStandard(int scheduleId)
//        {
//            try
//            {
//                var response = await _httpClient.GetAsync($"GroupSchedule/standard/{scheduleId}");
//                response.EnsureSuccessStatusCode();
//                var lesson = await response.Content.ReadFromJsonAsync<StandardLessonDto>();
//                return View(lesson);
//            }
//            catch
//            {
//                return View("Error", new ErrorViewModel
//                {
//                    Message = "Ошибка загрузки занятия"
//                });
//            }
//        }

//        [HttpPost("EditStandard/{scheduleId}")]
//        public async Task<IActionResult> EditStandard(int scheduleId, StandardLessonDto dto)
//        {
//            try
//            {
//                var response = await _httpClient.PutAsJsonAsync(
//                    $"GroupSchedule/standard/{scheduleId}", dto);
//                response.EnsureSuccessStatusCode();
//                return RedirectToAction("Manage", new { groupId = dto.GroupId });
//            }
//            catch
//            {
//                return View("Error", new ErrorViewModel
//                {
//                    Message = "Ошибка сохранения изменений"
//                });
//            }
//        }

//        [HttpGet("CreateStandard/{groupId}")]
//        public IActionResult CreateStandard(int groupId)
//        {
//            return View(new StandardLessonDto { GroupId = groupId });
//        }

//        [HttpPost("CreateStandard/{groupId}")]
//        public async Task<IActionResult> CreateStandard(int groupId, StandardLessonDto dto)
//        {
//            try
//            {
//                var response = await _httpClient.PostAsJsonAsync(
//                    $"GroupSchedule/{groupId}/standard", dto);
//                response.EnsureSuccessStatusCode();
//                return RedirectToAction("Manage", new { groupId });
//            }
//            catch
//            {
//                return View("Error", new ErrorViewModel
//                {
//                    Message = "Ошибка создания занятия"
//                });
//            }
//        }

//        [HttpGet("DeleteStandard/{scheduleId}")]
//        public async Task<IActionResult> DeleteStandard(int scheduleId)
//        {
//            try
//            {
//                var response = await _httpClient.DeleteAsync($"GroupSchedule/standard/{scheduleId}");
//                response.EnsureSuccessStatusCode();
//                return RedirectToAction("Manage", new { groupId = TempData["GroupId"] });
//            }
//            catch
//            {
//                return View("Error", new ErrorViewModel
//                {
//                    Message = "Ошибка удаления занятия"
//                });
//            }
//        }

//        // Аналогичные методы для работы с изменениями (Schedule Changes)
//    }

//    public class GroupInfoDto
//    {
//        public int Id { get; set; }
//        public string Name { get; set; }
//        public string SubjectName { get; set; }
//        public int MinAge { get; set; }
//        public int MaxAge { get; set; }
//    }

//    public class GroupScheduleManagementViewModel
//    {
//        public GroupInfoDto Group { get; set; }
//        public GroupScheduleResponseDto Schedule { get; set; }
//        public DateTime CurrentDate { get; set; }
//        public DateTime PreviousWeek { get; set; }
//        public DateTime NextWeek { get; set; }
//    }
//}