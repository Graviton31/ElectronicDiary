namespace ElectronicDiaryApi.ModelsDto.UsersView
{
    public class GroupDto
    {
        public int IdGroup { get; set; }

        public string Name { get; set; }

        public string Classroom { get; set; }

        public string SubjectName { get; set; }

        public int IdSubject { get; set; }

        public LocationDto Location { get; set; }

    }
}
