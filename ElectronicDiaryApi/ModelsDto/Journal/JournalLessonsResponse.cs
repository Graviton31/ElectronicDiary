namespace ElectronicDiaryApi.ModelsDto.Journal
{
    public class JournalLessonsResponse
    {
        public List<StudentVisitDto> Students { get; set; }
        public List<LessonDto> Lessons { get; set; }
        public JournalInfoDto JournalInfo { get; set; }
        public string Message { get; set; }
    }
}
