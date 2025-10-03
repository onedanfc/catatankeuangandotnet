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
        public int UserId { get; set; }
        public int CategoryId { get; set; }
    }
}