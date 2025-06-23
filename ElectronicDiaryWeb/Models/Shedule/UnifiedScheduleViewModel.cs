using ElectronicDiaryApi.Controllers;
using ElectronicDiaryApi.ModelsDto.Responses;
using static ElectronicDiaryApi.Controllers.ScheduleController;

namespace ElectronicDiaryWeb.Models
{
    public class UnifiedScheduleViewModel
    {
        public UnifiedScheduleResponseDto Schedule { get; set; }
        public DateTime CurrentWeekStart { get; set; }
        public DateTime PreviousWeekStart { get; set; }
        public DateTime NextWeekStart { get; set; }
        public bool ShowPersonalToggle { get; set; }
        public string UserRole { get; set; }
        public bool HasPersonalData { get; set; }

        public bool IsPersonalMode => Schedule?.IsPersonalMode ?? false;
    }
}
