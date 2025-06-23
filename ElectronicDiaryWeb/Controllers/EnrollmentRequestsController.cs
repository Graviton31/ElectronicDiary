using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Newtonsoft.Json;
using ElectronicDiaryApi.ModelsDto.UsersView;
using ElectronicDiaryWeb.Models;
using ElectronicDiaryApi.ModelsDto.Group;
using ElectronicDiaryApi.ModelsDto.EnrollmentRequest;
using Microsoft.AspNetCore.Authorization;

namespace ElectronicDiaryWeb.Controllers
{   [Authorize(Roles = "администратор, руководитель, учитель")]
    public class EnrollmentRequestsController : Controller
    {
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://localhost:7123"; // URL API

        public EnrollmentRequestsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
            _httpClient.BaseAddress = new Uri(ApiBaseUrl);
        }

        
        public async Task<IActionResult> Index(int subjectId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/EnrollmentRequests/{subjectId}/groups");

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var groups = JsonConvert.DeserializeObject<List<GroupShortInfoDto>>(content);

                // Загрузка количества заявок для каждой группы
                foreach (var group in groups)
                {
                    var countResponse = await _httpClient.GetAsync($"/api/EnrollmentRequests/groups/{group.IdGroup}/requests");
                    if (countResponse.IsSuccessStatusCode)
                    {
                        var requests = JsonConvert.DeserializeObject<List<dynamic>>(await countResponse.Content.ReadAsStringAsync());
                        group.RequestCount = requests?.Count ?? 0;
                    }
                }

                ViewBag.SubjectId = subjectId;
                return View(groups);
            }
            catch (HttpRequestException ex)
            {
                return View("Error", new ErrorViewModel
                {
                    Message = $"Ошибка подключения к API: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRequest(int requestId, [FromBody] UpdateEnrollmentRequestDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"/api/EnrollmentRequests/requests/{requestId}", dto);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true });
                }

                return Json(new
                {
                    success = false,
                    error = await response.Content.ReadAsStringAsync()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }
    }
}