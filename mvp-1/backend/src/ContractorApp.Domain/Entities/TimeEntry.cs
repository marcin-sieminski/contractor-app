using ContractorApp.Domain.Common;

namespace ContractorApp.Domain.Entities;

public class TimeEntry : Entity
{
    public Guid ProjectId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? StoppedAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsInvoiced { get; set; }
    public Guid? InvoiceId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Project Project { get; set; } = null!;

    public bool IsRunning => StoppedAt is null;

    public void Stop(DateTimeOffset stoppedAt)
    {
        StoppedAt = stoppedAt;
        DurationMinutes = (int)(stoppedAt - StartedAt).TotalMinutes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public decimal GetHours() => DurationMinutes.HasValue
        ? Math.Round(DurationMinutes.Value / 60m, 2)
        : 0m;
}
