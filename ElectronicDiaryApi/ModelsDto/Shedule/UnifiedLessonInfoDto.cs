namespace ElectronicDiaryApi.ModelsDto.Shedule
{
    public class UnifiedLessonInfoDto
    {
        public string GroupName { get; set; }
        public string SubjectName { get; set; }
        public List<string> Teachers { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Classroom { get; set; }
        public bool IsChanged { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsAdditional { get; set; }
        public string ChangeType { get; set; }
        public OriginalLessonDetailsDto? OriginalDetails { get; set; }
        public int? StandardScheduleId { get; set; }
        public int? ScheduleChangeId { get; set; }
    }
}
