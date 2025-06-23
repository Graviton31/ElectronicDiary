using ElectronicDiaryApi.ModelsDto.Journal;

namespace ElectronicDiaryApi.ModelsDto.Diary
{
    public class DiaryWeekResponseDto
    {
        public DateOnly WeekStartDate { get; set; }
        public DateOnly WeekEndDate { get; set; }
        public List<DiaryDayDto> Days { get; set; }
        public List<ChildDto> AvailableChildren { get; set; }
        public List<JournalInfoDto> AvailableJournals { get; set; }
        public int? SelectedChildId { get; set; }
        public int CurrentJournalId { get; set; }
        public DateOnly? JournalStartDate { get; set; }
        public DateOnly? JournalEndDate { get; set; }
    }
}
