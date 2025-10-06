using System.Collections.Generic;

namespace CatatanKeuanganDotnet.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsIncome { get; set; }
        public string UserId { get; set; } = string.Empty;

        public User? User { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
