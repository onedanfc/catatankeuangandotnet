using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Dtos.Categories;
using CatatanKeuanganDotnet.Models;

namespace CatatanKeuanganDotnet.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IReadOnlyCollection<Category>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Category> CreateAsync(CategoryRequest request, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(int id, CategoryRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}