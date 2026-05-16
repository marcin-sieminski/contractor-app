using MediatR;

namespace ContractorApp.Application.Features.TimeEntries.Commands.DeleteTimeEntry;

public record DeleteTimeEntryCommand(Guid TimeEntryId) : IRequest;
