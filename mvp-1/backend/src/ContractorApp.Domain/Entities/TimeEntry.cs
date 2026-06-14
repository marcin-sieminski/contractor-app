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
    public bool IsIpWork { get; set; } = false;
    public string? IpWorkDescription { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsPaused { get; set; }
    public int AccumulatedSeconds { get; set; }

    public Project Project { get; set; } = null!;

    public bool IsRunning => StoppedAt is null && !IsPaused;

    public void Pause(DateTimeOffset now)
    {
        AccumulatedSeconds += (int)(now - StartedAt).TotalSeconds;
        IsPaused = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Resume(DateTimeOffset now)
    {
        StartedAt = now;
        IsPaused = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Stop(DateTimeOffset stoppedAt)
    {
        var currentSegmentSeconds = IsPaused ? 0 : (int)(stoppedAt - StartedAt).TotalSeconds;
        DurationMinutes = (int)Math.Round((AccumulatedSeconds + currentSegmentSeconds) / 60.0);
        StoppedAt = stoppedAt;
        IsPaused = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public decimal GetHours() => DurationMinutes.HasValue
        ? Math.Round(DurationMinutes.Value / 60m, 2)
        : 0m;
}
