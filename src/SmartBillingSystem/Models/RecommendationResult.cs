namespace SmartBillingSystem.Models;

public class RecommendationResult
{
    public string ProductName { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
}
