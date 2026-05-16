using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.API.Controllers;

public class ProjectsController : BaseApiController
{
    private readonly IApplicationDbContext _db;

    public ProjectsController(IApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? clientId, CancellationToken ct)
    {
        var query = _db.Projects
            .Include(p => p.Client)
            .Where(p => p.DeletedAt == null);

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
