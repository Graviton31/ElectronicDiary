using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto;
using ElectronicDiaryApi.ModelsDto.EnrollmentRequest;
using ElectronicDiaryApi.ModelsDto.Group;
using ElectronicDiaryApi.ModelsDto.Subject;
using ElectronicDiaryWeb.Models;
using ElectronicDiaryWeb.ViewModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;
using System.Text.Json;

namespace ElectronicDiaryWeb.Controllers
{
    public class SubjectsController : Controller
    {
        private readonly HttpClient _apiClient;
        private readonly ILogger<HomeController> _logger;

        public SubjectsController(IHttpClientFactory httpClientFactory, ILogger<HomeController> logger)
        {
            _apiClient = httpClientFactory.CreateClient("ApiClient");
            _apiClient.BaseAddress = new Uri("https://localhost:7123");
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _apiClient.GetAsync("/api/Subjects");

                if (response.IsSuccessStatusCode)
                {
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

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new SubjectWithGroupViewModel();

            try
            {
                // Загрузка только локаций
                var locationsResponse = await _apiClient.GetAsync("/api/Locations");
                if (locationsResponse.IsSuccessStatusCode)
                {
                    var locations = await locationsResponse.Content.ReadFromJsonAsync<List<LocationDto>>();
                    vm.Locations = locations?.Select(l => new SelectListItem(l.Name, l.IdLocation.ToString())).ToList();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка загрузки данных: {ex.Message}");
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubjectWithGroupViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await ReloadDropdowns(vm);
                return View(vm);
            }

            try
            {
                var subjectDto = new CreateSubjectDto
                {
                    Name = vm.SubjectName,
                    FullName = vm.SubjectFullName,
                    Description = vm.SubjectDescription,
                    Duration = vm.Duration,
                    LessonLength = vm.LessonLength,
                    Syllabus = vm.Syllabus
                };

                var subjectResponse = await _apiClient.PostAsJsonAsync("/api/Subjects/create", subjectDto);
                subjectResponse.EnsureSuccessStatusCode();
                var createdSubject = await subjectResponse.Content.ReadFromJsonAsync<Subject>();

                foreach (var teacherId in vm.SelectedSubjectTeacherIds)
                {
                    await _apiClient.PostAsync(
                        $"/api/Subjects/{createdSubject.IdSubject}/teachers/{teacherId}",
                        null);
                }

                foreach (var group in vm.Groups)
                {
                    var groupDto = new CreateGroupDto
                    {
                        Name = group.Name,
                        Classroom = group.Classroom,
                        IdSubject = createdSubject.IdSubject,
                        IdLocation = group.SelectedLocationId,
                        TeacherIds = group.SelectedTeacherIds,
                        MinAge = group.MinAge,
                        MaxAge = group.MaxAge,
                    };

                    var groupResponse = await _apiClient.PostAsJsonAsync("/api/Groups", groupDto);
                    groupResponse.EnsureSuccessStatusCode();
                }

                return RedirectToAction("Index");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError("", $"Ошибка API: {ex.Message}");
                await ReloadDropdowns(vm);
                return View(vm);
            }
        }

        private async Task ReloadDropdowns(SubjectWithGroupViewModel vm)
        {
            // Загрузка только локаций
            var locations = await _apiClient.GetFromJsonAsync<List<LocationDto>>("/api/Locations");
            vm.Locations = locations?.Select(l => new SelectListItem(l.Name, l.IdLocation.ToString())).ToList();
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var subjectTask = _apiClient.GetFromJsonAsync<SubjectDto>($"/api/Subjects/{id}");
                var groupsTask = _apiClient.GetFromJsonAsync<List<GroupDto>>($"/api/Subjects/{id}/groups");

                await Task.WhenAll(subjectTask, groupsTask);

                if (subjectTask.Result == null) return NotFound();

                var model = new SubjectDetailsViewModel
                {
                    Subject = subjectTask.Result,
                    Groups = groupsTask.Result ?? new List<GroupDto>()
                };

                return View(model);
            }
            catch
            {
                return View("Error");
            }
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                // Загрузка данных предмета
                var subject = await _apiClient.GetFromJsonAsync<SubjectDto>($"/api/Subjects/{id}");
                if (subject == null)
                {
                    _logger.LogWarning("Предмет с ID {SubjectId} не найден", id);
                    return NotFound();
                }

                // Загрузка связанных данных
                var groups = await _apiClient.GetFromJsonAsync<List<GroupDto>>($"/api/Subjects/{id}/groups") ?? new();
                var locations = await _apiClient.GetFromJsonAsync<List<LocationDto>>("/api/Locations") ?? new();

                // Загрузка преподавателей
                var teachers = new List<EnrollmentRequestShortInfoDto>();
                var teachersResponse = await _apiClient.GetAsync("/api/Employees/all-short");
                if (teachersResponse.IsSuccessStatusCode)
                {
                    teachers = await teachersResponse.Content.ReadFromJsonAsync<List<EnrollmentRequestShortInfoDto>>() ?? new();
                }

                // Формирование ViewModel
                var vm = new EditSubjectViewModel
                {
                    IdSubject = subject.IdSubject,
                    SubjectName = subject.Name ?? "",
                    SubjectFullName = subject.FullName ?? "",
                    SubjectDescription = subject.Description ?? "",
                    Duration = subject.Duration,
                    LessonLength = subject.LessonLength,
                    Syllabus = subject.Syllabus, // nullable
                    SelectedSubjectTeacherIds = subject.Teachers?
                        .Select(t => t.IdEmployee)
                        .ToList() ?? new(),
                    ExistingGroups = groups.Select(g => new EditGroupViewModel
                    {
                        IdGroup = g.IdGroup,
                        Name = g.Name ?? "",
                        Classroom = g.Classroom ?? "",
                        SelectedLocationId = g.Location.IdLocation,
                        SelectedTeacherIds = g.Teachers
                            .Select(t => t.IdEmployee)
                            .ToList() ?? new(),
                        MaxStudentCount = g.MaxStudentCount,
                        MinAge = g.MinAge,
                        MaxAge = g.MaxAge   
                    }).ToList(),
                    Locations = locations.Select(l => new SelectListItem
                    {
                        Text = l.Name ?? "Без названия",
                        Value = l.IdLocation.ToString()
                    }).ToList(),
                    TeacherNames = teachers.ToDictionary(
                        t => t.IdEmployee,
                        t => t.FullName ?? "Неизвестный преподаватель")
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки данных для редактирования. ID: {SubjectId}", id);
                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Ошибка при загрузке данных предмета"
                });
            }
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditSubjectViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await RestoreViewModelData(vm);
                return View(vm);
            }

            try
            {
                // 1. Обновление основного предмета
                var updateSubjectDto = new UpdateSubjectDto
                {
                    Name = vm.SubjectName,
                    FullName = vm.SubjectFullName,
                    Description = vm.SubjectDescription,
                    Duration = vm.Duration,
                    LessonLength = vm.LessonLength,
                    Syllabus = vm.Syllabus,
                    TeacherIds = vm.SelectedSubjectTeacherIds ?? new List<int>(),
                    IsDelete = false
                };

                var subjectResponse = await _apiClient.PutAsJsonAsync($"/api/Subjects/{id}", updateSubjectDto);
                subjectResponse.EnsureSuccessStatusCode();

                // 2. Обработка групп
                foreach (var group in vm.ExistingGroups)
                {
                    if (group.IsDelete)
                    {
                        if (group.IdGroup != 0)
                        {
                            // Отправляем DELETE запрос для пометки удаления
                            await _apiClient.DeleteAsync($"/api/Groups/{group.IdGroup}");
                        }
                        continue;
                    }

                    if (group.IdGroup == 0) // Новая группа
                    {
                        var createGroupDto = new CreateGroupDto
                        {
                            Name = group.Name,
                            Classroom = group.Classroom,
                            IdSubject = id,
                            IdLocation = group.SelectedLocationId,
                            TeacherIds = group.SelectedTeacherIds ?? new List<int>(),
                            MaxStudentCount = group.MaxStudentCount,
                            MinAge = group.MinAge,
                            MaxAge = group.MaxAge
                        };

                        await _apiClient.PostAsJsonAsync("/api/Groups", createGroupDto);
                    }
                    else // Обновление существующей группы
                    {
                        var updateGroupDto = new UpdateGroupDto
                        {
                            Name = group.Name,
                            Classroom = group.Classroom,
                            IdLocation = group.SelectedLocationId,
                            TeacherIds = group.SelectedTeacherIds ?? new List<int>(),
                            MaxStudentCount = group.MaxStudentCount,
                            MinAge = group.MinAge,
                            MaxAge = group.MaxAge,
                            IsDelete = false
                        };

                        await _apiClient.PutAsJsonAsync($"/api/Groups/{group.IdGroup}", updateGroupDto);
                    }
                }

                // 3. Обновление кэша преподавателей
                var teachersResponse = await _apiClient.GetAsync("/api/Employees/all-short");
                if (teachersResponse.IsSuccessStatusCode)
                {
                    vm.TeacherNames = await teachersResponse.Content
                        .ReadFromJsonAsync<List<EnrollmentRequestShortInfoDto>>()
                        .ContinueWith(t => t.Result?
                            .ToDictionary(x => x.IdEmployee, x => x.FullName)
                            ?? new Dictionary<int, string>());
                }

                return RedirectToAction("Details", new { id });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении предмета. ID: {SubjectId}", id);
                ModelState.AddModelError("", $"Ошибка связи с сервером: {ex.Message}");
                await RestoreViewModelData(vm);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при обновлении предмета. ID: {SubjectId}", id);
                ModelState.AddModelError("", $"Произошла непредвиденная ошибка: {ex.Message}");
                await RestoreViewModelData(vm);
                return View(vm);
            }
        }

        private async Task RestoreViewModelData(EditSubjectViewModel vm)
        {
            // Восстановление списка локаций
            var locations = await _apiClient.GetFromJsonAsync<List<LocationDto>>("/api/Locations");
            vm.Locations = locations?
                .Select(l => new SelectListItem(l.Name, l.IdLocation.ToString()))
                .ToList() ?? new List<SelectListItem>();

            // Восстановление имен преподавателей
            var teachers = await _apiClient.GetFromJsonAsync<List<EnrollmentRequestShortInfoDto>>("/api/Employees/all-short");
            vm.TeacherNames = teachers?
                .ToDictionary(t => t.IdEmployee, t => t.FullName)
                ?? new Dictionary<int, string>();
        }

        private async Task ReloadDropdowns(EditSubjectViewModel vm)
        {
            var locations = await _apiClient.GetFromJsonAsync<List<LocationDto>>("/api/Locations");
            vm.Locations = locations?.Select(l => new SelectListItem(l.Name, l.IdLocation.ToString())).ToList();
        }
    }
}