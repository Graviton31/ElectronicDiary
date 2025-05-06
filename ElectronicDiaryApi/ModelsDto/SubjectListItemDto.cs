namespace ElectronicDiaryApi.ModelsDto
{
    public class SubjectListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public int GroupsCount { get; set; }
        public int TeachersCount { get; set; }
    }
}
