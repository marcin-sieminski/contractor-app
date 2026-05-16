using ContractorApp.Domain.Exceptions;

namespace ContractorApp.Domain.ValueObjects;

public sealed class Nip : IEquatable<Nip>
{
    private static readonly int[] Weights = [6, 5, 7, 2, 3, 4, 5, 6, 7];

    public string Value { get; }

    public Nip(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 10)
            throw new DomainException($"NIP '{value}' musi mieć dokładnie 10 cyfr.");

        var sum = Weights.Select((w, i) => w * (digits[i] - '0')).Sum();
        var controlDigit = sum % 11;
        if (controlDigit == 10 || controlDigit != (digits[9] - '0'))
            throw new DomainException($"NIP '{value}' ma nieprawidłową cyfrę kontrolną.");

        Value = digits;
    }

    public static bool IsValid(string value)
    {
        try { _ = new Nip(value); return true; }
        catch { return false; }
    }

    public override string ToString() => Value;
    public bool Equals(Nip? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Nip n && Equals(n);
    public override int GetHashCode() => Value.GetHashCode();
}
