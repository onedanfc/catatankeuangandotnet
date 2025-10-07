using System.ComponentModel.DataAnnotations;

namespace CatatanKeuanganDotnet.Dtos.Ai
{
    public class AiChatRequest
    {
        [Required(ErrorMessage = "UserId wajib diisi.")]
        [MinLength(3, ErrorMessage = "UserId minimal 3 karakter.")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pertanyaan wajib diisi.")]
        [MinLength(3, ErrorMessage = "Pertanyaan minimal 3 karakter.")]
        public string Question { get; set; } = string.Empty;
    }
}
