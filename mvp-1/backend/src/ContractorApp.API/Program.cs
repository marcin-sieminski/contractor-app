using System.Text;
using ContractorApp.API.Auth;
using ContractorApp.API.McpTools;
using ContractorApp.API.Middleware;
using ContractorApp.API.Services;
using ContractorApp.API.Services.Ai;
using ContractorApp.Application;
using ContractorApp.Infrastructure;
using ContractorApp.Infrastructure.Persistence;
using ContractorApp.Infrastructure.Services.Ollama;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<TimeTools>()
    .WithTools<InvoiceTools>()
    .WithTools<ClientTools>()
    .WithTools<ExpenseTools>()
    .WithTools<DeadlineTools>()
    .WithTools<TaxTools>();

// Lokalny asystent AI (Ollama + reflection-based tool registry)
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
builder.Services.AddHttpClient<OllamaService>((sp, http) =>
{
    var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    http.BaseAddress = new Uri(opts.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
});

// Klasy narzędzi MCP muszą być w głównym DI, żeby McpToolRegistry mógł je rezolwować w request scope
builder.Services.AddScoped<TimeTools>();
builder.Services.AddScoped<InvoiceTools>();
builder.Services.AddScoped<ClientTools>();
builder.Services.AddScoped<ExpenseTools>();
builder.Services.AddScoped<DeadlineTools>();
builder.Services.AddScoped<TaxTools>();

builder.Services.AddSingleton<McpToolRegistry>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "ContractorApp API", Version = "v1" }));

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .WithOrigins("http://localhost:5173", "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// Auto-migrate on startup in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapMcp("/mcp")
    .RequireAuthorization(policy => policy
        .AddAuthenticationSchemes("ApiKey")
        .RequireAuthenticatedUser());

app.Run();
