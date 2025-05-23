namespace ElectronicDiaryApi.ModelsDto.Journal
{
    public class LessonDto
    {
        public int IdLesson { get; set; }
        public DateOnly? LessonDate { get; set; }
        public Dictionary<int, VisitDto> Visits { get; set; }
    }
}
