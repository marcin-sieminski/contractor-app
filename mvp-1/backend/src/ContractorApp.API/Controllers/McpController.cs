using ContractorApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/mcp")]
public class McpController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    public record ApiKeyResponse(string Key, string ConfigSnippet);

    [HttpPost("api-key/generate")]
    public async Task<IActionResult> GenerateApiKey()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var key = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        user.McpApiKey = key;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Problem("Failed to save API key.");

        var snippet = $$"""
            {
              "mcpServers": {
                "contractor-app": {
                  "url": "http://localhost:5000/mcp",
                  "headers": { "Authorization": "ApiKey {{key}}" }
                }
              }
            }
            """;

        return Ok(new ApiKeyResponse(key, snippet));
    }

    [HttpDelete("api-key")]
    public async Task<IActionResult> RevokeApiKey()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        user.McpApiKey = null;
        await userManager.UpdateAsync(user);
        return NoContent();
    }

    [HttpGet("api-key/status")]
    public async Task<IActionResult> GetKeyStatus()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        return Ok(new { HasKey = user.McpApiKey is not null });
    }
}
