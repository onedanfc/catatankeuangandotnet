using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Dtos.Transactions;
using CatatanKeuanganDotnet.Models;
using CatatanKeuanganDotnet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
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
                return BadRequest(new { message = "Parameter userId diperlukan" });
            }

            var transactions = await _transactionService.GetByUserAsync(userId, cancellationToken);
            return Ok(transactions.Select(MapTransaction));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var transaction = await _transactionService.GetByIdAsync(id, cancellationToken);
            if (transaction == null)
            {
                return NotFound();
            }

            return Ok(MapTransaction(transaction));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TransactionRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var transaction = await _transactionService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, MapTransaction(transaction));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] TransactionRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var updated = await _transactionService.UpdateAsync(id, request, cancellationToken);
            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await _transactionService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
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