using ElectronicDiaryApi.Controllers;
using static ElectronicDiaryApi.Controllers.ScheduleController;

namespace ElectronicDiaryWeb.Models
{
    public class UnifiedScheduleViewModel
    {
        public UnifiedScheduleResponse Schedule { get; set; }
        public DateTime CurrentWeekStart { get; set; }
        public DateTime PreviousWeekStart { get; set; }
        public DateTime NextWeekStart { get; set; }
    }
}
