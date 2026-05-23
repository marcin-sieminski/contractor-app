using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Enums;
using ContractorApp.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.API.Controllers;

public class ProjectsController : BaseApiController
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ProjectsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? clientId, CancellationToken ct)
    {
        var query = _db.Projects
            .Include(p => p.Client)
            .Where(p => p.DeletedAt == null && p.Client.UserId == _currentUser.UserId);

        if (clientId.HasValue)
            query = query.Where(p => p.ClientId == clientId.Value);

        var projects = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProjectDto(p.Id, p.ClientId, p.Name, p.HourlyRate, p.Currency.ToString(), p.IsActive))
            .ToListAsync(ct);

        return Ok(projects);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest req, CancellationToken ct)
    {
        var clientExists = await _db.Clients.AnyAsync(
            c => c.Id == req.ClientId && c.DeletedAt == null && c.UserId == _currentUser.UserId, ct);

        if (!clientExists)
            throw new DomainException($"Klient {req.ClientId} nie istnieje.");

        var project = new Project
        {
            ClientId = req.ClientId,
            Name = req.Name,
            HourlyRate = req.HourlyRate,
            Currency = Enum.Parse<Currency>(req.Currency, true)
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), new { id = project.Id },
            new ProjectDto(project.Id, project.ClientId, project.Name, project.HourlyRate, project.Currency.ToString(), project.IsActive));
    }

    public record CreateProjectRequest(Guid ClientId, string Name, decimal HourlyRate, string Currency = "PLN");
}
