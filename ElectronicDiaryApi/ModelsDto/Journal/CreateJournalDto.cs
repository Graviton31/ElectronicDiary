namespace ElectronicDiaryApi.ModelsDto.Journal
{
    public class CreateJournalDto
    {
        public int GroupId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public sbyte? LessonsCount { get; set; }
    }
}
