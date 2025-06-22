using ElectronicDiaryApi.ModelsDto.Diary;
using ElectronicDiaryApi.ModelsDto.Journal;
using ElectronicDiaryApi.ModelsDto.UsersView;
using static ElectronicDiaryApi.Controllers.DiaryController;

namespace ElectronicDiaryWeb.Models
{
    public class DiaryWeekViewModel
    {
        public DateOnly WeekStartDate { get; set; }
        public DateOnly WeekEndDate { get; set; }
        public DateOnly PreviousWeekStart { get; set; }
        public DateOnly NextWeekStart { get; set; }
        public List<DiaryDayDto> Days { get; set; } = new();
        public List<ChildDto> AvailableChildren { get; set; } = new();
        public int? SelectedChildId { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int AbsentCount { get; set; }
        public bool HasMultipleChildren { get; set; }
    }
}
