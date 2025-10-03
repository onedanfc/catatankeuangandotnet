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
        public int UserId { get; set; }
    }
}