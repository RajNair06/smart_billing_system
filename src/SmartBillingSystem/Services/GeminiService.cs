using System.Text.Json;
using SmartBillingSystem.Models;

namespace SmartBillingSystem.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient httpClient, IConfiguration config, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _apiKey = config["Gemini:ApiKey"] ?? string.Empty;
        _model = config["Gemini:Model"] ?? "gemini-3.6-flash";
        _logger = logger;
    }

    public async Task<RecommendationResult> GetProductRecommendationAsync(string productName)
    {
        string prompt = $"Suggest 3 complementary products that go well with buying '{productName}'. " +
                        $"Return ONLY a JSON array of 3 product name strings, no markdown, no explanation. " +
                        $"Example: [\"Mouse pad\", \"USB hub\", \"Laptop stand\"]";

        var recommendations = await CallGeminiAsync(prompt);

        return new RecommendationResult
        {
            ProductName = productName,
            Recommendations = recommendations
        };
    }

    public async Task<List<string>> GetBillRecommendationsAsync(List<string> productNames)
    {
        string productList = string.Join(", ", productNames);
        string prompt = $"A customer is buying these items together: {productList}. " +
                        $"Suggest 3-5 additional products they might also need. " +
                        $"Return ONLY a JSON array of product name strings, no markdown, no explanation. " +
                        $"Example: [\"Cable organizer\", \"Screen protector\", \"Carrying case\"]";

        return await CallGeminiAsync(prompt);
    }

    public async Task<string> GetDebugResponseAsync(string prompt)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return "ERROR: API key is empty";

        var requestBody = BuildRequestBody(prompt);

        var response = await _httpClient.PostAsJsonAsync(
            $"v1beta/models/{_model}:generateContent?key={_apiKey}",
            requestBody);

        var rawContent = await response.Content.ReadAsStringAsync();
        return $"STATUS: {response.StatusCode}\nMODEL: {_model}\n\nRESPONSE:\n{rawContent}";
    }

    private object BuildRequestBody(string prompt)
    {
        return new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 256
            }
        };
    }

    private async Task<List<string>> CallGeminiAsync(string prompt)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Gemini API key not configured. Returning empty recommendations.");
            return new List<string>();
        }

        try
        {
            var requestBody = BuildRequestBody(prompt);

            var response = await _httpClient.PostAsJsonAsync(
                $"v1beta/models/{_model}:generateContent?key={_apiKey}",
                requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error {StatusCode}: {ErrorContent}", 
                    response.StatusCode, errorContent);
                return new List<string>();
            }

            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            var candidates = json!.RootElement.GetProperty("candidates");
            
            if (candidates.GetArrayLength() == 0)
            {
                _logger.LogWarning("Gemini returned empty candidates");
                return new List<string>();
            }

            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            string rawText = "[]";

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textProp))
                {
                    var text = textProp.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        rawText = text;
                        break;
                    }
                }
            }

            _logger.LogInformation("Gemini raw response: {RawText}", rawText);

            rawText = rawText.Replace("```json", "").Replace("```", "").Trim();

            var recommendations = JsonSerializer.Deserialize<List<string>>(rawText) ?? new();
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Gemini recommendations");
            return new List<string>();
        }
    }
}
