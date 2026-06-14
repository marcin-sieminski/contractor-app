using MediatR;

namespace ContractorApp.Application.Features.IpBox.Queries.GetIpBoxProgress;

public record GetIpBoxProgressQuery(int Year) : IRequest<IpBoxProgressDto>;
