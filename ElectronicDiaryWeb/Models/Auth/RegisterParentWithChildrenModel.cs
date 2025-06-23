namespace ElectronicDiaryWeb.Models.Auth
{
    public class RegisterParentWithChildrenModel : RegisterParentModel
    {
        public List<RegisterChildModel> Children { get; set; } = new();
    }
}
