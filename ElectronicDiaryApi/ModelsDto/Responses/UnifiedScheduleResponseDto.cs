using ElectronicDiaryApi.ModelsDto.Shedule;

namespace ElectronicDiaryApi.ModelsDto.Responses
{
    public class UnifiedScheduleResponseDto
    {
        public DateOnly WeekStartDate { get; set; }
        public DateOnly WeekEndDate { get; set; }
        public List<DayScheduleDto> Days { get; set; }
        public bool HasPersonalSchedule { get; set; }
        public bool IsPersonalMode { get; set; }
    }
}
