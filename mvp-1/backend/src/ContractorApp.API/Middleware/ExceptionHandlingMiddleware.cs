using FluentValidation;
using ContractorApp.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ValidationException ex)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsJsonAsync(new ValidationProblemDetails(
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }
        catch (DomainException ex)
        {
            ctx.Response.StatusCode = 422;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 422, Title = "Błąd domeny", Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 500, Title = "Błąd serwera", Detail = "Wystąpił nieoczekiwany błąd."
            });
        }
    }
}
