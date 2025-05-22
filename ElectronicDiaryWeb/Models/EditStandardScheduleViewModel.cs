namespace ElectronicDiaryWeb.Models
{
    public class EditStandardScheduleViewModel
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public sbyte WeekDay { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Classroom { get; set; }
    }
}
