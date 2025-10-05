using System;
using System.Threading;
using System.Threading.Tasks;

namespace CatatanKeuanganDotnet.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendPasswordResetTokenAsync(string email, string token, DateTime expiresAt, CancellationToken cancellationToken = default);
    }
}
