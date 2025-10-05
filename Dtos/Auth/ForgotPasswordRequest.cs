using System.ComponentModel.DataAnnotations;

namespace CatatanKeuanganDotnet.Dtos.Auth
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
