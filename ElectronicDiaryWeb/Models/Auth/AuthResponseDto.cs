namespace ElectronicDiaryWeb.Models.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
        public DateTime AccessTokenExpires { get; set; }
        public DateTime RefreshTokenExpires { get; set; }
    }
}
