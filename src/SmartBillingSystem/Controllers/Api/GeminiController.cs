using Microsoft.AspNetCore.Mvc;
using SmartBillingSystem.Services;

namespace SmartBillingSystem.Controllers.Api;

[ApiController]
[Route("api/gemini")]
public class GeminiController : ControllerBase
{
    private readonly IGeminiService _gemini;
    private readonly ILogger<GeminiController> _logger;

    public GeminiController(IGeminiService gemini, ILogger<GeminiController> logger)
    {
        _gemini = gemini;
        _logger = logger;
    }

    [HttpPost("recommend")]
    public async Task<IActionResult> Recommend([FromBody] RecommendRequest request)
    {
        if (request.Products == null || request.Products.Length == 0)
            return BadRequest("No products provided.");

        try
        {
            var recommendations = await _gemini.GetBillRecommendationsAsync(request.Products.ToList());
            return Ok(new { recommendations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for products: {Products}", string.Join(", ", request.Products));
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class RecommendRequest
{
    public string[] Products { get; set; } = Array.Empty<string>();
}
