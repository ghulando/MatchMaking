using Microsoft.AspNetCore.Mvc;
using MatchMaking.Service.Services;

namespace MatchMaking.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class MatchController(IMatchService matchService, ILogger<MatchController> logger): ControllerBase
{
    [HttpPost("search")]
    public async Task<IActionResult> SearchMatch([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Match search request with invalid userId");
            return BadRequest("UserId is required");
        }

        try
        {
            // Check rate limit
            if (!await matchService.CanMakeRequestAsync(userId))
            {
                return BadRequest("Rate limit exceeded. Max 1 request per 100ms");
            }

            // Send match request
            await matchService.RequestMatchAsync(userId);
            
            logger.LogInformation("Match search request accepted for user {UserId}", userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing match search for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("info/{userId}")]
    public async Task<IActionResult> GetMatchInfo(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Match info request with invalid userId");
            return BadRequest("UserId is required");
        }

        try
        {
            var matchInfo = await matchService.GetMatchInfoAsync(userId);
            
            if (matchInfo == null)
            {
                logger.LogInformation("No match found for user {UserId}", userId);
                return NotFound("No match found for this user");
            }

            logger.LogInformation("Match info retrieved for user {UserId}", userId);
            return Ok(matchInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving match info for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}