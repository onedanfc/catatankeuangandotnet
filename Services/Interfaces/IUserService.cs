using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Dtos.Auth;
using CatatanKeuanganDotnet.Models;

namespace CatatanKeuanganDotnet.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<User?> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> SendPasswordResetTokenAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    }
}
