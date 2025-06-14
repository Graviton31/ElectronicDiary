namespace ElectronicDiaryApi.ModelsDto.Auth
{
    public class RegisterEmployeeDto : BaseUserDto
    {
        public string Role { get; set; } = "сотрудник";
        public int PostId { get; set; }
    }
}
