using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Dtos.Transactions;
using CatatanKeuanganDotnet.Dtos.Common;
using CatatanKeuanganDotnet.Models;
using CatatanKeuanganDotnet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CatatanKeuanganDotnet.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByUser([FromQuery] int userId, CancellationToken cancellationToken)
        {
            if (userId <= 0)
            {
                return BadRequest(ApiResponse.Failure(
                    "Parameter userId diperlukan.",
                    StatusCodes.Status400BadRequest));
            }

            var transactions = await _transactionService.GetByUserAsync(userId, cancellationToken);
            var data = transactions.Select(MapTransaction).ToList();

            return Ok(ApiResponse<IEnumerable<TransactionResponse>>.Succeeded(
                data,
                "Daftar transaksi berhasil diambil."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var transaction = await _transactionService.GetByIdAsync(id, cancellationToken);
            if (transaction == null)
            {
                return NotFound(ApiResponse.Failure(
                    "Transaksi tidak ditemukan.",
                    StatusCodes.Status404NotFound));
            }

            return Ok(ApiResponse<TransactionResponse>.Succeeded(
                MapTransaction(transaction),
                "Detail transaksi berhasil diambil."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TransactionRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data transaksi tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            var transaction = await _transactionService.CreateAsync(request, cancellationToken);
            var response = ApiResponse<TransactionResponse>.Succeeded(
                MapTransaction(transaction),
                "Transaksi berhasil dibuat.",
                StatusCodes.Status201Created);

            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] TransactionRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data transaksi tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            var updated = await _transactionService.UpdateAsync(id, request, cancellationToken);
            if (!updated)
            {
                return NotFound(ApiResponse.Failure(
                    "Transaksi tidak ditemukan.",
                    StatusCodes.Status404NotFound));
            }

            return Ok(ApiResponse.Succeeded("Transaksi berhasil diperbarui."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await _transactionService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(ApiResponse.Failure(
                    "Transaksi tidak ditemukan.",
                    StatusCodes.Status404NotFound));
            }

            return Ok(ApiResponse.Succeeded("Transaksi berhasil dihapus."));
        }

        private static TransactionResponse MapTransaction(Transaction transaction) => new()
        {
            Id = transaction.Id,
            Description = transaction.Description,
            Amount = transaction.Amount,
            Date = transaction.Date,
            IsIncome = transaction.IsIncome,
            UserId = transaction.UserId,
            CategoryId = transaction.CategoryId
        };
    }
}
