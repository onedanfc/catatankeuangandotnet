namespace CatatanKeuanganDotnet.Dtos.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserResponse User { get; set; } = new();
    }
}