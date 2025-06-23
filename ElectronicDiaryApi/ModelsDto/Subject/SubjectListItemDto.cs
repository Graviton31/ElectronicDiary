namespace ElectronicDiaryApi.ModelsDto.Subject
{
    public class SubjectListItemDto
    {
        public int IdSubject { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public int GroupsCount { get; set; }
        public int TeachersCount { get; set; }
        public sbyte Duration { get; set; }
    }
}
