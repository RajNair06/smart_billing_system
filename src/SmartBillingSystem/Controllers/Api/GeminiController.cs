using Microsoft.AspNetCore.Mvc;
using SmartBillingSystem.Services;

namespace SmartBillingSystem.Controllers.Api;

[ApiController]
[Route("api/gemini")]
public class GeminiController : ControllerBase
{
    private readonly IGeminiService _gemini;

    public GeminiController(IGeminiService gemini)
    {
        _gemini = gemini;
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
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class RecommendRequest
{
    public string[] Products { get; set; } = Array.Empty<string>();
}
