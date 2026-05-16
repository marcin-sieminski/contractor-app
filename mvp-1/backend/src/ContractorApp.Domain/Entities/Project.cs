using ContractorApp.Domain.Common;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Domain.Entities;

public class Project : Entity
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public Currency Currency { get; set; } = Currency.PLN;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? DeletedAt { get; set; }

    public Client Client { get; set; } = null!;
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
