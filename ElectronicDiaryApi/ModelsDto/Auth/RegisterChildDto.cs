namespace ElectronicDiaryApi.ModelsDto.Auth
{
    public class RegisterChildDto : BaseUserDto
    {
        public string EducationName { get; set; }
        public string ParentRole { get; set; } = "опекун"; // Добавляем поле
    }
}
