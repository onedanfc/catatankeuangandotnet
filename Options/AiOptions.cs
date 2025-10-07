namespace CatatanKeuanganDotnet.Options
{
    public class AiOptions
    {
        public string Provider { get; set; } = "gemini";

        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

        public string ApiKey { get; set; } = string.Empty;

        public string Model { get; set; } = "gemini-2.0-flash";

        public double Temperature { get; set; } = 0.2;

        public int MaxTokens { get; set; } = 1024;

        public string? Organization { get; set; }

        public string ApiKeyHeaderName { get; set; } = "X-goog-api-key";

        public bool UseBearerPrefix { get; set; } = false;
    }
}
