using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto;
using ElectronicDiaryApi.ModelsDto.UsersView;
using ElectronicDiaryWeb.Models;
using ElectronicDiaryWeb.ViewModel;
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
                    LessonLength = vm.LessonLength
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
                        TeacherIds = group.SelectedTeacherIds
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
    }
}