namespace ElectronicDiaryApi.ModelsDto
{
    public class CreateGroupDto
    {
        public string Name { get; set; }
        public sbyte? StudentCount { get; set; }
        public string Classroom { get; set; }
        public int IdSubject { get; set; }
        public int IdLocation { get; set; }
        public List<int> TeacherIds { get; set; } = new(); // Добавляем список учителей
    }
}
