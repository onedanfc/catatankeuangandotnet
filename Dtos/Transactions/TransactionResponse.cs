using System;

namespace CatatanKeuanganDotnet.Dtos.Transactions
{
    public class TransactionResponse
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public bool IsIncome { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int CategoryId { get; set; }
    }
}
