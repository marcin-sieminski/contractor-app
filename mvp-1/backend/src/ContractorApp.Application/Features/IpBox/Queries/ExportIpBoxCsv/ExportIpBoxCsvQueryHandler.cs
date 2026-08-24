using System.Text;
using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.IpBox.Queries.ExportIpBoxCsv;

public class ExportIpBoxCsvQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<ExportIpBoxCsvQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportIpBoxCsvQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var year = request.Year;

        var query = db.TimeEntries
            .Include(te => te.Project)
            .ThenInclude(p => p.Client)
            .Where(te => te.Project.Client.UserId == userId
                      && te.StartedAt.Year == year
                      && te.StoppedAt != null
                      && (te.IsIpWork || te.Project.IsIpProject));

        if (request.Month.HasValue)
            query = query.Where(te => te.StartedAt.Month == request.Month.Value);

        var entries = await query
            .OrderBy(te => te.StartedAt)
            .Select(te => new
            {
                te.StartedAt,
                te.StoppedAt,
                te.DurationMinutes,
                te.Description,
                te.IsIpWork,
                te.IpWorkDescription,
                ProjectName = te.Project.Name,
                IsIpProject = te.Project.IsIpProject
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Data,Projekt,Godziny,Opis zadania,Opis IP Box,Typ IP");

        foreach (var e in entries)
        {
            var date = e.StartedAt.LocalDateTime.ToString("yyyy-MM-dd");
            var hours = Math.Round((e.DurationMinutes ?? 0) / 60m, 2);
            var desc = (e.Description ?? "").Replace("\"", "\"\"");
            var ipDesc = (e.IpWorkDescription ?? "").Replace("\"", "\"\"");
            var type = e.IsIpWork ? "Wpis IP" : "Projekt IP";
            sb.AppendLine($"{date},\"{e.ProjectName}\",{hours},\"{desc}\",\"{ipDesc}\",{type}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}
