using MediatR;

namespace ContractorApp.Application.Features.IpBox.Commands.SetProjectIpStatus;

public record SetProjectIpStatusCommand(Guid ProjectId, bool IsIpProject) : IRequest;
