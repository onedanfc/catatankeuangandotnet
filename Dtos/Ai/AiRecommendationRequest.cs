using System.ComponentModel.DataAnnotations;

namespace CatatanKeuanganDotnet.Dtos.Ai
{
    public class AiRecommendationRequest
    {
        [Required(ErrorMessage = "UserId wajib diisi.")]
        [MinLength(3, ErrorMessage = "UserId minimal 3 karakter.")]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(120, ErrorMessage = "Fokus maksimal 120 karakter.")]
        public string? Focus { get; set; }
    }
}
