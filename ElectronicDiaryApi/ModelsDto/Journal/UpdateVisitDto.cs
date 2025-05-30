namespace ElectronicDiaryApi.ModelsDto.Journal
{
    public class UpdateVisitDto
    {
        public string? UnvisitedStatuses { get; set; } // 'н','б','у/п','к' или null (присутствовал)
        public string? Comment { get; set; }
    }
}
