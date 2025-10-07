using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Dtos.Ai;
using CatatanKeuanganDotnet.Dtos.Common;
using CatatanKeuanganDotnet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CatatanKeuanganDotnet.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;
        private readonly IAiClient _aiClient;

        public AiController(IAiService aiService, IAiClient aiClient)
        {
            _aiService = aiService;
            _aiClient = aiClient;
        }

        [HttpPost("insights")]
        public async Task<IActionResult> GenerateInsights([FromBody] AiInsightRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data permintaan tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            var configurationResult = EnsureAiConfigured();
            if (configurationResult != null)
            {
                return configurationResult;
            }

            try
            {
                var result = await _aiService.GenerateInsightsAsync(request, cancellationToken);
                return Ok(ApiResponse<AiMessageResponse>.Succeeded(
                    new AiMessageResponse { Content = result },
                    "Insight berhasil dibuat."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<AiMessageResponse>.Failure(
                    ex.Message,
                    StatusCodes.Status400BadRequest));
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, ApiResponse<AiMessageResponse>.Failure(
                    $"Gagal menghubungi penyedia AI: {ex.Message}",
                    StatusCodes.Status502BadGateway));
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<AiMessageResponse>.Failure(
                    ex.Message,
                    StatusCodes.Status503ServiceUnavailable));
            }
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data permintaan tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            var configurationResult = EnsureAiConfigured();
            if (configurationResult != null)
            {
                return configurationResult;
            }

            try
            {
                var result = await _aiService.AnswerQuestionAsync(request, cancellationToken);
                return Ok(ApiResponse<AiMessageResponse>.Succeeded(
                    new AiMessageResponse { Content = result },
                    "Jawaban berhasil dibuat."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<AiMessageResponse>.Failure(
                    ex.Message,
                    StatusCodes.Status400BadRequest));
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, ApiResponse<AiMessageResponse>.Failure(
                    $"Gagal menghubungi penyedia AI: {ex.Message}",
                    StatusCodes.Status502BadGateway));
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<AiMessageResponse>.Failure(
                    ex.Message,
                    StatusCodes.Status503ServiceUnavailable));
            }
        }

        [HttpPost("recommendations")]
        public async Task<IActionResult> Recommendations([FromBody] AiRecommendationRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data permintaan tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            var configurationResult = EnsureAiConfigured();
            if (configurationResult != null)
            {
                return configurationResult;
            }

            try
            {
                var result = await _aiService.GenerateRecommendationsAsync(request, cancellationToken);
                return Ok(ApiResponse<AiMessageResponse>.Succeeded(
                    new AiMessageResponse { Content = result },
                    "Rekomendasi berhasil dibuat."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<AiMessageResponse>.Failure(
                    ex.Message,
                    StatusCodes.Status400BadRequest));
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, ApiResponse<AiMessageResponse>.Failure(
                    $"Gagal menghubungi penyedia AI: {ex.Message}",
                    StatusCodes.Status502BadGateway));
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<AiMessageResponse>.Failure(
                    ex.Message,
                    StatusCodes.Status503ServiceUnavailable));
            }
        }

        [HttpPost("digest")]
        public async Task<IActionResult> Digest([FromBody] AiDigestRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data permintaan tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            var configurationResult = EnsureAiConfigured();
            if (configurationResult != null)
            {
                return configurationResult;
            }

            try
            {
                var result = await _aiService.GenerateDigestAsync(request, cancellationToken);
                return Ok(ApiResponse<AiMessageResponse>.Succeeded(
                    new AiMessageResponse { Content = result },
                    "Ringkasan berhasil dibuat."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<AiMessageResponse>.Failure(
                    ex.Message,
                    StatusCodes.Status400BadRequest));
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, ApiResponse<AiMessageResponse>.Failure(
                    $"Gagal menghubungi penyedia AI: {ex.Message}",
                    StatusCodes.Status502BadGateway));
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<AiMessageResponse>.Failure(
                    ex.Message,
                    StatusCodes.Status503ServiceUnavailable));
            }
        }

        private IActionResult? EnsureAiConfigured()
        {
            if (_aiClient.IsConfigured)
            {
                return null;
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<AiMessageResponse>.Failure(
                "Layanan AI belum dikonfigurasi. Setel API key, base URL, dan model terlebih dahulu.",
                StatusCodes.Status503ServiceUnavailable));
        }
    }
}
