namespace ElectronicDiaryApi.ModelsDto.Journal
{
    public class JournalExcelExportDto
    {
        public string GroupName { get; set; }
        public string SubjectName { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public List<StudentVisitDto> Students { get; set; }
        public List<LessonDto> Lessons { get; set; }
    }
}
