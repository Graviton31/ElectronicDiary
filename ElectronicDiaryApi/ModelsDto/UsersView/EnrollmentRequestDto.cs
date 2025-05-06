namespace ElectronicDiaryApi.ModelsDto.UsersView
{
    public class EnrollmentRequestDto
    {
        public int IdRequest { get; set; }
        public DateTime? RequestDate { get; set; }
        public string Status { get; set; }
        public string StudentFullName { get; set; }
        public string GroupName { get; set; }
        public string SubjectName { get; set; }
    }
}
