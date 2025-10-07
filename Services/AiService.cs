using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Data;
using CatatanKeuanganDotnet.Dtos.Ai;
using CatatanKeuanganDotnet.Services.Interfaces;
using CatatanKeuanganDotnet.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace CatatanKeuanganDotnet.Services
{
    public class AiService : IAiService
    {
        private const int DefaultTransactionLimit = 250;
        private static readonly JsonSerializerOptions PromptSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private const string BaseSystemPrompt =
            "Kamu adalah asisten keuangan pribadi untuk aplikasi Catatan Keuangan. Jawab selalu dalam Bahasa Indonesia yang mudah dipahami, sertakan angka spesifik, dan fokus pada insight yang dapat ditindaklanjuti. Jika data tidak cukup, jelaskan keterbatasannya dengan sopan.";

        private readonly ApplicationDbContext _context;
        private readonly IAiClient _aiClient;

        public AiService(ApplicationDbContext context, IAiClient aiClient)
        {
            _context = context;
            _aiClient = aiClient;
        }

        public async Task<string> GenerateInsightsAsync(AiInsightRequest request, CancellationToken cancellationToken = default)
        {
            var rangeEnd = NormalizeEnd(request.To ?? DateTime.UtcNow);
            var rangeStart = NormalizeStart(request.From ?? rangeEnd.AddDays(-60));
            EnsureRange(rangeStart, rangeEnd);

            var transactions = await GetTransactionsAsync(request.UserId, rangeStart, rangeEnd, DefaultTransactionLimit, cancellationToken);
            if (transactions.Count == 0)
            {
                return "Belum ada transaksi pada rentang waktu tersebut, jadi belum ada insight yang bisa dibagikan. Tambahkan transaksi terlebih dahulu.";
            }

            var snapshot = BuildSnapshot(request.UserId, transactions, rangeStart, rangeEnd);
            var snapshotJson = JsonSerializer.Serialize(snapshot, PromptSerializerOptions);
            var weeklyComparison = BuildWeeklyComparison(transactions, rangeEnd);
            var weeklyJson = JsonSerializer.Serialize(weeklyComparison, PromptSerializerOptions);

            var userPrompt =
                "Buat maksimal tiga insight yang tajam berdasarkan data berikut. " +
                "Soroti perubahan besar, proyeksikan dampaknya, dan tulis ringkas dalam bentuk bullet. " +
                "Gunakan angka dan persentase bila tersedia.\n\n" +
                $"Statistik mingguan: ```json\n{weeklyJson}\n```\n\n" +
                $"Snapshot transaksi: ```json\n{snapshotJson}\n```";

            var messages = new[]
            {
                new AiMessage("system", BaseSystemPrompt + " Fokus pada perubahan pola dan risiko keuangan."),
                new AiMessage("user", userPrompt)
            };

            return await _aiClient.GenerateAsync(messages, cancellationToken);
        }

        public async Task<string> AnswerQuestionAsync(AiChatRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsFinanceRelated(request.Question))
            {
                return "Pertanyaan ini di luar konteks catatan keuangan pribadi. Silakan tanya hal yang berhubungan dengan transaksi, pemasukan, pengeluaran, tabungan, atau anggaran Anda.";
            }

            var rangeEnd = NormalizeEnd(DateTime.UtcNow);
            var rangeStart = NormalizeStart(rangeEnd.AddDays(-180));

            var transactions = await GetTransactionsAsync(request.UserId, rangeStart, rangeEnd, 400, cancellationToken);
            if (transactions.Count == 0)
            {
                return "Belum ada data transaksi yang bisa dijadikan dasar jawaban. Tambahkan transaksi dan coba lagi.";
            }

            var snapshot = BuildSnapshot(request.UserId, transactions, rangeStart, rangeEnd);
            var snapshotJson = JsonSerializer.Serialize(snapshot, PromptSerializerOptions);

            var userPrompt =
                $"Pertanyaan pengguna: \"{request.Question}\".\n" +
                "Jawab hanya berdasarkan data yang diberikan. Jika data tidak cukup menjawab secara pasti, beritahu pengguna dan berikan pendekatan alternatif.\n" +
                $"Gunakan data berikut: ```json\n{snapshotJson}\n```";

            var messages = new[]
            {
                new AiMessage("system", BaseSystemPrompt + " Jawab runtut dan sertakan langkah atau angka yang relevan."),
                new AiMessage("user", userPrompt)
            };

            return await _aiClient.GenerateAsync(messages, cancellationToken);
        }

        public async Task<string> GenerateRecommendationsAsync(AiRecommendationRequest request, CancellationToken cancellationToken = default)
        {
            var rangeEnd = NormalizeEnd(DateTime.UtcNow);
            var rangeStart = NormalizeStart(rangeEnd.AddDays(-120));

            var transactions = await GetTransactionsAsync(request.UserId, rangeStart, rangeEnd, DefaultTransactionLimit, cancellationToken);
            if (transactions.Count == 0)
            {
                return "Belum ada data transaksi yang bisa dianalisis untuk rekomendasi. Tambahkan transaksi terlebih dahulu.";
            }

            var snapshot = BuildSnapshot(request.UserId, transactions, rangeStart, rangeEnd);
            var snapshotJson = JsonSerializer.Serialize(snapshot, PromptSerializerOptions);

            var focusInstruction = string.IsNullOrWhiteSpace(request.Focus)
                ? string.Empty
                : $" Prioritaskan rekomendasi terkait \"{request.Focus}\".";

            var userPrompt =
                "Buat 3-4 rekomendasi finansial yang spesifik dan dapat ditindaklanjuti. " +
                "Selaraskan dengan pola transaksi pengguna dan sertakan estimasi dampak jika memungkinkan." +
                focusInstruction +
                $"\n\nData referensi: ```json\n{snapshotJson}\n```";

            var messages = new[]
            {
                new AiMessage("system", BaseSystemPrompt + " Pastikan rekomendasi realistis dan sesuai konteks pengguna."),
                new AiMessage("user", userPrompt)
            };

            return await _aiClient.GenerateAsync(messages, cancellationToken);
        }

        public async Task<string> GenerateDigestAsync(AiDigestRequest request, CancellationToken cancellationToken = default)
        {
            var referenceDateUtc = NormalizeStart((request.ReferenceDate ?? DateTime.UtcNow).Date);
            var isWeekly = request.Period.Equals("weekly", StringComparison.OrdinalIgnoreCase);
            var rangeStart = isWeekly ? referenceDateUtc.AddDays(-6) : referenceDateUtc;
            var rangeEnd = NormalizeEnd(referenceDateUtc.AddDays(1).AddTicks(-1));
            var label = isWeekly ? "Ringkasan mingguan" : "Ringkasan harian";

            EnsureRange(rangeStart, rangeEnd);

            var transactions = await GetTransactionsAsync(request.UserId, rangeStart, rangeEnd, 200, cancellationToken);
            if (transactions.Count == 0)
            {
                return $"{label}: belum ada transaksi yang tercatat pada periode ini.";
            }

            var snapshot = BuildSnapshot(request.UserId, transactions, rangeStart, rangeEnd);
            var snapshotJson = JsonSerializer.Serialize(snapshot, PromptSerializerOptions);

            var digestMetrics = BuildDigestMetrics(transactions, rangeStart, rangeEnd);
            var digestJson = JsonSerializer.Serialize(digestMetrics, PromptSerializerOptions);

            var userPrompt =
                $"{label} dengan gaya ringkas dan ramah. Tampilkan total pengeluaran, pemasukan, jumlah transaksi, dan transaksi terbesar. " +
                "Akhiri dengan satu kalimat ajakan atau tips singkat." +
                $"\n\nAngka utama: ```json\n{digestJson}\n```\n" +
                $"Data detail: ```json\n{snapshotJson}\n```";

            var messages = new[]
            {
                new AiMessage("system", BaseSystemPrompt + " Format ringkasan menggunakan kalimat pendek, maksimal 3 paragraf."),
                new AiMessage("user", userPrompt)
            };

            return await _aiClient.GenerateAsync(messages, cancellationToken);
        }

        private async Task<IReadOnlyList<TransactionView>> GetTransactionsAsync(
            string userId,
            DateTime fromInclusive,
            DateTime toInclusive,
            int limit,
            CancellationToken cancellationToken)
        {
            var utcFrom = NormalizeStart(fromInclusive);
            var utcTo = NormalizeEnd(toInclusive);

            var rawTransactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.Date >= utcFrom && t.Date <= utcTo)
                .OrderByDescending(t => t.Date)
                .Take(limit)
                .Select(t => new
                {
                    t.Date,
                    t.Amount,
                    t.IsIncome,
                    CategoryName = t.Category != null ? t.Category.Name : null,
                    t.Description
                })
                .ToListAsync(cancellationToken);

            return rawTransactions
                .Select(t => new TransactionView(
                    EnsureUtc(t.Date),
                    t.Amount,
                    t.IsIncome,
                    t.CategoryName,
                    t.Description))
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        private static object BuildSnapshot(string userId, IReadOnlyList<TransactionView> transactions, DateTime rangeStart, DateTime rangeEnd)
        {
            var expenses = transactions.Where(t => !t.IsIncome).ToList();
            var incomes = transactions.Where(t => t.IsIncome).ToList();

            var totalExpense = expenses.Sum(t => t.Amount);
            var totalIncome = incomes.Sum(t => t.Amount);
            var netIncome = totalIncome - totalExpense;
            var totalDays = Math.Max(1, (int)Math.Ceiling((rangeEnd.Date - rangeStart.Date).TotalDays) + 1);

            var topExpenseCategories = expenses
                .Where(t => !string.IsNullOrWhiteSpace(t.CategoryName))
                .GroupBy(t => t.CategoryName!)
                .Select(group => new
                {
                    category = group.Key,
                    total = group.Sum(item => item.Amount),
                    count = group.Count()
                })
                .OrderByDescending(item => item.total)
                .Take(5)
                .ToList();

            var topIncomeCategories = incomes
                .Where(t => !string.IsNullOrWhiteSpace(t.CategoryName))
                .GroupBy(t => t.CategoryName!)
                .Select(group => new
                {
                    category = group.Key,
                    total = group.Sum(item => item.Amount),
                    count = group.Count()
                })
                .OrderByDescending(item => item.total)
                .Take(5)
                .ToList();

            var monthlyBreakdown = transactions
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(group => new
                {
                    month = $"{group.Key.Year:D4}-{group.Key.Month:D2}",
                    income = group.Where(t => t.IsIncome).Sum(t => t.Amount),
                    expense = group.Where(t => !t.IsIncome).Sum(t => t.Amount)
                })
                .OrderBy(item => item.month)
                .ToList();

            var recentTransactions = transactions
                .OrderByDescending(t => t.Date)
                .Take(60)
                .Select(t => MapTransaction(t))
                .ToList();

            return new
            {
                userId,
                range = new
                {
                    from = rangeStart.ToString("O", CultureInfo.InvariantCulture),
                    to = rangeEnd.ToString("O", CultureInfo.InvariantCulture)
                },
                totals = new
                {
                    income = totalIncome,
                    expense = totalExpense,
                    net = netIncome,
                    count = transactions.Count
                },
                averages = new
                {
                    dailyExpense = totalExpense / totalDays,
                    dailyIncome = totalIncome / totalDays
                },
                topExpenseCategories,
                topIncomeCategories,
                monthlyBreakdown,
                largestExpense = MapTransaction(expenses.OrderByDescending(t => t.Amount).FirstOrDefault()),
                largestIncome = MapTransaction(incomes.OrderByDescending(t => t.Amount).FirstOrDefault()),
                recentTransactions
            };
        }

        private static object BuildWeeklyComparison(IReadOnlyList<TransactionView> transactions, DateTime referenceEnd)
        {
            var end = referenceEnd.Date;
            var currentStart = end.AddDays(-6);
            var previousEnd = currentStart.AddDays(-1);
            var previousStart = previousEnd.AddDays(-6);

            var currentExpense = transactions
                .Where(t => !t.IsIncome && t.Date.Date >= currentStart && t.Date.Date <= end)
                .Sum(t => t.Amount);

            var previousExpense = transactions
                .Where(t => !t.IsIncome && t.Date.Date >= previousStart && t.Date.Date <= previousEnd)
                .Sum(t => t.Amount);

            var difference = currentExpense - previousExpense;
            var percentage = previousExpense == 0 ? null : (double?)((difference / previousExpense) * 100);

            return new
            {
                currentWeek = new { start = currentStart.ToString("O", CultureInfo.InvariantCulture), end = end.ToString("O", CultureInfo.InvariantCulture), expense = currentExpense },
                previousWeek = new { start = previousStart.ToString("O", CultureInfo.InvariantCulture), end = previousEnd.ToString("O", CultureInfo.InvariantCulture), expense = previousExpense },
                difference,
                percentageChange = percentage
            };
        }

        private static object BuildDigestMetrics(IReadOnlyList<TransactionView> transactions, DateTime rangeStart, DateTime rangeEnd)
        {
            var totalExpense = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
            var totalIncome = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            var largestExpense = transactions.Where(t => !t.IsIncome).OrderByDescending(t => t.Amount).FirstOrDefault();
            var largestIncome = transactions.Where(t => t.IsIncome).OrderByDescending(t => t.Amount).FirstOrDefault();

            var topCategory = transactions
                .Where(t => !t.IsIncome && !string.IsNullOrWhiteSpace(t.CategoryName))
                .GroupBy(t => t.CategoryName!)
                .Select(group => new
                {
                    category = group.Key,
                    total = group.Sum(item => item.Amount)
                })
                .OrderByDescending(item => item.total)
                .FirstOrDefault();

            return new
            {
                period = new
                {
                    from = rangeStart.ToString("O", CultureInfo.InvariantCulture),
                    to = rangeEnd.ToString("O", CultureInfo.InvariantCulture)
                },
                totals = new
                {
                    income = totalIncome,
                    expense = totalExpense,
                    net = totalIncome - totalExpense,
                    transactions = transactions.Count
                },
                topCategory,
                largestExpense = MapTransaction(largestExpense),
                largestIncome = MapTransaction(largestIncome)
            };
        }

        private static object? MapTransaction(TransactionView? transaction)
        {
            if (transaction == null)
            {
                return null;
            }

            return new
            {
                date = transaction.Date.ToString("O", CultureInfo.InvariantCulture),
                amount = transaction.Amount,
                type = transaction.IsIncome ? "income" : "expense",
                category = transaction.CategoryName,
                description = transaction.Description
            };
        }

        private static DateTime NormalizeStart(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private static DateTime NormalizeEnd(DateTime value)
        {
            var normalized = NormalizeStart(value);
            return normalized.Kind == DateTimeKind.Utc ? normalized : DateTime.SpecifyKind(normalized, DateTimeKind.Utc);
        }

        private static bool IsFinanceRelated(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return false;
            }

            var normalized = question.Trim().ToLowerInvariant();
            foreach (var keyword in FinanceKeywords)
            {
                if (normalized.Contains(keyword, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureRange(DateTime from, DateTime to)
        {
            if (from > to)
            {
                throw new ArgumentException("Tanggal awal tidak boleh lebih besar dari tanggal akhir.");
            }
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private sealed record TransactionView(
            DateTime Date,
            decimal Amount,
            bool IsIncome,
            string? CategoryName,
            string? Description);

        private static readonly string[] FinanceKeywords =
        {
            "pengeluaran",
            "pemasukan",
            "transaksi",
            "tabungan",
            "budget",
            "anggaran",
            "saldo",
            "keuangan",
            "hemat",
            "invest",
            "utang",
            "hutang",
            "cicilan",
            "dompet",
            "uang",
            "laporan",
            "summary",
            "rekap",
            "spending",
            "expense",
            "income",
            "saving",
            "budgeting"
        };
    }
}
