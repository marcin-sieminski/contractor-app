using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Infrastructure.Persistence;
using ContractorApp.Infrastructure.Services;
using ContractorApp.Infrastructure.Services.CompanyLookup;
using ContractorApp.Infrastructure.Services.KSeF;
using ContractorApp.Infrastructure.Services.Nbp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
