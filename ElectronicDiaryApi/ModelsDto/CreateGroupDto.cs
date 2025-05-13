namespace ElectronicDiaryApi.ModelsDto
{
    public class CreateGroupDto
    {
        public string Name { get; set; }
        public sbyte MaxStudentCount { get; set; }
        public string Classroom { get; set; }
        public int IdSubject { get; set; }
        public int IdLocation { get; set; }
        public List<int> TeacherIds { get; set; } = new(); // Добавляем список учителей
        public string MinAge { get; set; } = null!;
        public string MaxAge { get; set; } = null!;
    }
}
