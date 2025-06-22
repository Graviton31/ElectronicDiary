namespace ElectronicDiaryApi.ModelsDto.Diary
{
    public class DiaryDayDto
    {
        public DateOnly Date { get; set; }
        public string DayOfWeek { get; set; }
        public List<DiaryLessonDto> Lessons { get; set; } = new();
    }
}
