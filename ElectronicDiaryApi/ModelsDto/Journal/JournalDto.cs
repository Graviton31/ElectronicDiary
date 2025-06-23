namespace ElectronicDiaryApi.ModelsDto.Journal
{
    public class JournalDto
    {
        public int IdJournal { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Name { get; set; }
    }
}
