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
            _logger.LogError("Gemini API key is not configured");
            throw new Exception("Gemini API key is not configured");
        }

        var requestBody = BuildRequestBody(prompt);

        var response = await _httpClient.PostAsJsonAsync(
            $"v1beta/models/{_model}:generateContent?key={_apiKey}",
            requestBody);

        var rawContent = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Gemini API response status: {StatusCode}", response.StatusCode);
        _logger.LogDebug("Gemini API raw response: {Response}", rawContent.Substring(0, Math.Min(500, rawContent.Length)));

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, rawContent);
            throw new Exception($"Gemini API {response.StatusCode}: {rawContent}");
        }

        // Check if response is HTML (starts with '<')
        if (rawContent.TrimStart().StartsWith("<"))
        {
            _logger.LogError("Gemini API returned HTML instead of JSON: {Response}", rawContent.Substring(0, Math.Min(200, rawContent.Length)));
            throw new Exception("Gemini API returned invalid response (HTML instead of JSON)");
        }

        JsonDocument json;
        try
        {
            json = JsonDocument.Parse(rawContent);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini response as JSON. Response: {Response}", rawContent);
            throw new Exception($"Failed to parse Gemini response: {ex.Message}");
        }

        var candidates = json.RootElement.GetProperty("candidates");

        if (candidates.GetArrayLength() == 0)
        {
            _logger.LogWarning("Gemini returned empty candidates");
            throw new Exception("Gemini returned empty candidates");
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

        rawText = rawText.Replace("```json", "").Replace("```", "").Trim();

        var recommendations = JsonSerializer.Deserialize<List<string>>(rawText) ?? new();
        return recommendations;
    }
}
