namespace ElectronicDiaryWeb.Models.Auth
{
    public class AuthResponseModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
        public DateTime AccessTokenExpires { get; set; }
        public DateTime RefreshTokenExpires { get; set; }
    }
}
