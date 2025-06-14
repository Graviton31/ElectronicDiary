namespace ElectronicDiaryApi.ModelsDto.Subject
{
    public class SubjectWithStatsDto : SubjectListItemDto
    {
        public string Description { get; set; }
        public int TotalStudents { get; set; }
        public double AvgGroupFillPercentage { get; set; }
    }
}
