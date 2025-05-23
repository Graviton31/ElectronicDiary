namespace ElectronicDiaryApi.ModelsDto.Journal
{
    public class JournalLessonsResponse
    {
        public List<StudentVisitDto> Students { get; set; }
        public List<LessonDto> Lessons { get; set; }
    }
}
