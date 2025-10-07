using System;
using System.ComponentModel.DataAnnotations;

namespace CatatanKeuanganDotnet.Dtos.Ai
{
    public class AiInsightRequest
    {
        [Required(ErrorMessage = "UserId wajib diisi.")]
        [MinLength(3, ErrorMessage = "UserId minimal 3 karakter.")]
        public string UserId { get; set; } = string.Empty;

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }
    }
}
