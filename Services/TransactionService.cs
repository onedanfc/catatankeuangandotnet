using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Data;
using CatatanKeuanganDotnet.Dtos.Transactions;
using CatatanKeuanganDotnet.Models;
using CatatanKeuanganDotnet.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CatatanKeuanganDotnet.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _context;

        public TransactionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<Transaction>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.UserId == userId)
                .OrderByDescending(transaction => transaction.Date)
                .ToListAsync(cancellationToken);

            return transactions;
        }

        public async Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(transaction => transaction.Id == id, cancellationToken);
        }

        public async Task<Transaction> CreateAsync(TransactionRequest request, CancellationToken cancellationToken = default)
        {
            var transaction = new Transaction
            {
                Description = request.Description,
                Amount = request.Amount,
                Date = request.Date,
                IsIncome = request.IsIncome,
                UserId = request.UserId,
                CategoryId = request.CategoryId
            };

            await _context.Transactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return transaction;
        }

        public async Task<bool> UpdateAsync(int id, TransactionRequest request, CancellationToken cancellationToken = default)
        {
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            if (transaction == null)
            {
                return false;
            }

            transaction.Description = request.Description;
            transaction.Amount = request.Amount;
            transaction.Date = request.Date;
            transaction.IsIncome = request.IsIncome;
            transaction.UserId = request.UserId;
            transaction.CategoryId = request.CategoryId;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            if (transaction == null)
            {
                return false;
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
