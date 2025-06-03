namespace ElectronicDiaryApi.ModelsDto.Shedule
{
    public class StandardScheduleDto
    {
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Classroom { get; set; }
    }
}
