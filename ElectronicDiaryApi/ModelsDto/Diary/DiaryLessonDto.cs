using ElectronicDiaryApi.ModelsDto.Shedule;

namespace ElectronicDiaryApi.ModelsDto.Diary
{
    public class DiaryLessonDto
    {
        public int IdLesson { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string SubjectName { get; set; }
        public string GroupName { get; set; }
        public string Classroom { get; set; }
        public string Location { get; set; }
        public List<string> Teachers { get; set; } = new();
        public string? VisitStatus { get; set; }
        public string? Comment { get; set; }
        public string? Grade { get; set; } // Добавлено свойство для оценки
        public bool IsCancelled { get; set; }
        public bool IsAdditional { get; set; }
        public bool IsRescheduled { get; set; }
        public OriginalLessonDetailsDto? OriginalDetails { get; set; }
    }
}
