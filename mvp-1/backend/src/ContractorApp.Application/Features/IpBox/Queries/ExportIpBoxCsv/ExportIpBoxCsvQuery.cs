using MediatR;

namespace ContractorApp.Application.Features.IpBox.Queries.ExportIpBoxCsv;

public record ExportIpBoxCsvQuery(int Year, int? Month) : IRequest<byte[]>;
