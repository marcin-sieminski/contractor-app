using Microsoft.AspNetCore.Identity;

namespace ContractorApp.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? McpApiKey { get; set; }
}
