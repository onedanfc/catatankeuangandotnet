using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Options;
using CatatanKeuanganDotnet.Services.Interfaces;
using CatatanKeuanganDotnet.Services.Models;
using Microsoft.Extensions.Options;

namespace CatatanKeuanganDotnet.Services
{
    public class AiClient : IAiClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient;
        private readonly IOptionsMonitor<AiOptions> _optionsMonitor;

        public AiClient(HttpClient httpClient, IOptionsMonitor<AiOptions> optionsMonitor)
        {
            _httpClient = httpClient;
            _optionsMonitor = optionsMonitor;
        }

        private AiOptions Options => _optionsMonitor.CurrentValue;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Options.ApiKey) &&
            !string.IsNullOrWhiteSpace(Options.BaseUrl) &&
            !string.IsNullOrWhiteSpace(Options.Model);

        private string Provider =>
            string.IsNullOrWhiteSpace(Options.Provider)
                ? "gemini"
                : Options.Provider.Trim().ToLowerInvariant();

        public async Task<string> GenerateAsync(IEnumerable<AiMessage> messages, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("Konfigurasi AI belum lengkap. Silakan set API key dan model terlebih dahulu.");
            }
            var provider = Provider;

            return provider switch
            {
                "gemini" or "google" or "googleai" or "google-ai" or "google_gemini" => await GenerateWithGeminiAsync(messages, cancellationToken),
                _ => await GenerateWithOpenAiAsync(messages, cancellationToken)
            };
        }

        private async Task<string> GenerateWithOpenAiAsync(IEnumerable<AiMessage> messages, CancellationToken cancellationToken)
        {
            var payload = new ChatCompletionRequest
            {
                Model = Options.Model,
                Messages = messages.Select(message => new ChatMessage
                {
                    Role = message.Role,
                    Content = message.Content
                }).ToArray(),
                Temperature = Options.Temperature,
                MaxTokens = Options.MaxTokens > 0 ? Options.MaxTokens : null
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, BuildOpenAiEndpointUri());
            ApplyHeaders(request);

            var json = JsonSerializer.Serialize(payload, SerializerOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Gagal memanggil penyedia AI (status {(int)response.StatusCode}). {response.ReasonPhrase}",
                    null,
                    response.StatusCode);
            }

            var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseText, SerializerOptions);
            var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Respons AI kosong atau tidak dapat dibaca.");
            }

            return content.Trim();
        }

        private async Task<string> GenerateWithGeminiAsync(IEnumerable<AiMessage> messages, CancellationToken cancellationToken)
        {
            var systemInstructions = messages
                .Where(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Content)
                .ToArray();

            var conversation = messages
                .Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
                .Select(m => new GeminiContent
                {
                    Role = MapRole(m.Role),
                    Parts = new[]
                    {
                        new GeminiPart { Text = m.Content }
                    }
                })
                .ToArray();

            var payload = new GeminiRequest
            {
                Contents = conversation.Length > 0 ? conversation : new[]
                {
                    new GeminiContent
                    {
                        Role = "user",
                        Parts = new[] { new GeminiPart { Text = string.Empty } }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = Options.Temperature,
                    MaxOutputTokens = Options.MaxTokens > 0 ? Options.MaxTokens : null
                }
            };

            if (systemInstructions.Length > 0)
            {
                payload.SystemInstruction = new GeminiContent
                {
                    Parts = new[]
                    {
                        new GeminiPart { Text = string.Join("\n\n", systemInstructions) }
                    }
                };
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, BuildGeminiEndpointUri());
            ApplyHeaders(request);

            var json = JsonSerializer.Serialize(payload, SerializerOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Gagal memanggil Gemini (status {(int)response.StatusCode}). {response.ReasonPhrase}",
                    null,
                    response.StatusCode);
            }

            var completion = JsonSerializer.Deserialize<GeminiResponse>(responseText, SerializerOptions);
            var content = completion?.Candidates?
                .SelectMany(candidate => candidate.Content?.Parts ?? Array.Empty<GeminiPart>())
                .Select(part => part.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Respons Gemini kosong atau tidak dapat dibaca.");
            }

            return content.Trim();
        }

        private Uri BuildOpenAiEndpointUri()
        {
            var baseUrl = Options.BaseUrl?.Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("Base URL AI tidak valid.");
            }

            if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
            {
                baseUrl += "/";
            }

            return new Uri(new Uri(baseUrl, UriKind.Absolute), "chat/completions");
        }

        private Uri BuildGeminiEndpointUri()
        {
            var baseUrl = Options.BaseUrl?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("Base URL Gemini tidak valid.");
            }

            var model = Options.Model?.Trim();
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new InvalidOperationException("Model Gemini belum diatur.");
            }

            var path = $"models/{model}:generateContent";
            return new Uri($"{baseUrl}/{path}");
        }

        private void ApplyHeaders(HttpRequestMessage request)
        {
            var options = Options;

            if (!string.IsNullOrWhiteSpace(options.ApiKeyHeaderName) &&
                !string.Equals(options.ApiKeyHeaderName, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Remove(options.ApiKeyHeaderName);
                request.Headers.TryAddWithoutValidation(options.ApiKeyHeaderName, options.ApiKey);
            }
            else if (options.UseBearerPrefix)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            }
            else
            {
                request.Headers.TryAddWithoutValidation("Authorization", options.ApiKey);
            }

            if (!string.IsNullOrWhiteSpace(options.Organization))
            {
                request.Headers.Remove("OpenAI-Organization");
                request.Headers.TryAddWithoutValidation("OpenAI-Organization", options.Organization);
            }
        }

        private static string MapRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return "user";
            }

            return role.ToLowerInvariant() switch
            {
                "assistant" or "model" => "model",
                _ => "user"
            };
        }

        private sealed class ChatCompletionRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; init; } = string.Empty;

            [JsonPropertyName("messages")]
            public ChatMessage[] Messages { get; init; } = Array.Empty<ChatMessage>();

            [JsonPropertyName("temperature")]
            public double Temperature { get; init; }

            [JsonPropertyName("max_tokens")]
            public int? MaxTokens { get; init; }
        }

        private sealed class ChatCompletionResponse
        {
            [JsonPropertyName("choices")]
            public ChatChoice[]? Choices { get; init; }
        }

        private sealed class ChatChoice
        {
            [JsonPropertyName("message")]
            public ChatMessage? Message { get; init; }
        }

        private sealed class ChatMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; init; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; init; } = string.Empty;
        }

        private sealed class GeminiRequest
        {
            [JsonPropertyName("system_instruction")]
            public GeminiContent? SystemInstruction { get; set; }

            [JsonPropertyName("contents")]
            public GeminiContent[] Contents { get; set; } = Array.Empty<GeminiContent>();

            [JsonPropertyName("generation_config")]
            public GeminiGenerationConfig? GenerationConfig { get; set; }
        }

        private sealed class GeminiGenerationConfig
        {
            [JsonPropertyName("temperature")]
            public double? Temperature { get; set; }

            [JsonPropertyName("max_output_tokens")]
            public int? MaxOutputTokens { get; set; }
        }

        private sealed class GeminiContent
        {
            [JsonPropertyName("role")]
            public string? Role { get; set; }

            [JsonPropertyName("parts")]
            public GeminiPart[] Parts { get; set; } = Array.Empty<GeminiPart>();
        }

        private sealed class GeminiPart
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }

        private sealed class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public GeminiCandidate[]? Candidates { get; set; }
        }

        private sealed class GeminiCandidate
        {
            [JsonPropertyName("content")]
            public GeminiContent? Content { get; set; }
        }
    }
}
