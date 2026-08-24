using FluentValidation;
using ContractorApp.Domain.ValueObjects;

namespace ContractorApp.Application.Features.Clients.Commands.UpdateClient;

public class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Nip).NotEmpty().Must(nip => Nip.IsValid(nip)).WithMessage("Nieprawidłowy NIP.");
        RuleFor(x => x.Country).NotEmpty().Length(2);
    }
}
