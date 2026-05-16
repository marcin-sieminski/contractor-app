using ContractorApp.Domain.Entities;

namespace ContractorApp.Application.Common.Interfaces;

public interface IKsefXmlBuilder
{
    string Build(Invoice invoice, string sellerNip);
}
