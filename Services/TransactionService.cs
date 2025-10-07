using System;
using System.Collections.Generic;
using System.Globalization;
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

        public async Task<TransactionRecapResponse> GetMonthlyRecapAsync(string userId, string groupBy, CancellationToken cancellationToken = default)
        {
            // Validasi awal agar kita tidak mengeksekusi query tanpa identitas pengguna.
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("UserId wajib diisi untuk rekap transaksi.", nameof(userId));
            }

            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Normalisasi nilai groupBy agar hanya berisi day/week/month.
            var normalizedGroupBy = NormalizeGroupBy(groupBy);

            // Contoh SQL PostgreSQL setara query LINQ di bawah ini untuk dokumentasi frontend/backend:
            // SELECT *
            // FROM "Transactions"
            // WHERE "UserId" = @userId
            //   AND "Date" >= @startOfMonth
            //   AND "Date" < (@today + INTERVAL '1 day')
            // ORDER BY "Date" ASC;

            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId
                    && t.Date >= startOfMonth
                    && t.Date < today.AddDays(1))
                .OrderBy(t => t.Date)
                .ToListAsync(cancellationToken);

            // Pemetaan ke DTO agar bentuk object siap kirim ke lapisan controller/frontend.
            var mappedTransactions = transactions
                .Select(MapTransactionToResponse)
                .ToList();

            var groupedData = BuildGroupedRecap(mappedTransactions, normalizedGroupBy, startOfMonth, today);

            return new TransactionRecapResponse
            {
                Status = "success",
                Period = normalizedGroupBy,
                StartDate = startOfMonth,
                EndDate = today,
                Data = groupedData
            };
        }

        private static TransactionResponse MapTransactionToResponse(Transaction transaction) => new TransactionResponse
        {
            Id = transaction.Id,
            Description = transaction.Description,
            Amount = transaction.Amount,
            Date = transaction.Date,
            IsIncome = transaction.IsIncome,
            UserId = transaction.UserId,
            CategoryId = transaction.CategoryId
        };

        private static IReadOnlyCollection<TransactionRecapItem> BuildGroupedRecap(
            IReadOnlyCollection<TransactionResponse> transactions,
            string groupBy,
            DateTime startOfMonth,
            DateTime endDate)
        {
            return groupBy switch
            {
                "week" => BuildWeeklyRecap(transactions, endDate),
                "month" => BuildMonthlyRecap(transactions, startOfMonth, endDate),
                _ => BuildDailyRecap(transactions)
            };
        }

        private static IReadOnlyCollection<TransactionRecapItem> BuildDailyRecap(IReadOnlyCollection<TransactionResponse> transactions)
        {
            // Grup berdasarkan tanggal kalender (ISO yyyy-MM-dd) agar cocok dengan kebutuhan grafik harian.
            return transactions
                .GroupBy(t => EnsureUtcDate(t.Date).Date)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var day = g.Key;
                    var items = g.ToList();
                    return new TransactionRecapItem
                    {
                        Label = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        PeriodStart = day,
                        PeriodEnd = day,
                        Transactions = items,
                        TotalAmount = items.Sum(t => t.Amount)
                    };
                })
                .ToList();
        }

        private static IReadOnlyCollection<TransactionRecapItem> BuildWeeklyRecap(
            IReadOnlyCollection<TransactionResponse> transactions,
            DateTime endDate)
        {
            // Gunakan Senin sebagai awal minggu agar konsisten dengan standar ISO-8601.
            return transactions
                .GroupBy(t => GetWeekStart(EnsureUtcDate(t.Date)))
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var weekStart = g.Key;
                    var weekEnd = weekStart.AddDays(6);
                    if (weekEnd > endDate)
                    {
                        weekEnd = endDate;
                    }

                    var items = g.ToList();
                    return new TransactionRecapItem
                    {
                        Label = $"Week of {weekStart:yyyy-MM-dd}",
                        PeriodStart = weekStart,
                        PeriodEnd = weekEnd,
                        Transactions = items,
                        TotalAmount = items.Sum(t => t.Amount)
                    };
                })
                .ToList();
        }

        private static IReadOnlyCollection<TransactionRecapItem> BuildMonthlyRecap(
            IReadOnlyCollection<TransactionResponse> transactions,
            DateTime startOfMonth,
            DateTime endDate)
        {
            // Jika belum ada transaksi, cukup kembalikan list kosong agar frontend bisa menampilkan state kosong.
            if (transactions.Count == 0)
            {
                return Array.Empty<TransactionRecapItem>();
            }

            var items = transactions.ToList();
            var total = items.Sum(t => t.Amount);

            return new List<TransactionRecapItem>
            {
                new TransactionRecapItem
                {
                    Label = endDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                    PeriodStart = startOfMonth,
                    PeriodEnd = endDate,
                    Transactions = items,
                    TotalAmount = total
                }
            };
        }

        private static string NormalizeGroupBy(string groupBy)
        {
            var normalized = string.IsNullOrWhiteSpace(groupBy)
                ? "day"
                : groupBy.Trim().ToLowerInvariant();

            return normalized switch
            {
                "day" or "week" or "month" => normalized,
                _ => "day"
            };
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var localDate = EnsureUtcDate(date).Date;
            var diff = (7 + (localDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            return localDate.AddDays(-diff);
        }

        private static DateTime EnsureUtcDate(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                _ => dateTime.ToUniversalTime()
            };
        }
    }
}
