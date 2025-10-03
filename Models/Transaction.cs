using System;

namespace CatatanKeuanganDotnet.Models
{
    public class Transaction
    {
        public int Id { get; set; } // Primary Key
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public bool IsIncome { get; set; } // true untuk pemasukan, false untuk pengeluaran

        public int UserId { get; set; }
        public User? User { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}
