using ElectronicDiaryApi.Controllers;
using ElectronicDiaryApi.ModelsDto.Shedule;
using static ElectronicDiaryApi.Controllers.ScheduleController;

namespace ElectronicDiaryWeb.Models
{
    public class UnifiedScheduleViewModel
    {
        public UnifiedScheduleResponseDto Schedule { get; set; }
        public DateTime CurrentWeekStart { get; set; }
        public DateTime PreviousWeekStart { get; set; }
        public DateTime NextWeekStart { get; set; }
    }
}
