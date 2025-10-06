using System.ComponentModel.DataAnnotations;

namespace CatatanKeuanganDotnet.Dtos.Categories
{
    public class CategoryRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        public bool IsIncome { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 1)]
        public string UserId { get; set; } = string.Empty;
    }
}
