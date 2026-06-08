using ContractorApp.Domain.Common;

namespace ContractorApp.Domain.Entities;

/// <summary>
/// Oryginalny skan/zdjęcie paragonu lub faktury powiązane z wydatkiem.
/// Tworzony podczas skanowania (przed zapisem wydatku), linkowany do <see cref="Expense"/> przy zapisie.
/// Plik trzymany jako blob (bytea) w PostgreSQL.
/// </summary>
public class ExpenseReceipt : Entity
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>Null dopóki skan nie zostanie powiązany z konkretnym wydatkiem.</summary>
    public Guid? ExpenseId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    /// <summary>Zawartość pliku (obraz po ewentualnym przeskalowaniu).</summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>Surowy JSON zwrócony przez model wizyjny — do audytu/debugowania.</summary>
    public string? ExtractedJson { get; set; }

    public string? Provider { get; set; }
    public string? Model { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
