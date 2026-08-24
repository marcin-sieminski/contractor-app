using ContractorApp.API.Services;
using ContractorApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;

    public AuthController(UserManager<ApplicationUser> userManager, TokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public record RegisterRequest(string Email, string Password, string ConfirmPassword);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Token, string Email, DateTimeOffset ExpiresAt, string? DisplayName);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return BadRequest(new { error = "Hasła nie są zgodne." });

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        var (token, expiresAt) = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Email!, expiresAt, user.DisplayName));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { error = "Nieprawidłowy email lub hasło." });

        var (token, expiresAt) = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Email!, expiresAt, user.DisplayName));
    }
}
