namespace ElectronicDiaryApi.ModelsDto.Shedule
{
    public class DayScheduleDto
    {
        public DateOnly Date { get; set; }
        public string DayOfWeek { get; set; }
        public List<UnifiedLessonInfoDto> Lessons { get; set; }
    }
}
