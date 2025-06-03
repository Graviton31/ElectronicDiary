namespace ElectronicDiaryApi.ModelsDto.Shedule
{
    public class UnifiedScheduleResponseDto
    {
        public DateOnly WeekStartDate { get; set; }
        public DateOnly WeekEndDate { get; set; }
        public List<DayScheduleDto> Days { get; set; }
    }
}
