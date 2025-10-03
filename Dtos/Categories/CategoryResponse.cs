using System;

namespace CatatanKeuanganDotnet.Dtos.Categories
{
    public class CategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsIncome { get; set; }
        public int UserId { get; set; }
    }
}