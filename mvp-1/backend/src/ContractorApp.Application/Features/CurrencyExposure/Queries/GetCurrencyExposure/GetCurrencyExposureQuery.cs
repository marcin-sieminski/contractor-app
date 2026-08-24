using MediatR;

namespace ContractorApp.Application.Features.CurrencyExposure.Queries.GetCurrencyExposure;

public record GetCurrencyExposureQuery(int Year) : IRequest<CurrencyExposureDto>;
