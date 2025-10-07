using System;
using System.ComponentModel.DataAnnotations;

namespace CatatanKeuanganDotnet.Dtos.Ai
{
    public class AiDigestRequest
    {
        private const string PeriodError = "Period hanya boleh bernilai daily atau weekly.";

        [Required(ErrorMessage = "UserId wajib diisi.")]
        [MinLength(3, ErrorMessage = "UserId minimal 3 karakter.")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = PeriodError)]
        [RegularExpression("daily|weekly", ErrorMessage = PeriodError)]
        public string Period { get; set; } = "daily";

        public DateTime? ReferenceDate { get; set; }
    }
}
