using MediatR;

namespace ContractorApp.Application.Features.IpBox.Commands.SetTimeEntryIpWork;

public record SetTimeEntryIpWorkCommand(
    Guid TimeEntryId,
    bool IsIpWork,
    string? IpWorkDescription) : IRequest;
