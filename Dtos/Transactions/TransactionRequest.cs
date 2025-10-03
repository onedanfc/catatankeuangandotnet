using System;
using System.ComponentModel.DataAnnotations;

namespace CatatanKeuanganDotnet.Dtos.Transactions
{
    public class TransactionRequest
    {
        [StringLength(250)]
        public string? Description { get; set; }

        [Range(typeof(decimal), "0.0", "79228162514264337593543950335")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public bool IsIncome { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CategoryId { get; set; }
    }
}