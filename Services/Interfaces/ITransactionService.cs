using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Dtos.Transactions;
using CatatanKeuanganDotnet.Models;

namespace CatatanKeuanganDotnet.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<IReadOnlyCollection<Transaction>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Transaction> CreateAsync(TransactionRequest request, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(int id, TransactionRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Menghasilkan rekap transaksi bulan berjalan sesuai mode pengelompokan yang diminta frontend.
        /// </summary>
        Task<TransactionRecapResponse> GetMonthlyRecapAsync(string userId, string groupBy, CancellationToken cancellationToken = default);
    }
}
