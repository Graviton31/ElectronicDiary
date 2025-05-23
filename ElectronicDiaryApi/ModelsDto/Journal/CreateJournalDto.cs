namespace ElectronicDiaryApi.ModelsDto.Journal
{
    public class CreateJournalDto
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int GroupId { get; set; }
    }
}
