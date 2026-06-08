using FluentValidation;

namespace ContractorApp.Application.Features.Expenses.Commands.ScanReceipt;

public class ScanReceiptCommandValidator : AbstractValidator<ScanReceiptCommand>
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp", "application/pdf"];
    private const int MaxBytes = 15 * 1024 * 1024; // 15 MB

    public ScanReceiptCommandValidator()
    {
        RuleFor(x => x.FileBytes)
            .NotEmpty().WithMessage("Plik jest pusty.")
            .Must(b => b.Length <= MaxBytes).WithMessage("Plik jest za duży (maks. 10 MB).");

        RuleFor(x => x.ContentType)
            .Must(t => AllowedTypes.Contains(t?.ToLowerInvariant()))
            .WithMessage("Dozwolone formaty: JPEG, PNG, WEBP, PDF.");
    }
}
