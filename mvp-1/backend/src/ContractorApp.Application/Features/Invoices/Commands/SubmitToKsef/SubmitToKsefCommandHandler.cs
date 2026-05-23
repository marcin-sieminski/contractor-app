using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Enums;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Invoices.Commands.SubmitToKsef;

public class SubmitToKsefCommandHandler : IRequestHandler<SubmitToKsefCommand, InvoiceDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IKsefService _ksef;
    private readonly IKsefXmlBuilder _xmlBuilder;
    private readonly ICurrentUserService _currentUser;

    public SubmitToKsefCommandHandler(
        IApplicationDbContext db,
        IKsefService ksef,
        IKsefXmlBuilder xmlBuilder,
        ICurrentUserService currentUser)
    {
        _db = db;
        _ksef = ksef;
        _xmlBuilder = xmlBuilder;
        _currentUser = currentUser;
    }

    public async Task<InvoiceDto> Handle(SubmitToKsefCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Client)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i =>
                i.Id == request.InvoiceId &&
                i.Client.UserId == _currentUser.UserId,
                cancellationToken)
            ?? throw new DomainException($"Faktura {request.InvoiceId} nie istnieje.");

        if (invoice.Status == InvoiceStatus.Accepted)
            throw new DomainException("Faktura jest już zatwierdzona w KSeF.");

        if (string.IsNullOrEmpty(invoice.XmlContent))
            invoice.XmlContent = _xmlBuilder.Build(invoice, request.SellerNip);

        var result = await _ksef.SubmitInvoiceAsync(invoice.XmlContent, request.SellerNip, cancellationToken);

        if (result.Success)
        {
            invoice.Status = InvoiceStatus.Accepted;
            invoice.KsefReferenceNumber = result.ReferenceNumber;
            invoice.KsefSubmittedAt = DateTimeOffset.UtcNow;
            invoice.KsefError = null;
        }
        else
        {
            invoice.Status = InvoiceStatus.Rejected;
            invoice.KsefError = result.Error;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return invoice.ToDto();
    }
}
