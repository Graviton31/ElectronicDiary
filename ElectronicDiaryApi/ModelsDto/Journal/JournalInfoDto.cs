namespace ElectronicDiaryApi.ModelsDto.Journal
{
    public class JournalInfoDto
    {
        public int IdJournal { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public sbyte? LessonsCount { get; set; }
        public int CompletedLessons { get; set; }
        public int AbsentCount { get; set; }
        public string Name { get; set; }
    }
}
