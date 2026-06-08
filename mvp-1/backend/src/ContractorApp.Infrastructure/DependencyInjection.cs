using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Infrastructure.Identity;
using ContractorApp.Infrastructure.Persistence;
using ContractorApp.Infrastructure.Services;
using ContractorApp.Infrastructure.Services.Auth;
using ContractorApp.Infrastructure.Services.CompanyLookup;
using ContractorApp.Infrastructure.Services.KSeF;
using ContractorApp.Infrastructure.Services.Nbp;
using ContractorApp.Infrastructure.Services.Ocr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ContractorApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured.");

        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseNpgsql(connectionString, o => o.MigrationsAssembly("ContractorApp.Infrastructure")));

        services.AddDbContextFactory<ApplicationDbContext>(opts =>
            opts.UseNpgsql(connectionString), ServiceLifetime.Scoped);

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IInvoiceNumberService, InvoiceNumberService>();

        // Identity (user management only, no cookie auth)
        services.AddIdentityCore<ApplicationUser>(opts =>
        {
            opts.Password.RequireDigit = false;
            opts.Password.RequiredLength = 8;
            opts.Password.RequireNonAlphanumeric = false;
            opts.Password.RequireUppercase = false;
            opts.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Current user service
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // KSeF
        services.Configure<KsefOptions>(configuration.GetSection("KSeF").Bind);
        services.AddSingleton<KsefAuthService>();
        services.AddHttpClient<KsefAuthService>();
        services.AddHttpClient<KsefService>();
        services.AddScoped<IKsefService, KsefService>();
        services.AddScoped<IKsefXmlBuilder, KsefXmlBuilder>();

        // NBP
        services.AddHttpClient<NbpService>();
        services.AddScoped<INbpService, NbpService>();

        // Company Lookup
        services.AddHttpClient<CompanyLookupService>();
        services.AddScoped<ICompanyLookupService, CompanyLookupService>();

        // OCR paragonów/faktur (Claude Vision + fallback Ollama)
        services.Configure<ReceiptOcrOptions>(configuration.GetSection("ReceiptOcr").Bind);
        services.AddHttpClient<IReceiptExtractionService, ReceiptExtractionService>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<ReceiptOcrOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        return services;
    }
}
