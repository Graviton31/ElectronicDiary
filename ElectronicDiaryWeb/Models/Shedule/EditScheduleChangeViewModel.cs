using ElectronicDiaryApi.ModelsDto.Shedule;

namespace ElectronicDiaryWeb.Models.Shedule
{
    public class EditScheduleChangeViewModel
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string ChangeType { get; set; }
        public DateOnly? OldDate { get; set; }
        public DateOnly? NewDate { get; set; }
        public TimeOnly? NewStartTime { get; set; }
        public TimeOnly? NewEndTime { get; set; }
        public string NewClassroom { get; set; }
        public int? StandardScheduleId { get; set; }

        public StandardScheduleDto StandardSchedule { get; set; }
    }
}
