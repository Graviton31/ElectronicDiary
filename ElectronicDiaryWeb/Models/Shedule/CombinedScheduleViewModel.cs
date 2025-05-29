using ElectronicDiaryApi.ModelsDto.Group;
using ElectronicDiaryApi.ModelsDto.Shedule;

namespace ElectronicDiaryWeb.Models
{
    public class CombinedScheduleViewModel
    {
        public List<GroupNameDto> Groups { get; set; } = new();
        public int? SelectedGroupId { get; set; }
        public UnifiedScheduleResponseDto Schedule { get; set; }
        public DateTime CurrentWeekStart { get; set; }
        public DateTime PreviousWeekStart { get; set; }
        public DateTime NextWeekStart { get; set; }
        public int SubjectId { get; set; } = 1;
    }
}
