using Microsoft.AspNetCore.Mvc;
using MatchMaking.Application.UseCases;

namespace MatchMaking.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class MatchController : ControllerBase
{
    private readonly RequestMatchUseCase _requestMatchUseCase;
    private readonly GetMatchInfoUseCase _getMatchInfoUseCase;
    private readonly ILogger<MatchController> _logger;

    public MatchController(
        RequestMatchUseCase requestMatchUseCase,
        GetMatchInfoUseCase getMatchInfoUseCase,
        ILogger<MatchController> logger)
    {
        _requestMatchUseCase = requestMatchUseCase;
        _getMatchInfoUseCase = getMatchInfoUseCase;
        _logger = logger;
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchMatch([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Match search request with invalid userId");
            return BadRequest("UserId is required");
        }

        try
        {
            var result = await _requestMatchUseCase.ExecuteAsync(userId);
            
            if (!result)
            {
                return BadRequest("Rate limit exceeded. Max 1 request per 100ms");
            }
            
            _logger.LogInformation("Match search request accepted for user {UserId}", userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing match search for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("info/{userId}")]
    public async Task<IActionResult> GetMatchInfo(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Match info request with invalid userId");
            return BadRequest("UserId is required");
        }

        try
        {
            var matchInfo = await _getMatchInfoUseCase.ExecuteAsync(userId);
            
            if (matchInfo == null)
            {
                _logger.LogInformation("No match found for user {UserId}", userId);
                return NotFound("No match found for this user");
            }

            _logger.LogInformation("Match info retrieved for user {UserId}", userId);
            return Ok(matchInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving match info for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}