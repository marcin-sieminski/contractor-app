using FluentValidation;
using ContractorApp.Domain.ValueObjects;

namespace ContractorApp.Application.Features.Clients.Commands.CreateClient;

public class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Nip).NotEmpty().Must(nip => Nip.IsValid(nip)).WithMessage("Nieprawidłowy NIP.");
        RuleFor(x => x.Country).NotEmpty().Length(2);
    }
}
