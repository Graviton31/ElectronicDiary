namespace ElectronicDiaryApi.ModelsDto.Group
{
    public class GroupShortInfoDto
    {
        public int IdGroup { get; set; }
        public string Name { get; set; }
        public sbyte MaxStudentCount { get; set; }
        public int CurrentStudents { get; set; }
        public int RequestCount { get; set; }
    }
}
