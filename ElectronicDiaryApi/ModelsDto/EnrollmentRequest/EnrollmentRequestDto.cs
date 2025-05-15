namespace ElectronicDiaryApi.ModelsDto.EnrollmentRequest
{
    public class EnrollmentRequestDto
    {
        public int IdRequest { get; set; }
        public DateTime? RequestDate { get; set; }
        public string Status { get; set; }
        public string StudentFullName { get; set; }
        public int IdStudent { get; set; }
        public string ParentFullName { get; set; }
        public int IdParent { get; set; }
        public string GroupName { get; set; }
        public string SubjectName { get; set; }
        public string? Comment { get; set; }
    }
}
