namespace ElectronicDiaryApi.ModelsDto.Shedule
{
    public class OriginalLessonDetailsDto
    {
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Classroom { get; set; }
    }
}
