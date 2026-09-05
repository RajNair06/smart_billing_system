using SmartBillingSystem.Models;

namespace SmartBillingSystem.Services;

public interface IGeminiService
{
    Task<RecommendationResult> GetProductRecommendationAsync(string productName);
    Task<List<string>> GetBillRecommendationsAsync(List<string> productNames);
    Task<string> GetDebugResponseAsync(string prompt);
}
