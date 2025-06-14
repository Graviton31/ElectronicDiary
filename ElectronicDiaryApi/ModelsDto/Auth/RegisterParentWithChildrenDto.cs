namespace ElectronicDiaryApi.ModelsDto.Auth
{
    public class RegisterParentWithChildrenDto : RegisterParentDto
    {
        public List<RegisterChildDto> Children { get; set; } = new();
    }
}
