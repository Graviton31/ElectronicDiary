namespace ElectronicDiaryApi.ModelsDto.Auth
{
    public class BaseUserDto
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string? Patronymic { get; set; }
        public DateOnly BirthDate { get; set; }
        public string Phone { get; set; }
    }
}
