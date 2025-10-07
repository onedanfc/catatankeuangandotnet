using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Services.Models;

namespace CatatanKeuanganDotnet.Services.Interfaces
{
    public interface IAiClient
    {
        bool IsConfigured { get; }

        Task<string> GenerateAsync(IEnumerable<AiMessage> messages, CancellationToken cancellationToken = default);
    }
}
