using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Dtos.Ai;

namespace CatatanKeuanganDotnet.Services.Interfaces
{
    public interface IAiService
    {
        Task<string> GenerateInsightsAsync(AiInsightRequest request, CancellationToken cancellationToken = default);

        Task<string> AnswerQuestionAsync(AiChatRequest request, CancellationToken cancellationToken = default);

        Task<string> GenerateRecommendationsAsync(AiRecommendationRequest request, CancellationToken cancellationToken = default);

        Task<string> GenerateDigestAsync(AiDigestRequest request, CancellationToken cancellationToken = default);
    }
}
