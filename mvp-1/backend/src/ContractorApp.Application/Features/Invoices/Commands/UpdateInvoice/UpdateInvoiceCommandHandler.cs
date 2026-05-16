using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Enums;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Invoices.Commands.UpdateInvoice;

public class UpdateInvoiceCommandHandler : IRequestHandler<UpdateInvoiceCommand, InvoiceDto>
{
    private readonly IApplicationDbContext _db;
    private readonly INbpService _nbp;

    public UpdateInvoiceCommandHandler(IApplicationDbContext db, INbpService nbp)
    {
        _db = db;
        _nbp = nbp;
    }

    public async Task<InvoiceDto> Handle(UpdateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Client)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new DomainException($"Faktura {request.InvoiceId} nie istnieje.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new DomainException("Można edytować tylko faktury w stanie Szkic.");

        if (!request.LineItems.Any())
            throw new DomainException("Faktura musi zawierać co najmniej jedną pozycję.");

        // Refresh NBP rate if currency or date changed
        NbpRateResult? nbpRate = null;
        if (request.Currency != Currency.PLN)
        {
            var rateDate = request.IssueDate.AddDays(-1);
            nbpRate = await _nbp.GetRateAsync(request.Currency.ToString(), rateDate, cancellationToken)
                ?? throw new DomainException($"Nie udało się pobrać kursu NBP dla {request.Currency} na dzień {rateDate}.");
        }

        var vatRate = request.VatTreatment == VatTreatment.Domestic23 ? 0.23m : 0m;

        // Rebuild line items
        var newItems = request.LineItems.Select(l =>
        {
            var net = Math.Round(l.Quantity * l.UnitPrice, 2);
            var vat = Math.Round(net * vatRate, 2);
            return new InvoiceLineItem
            {
                InvoiceId = invoice.Id,
                LineNumber = l.LineNumber,
                Description = l.Description,
                Quantity = l.Quantity,
                Unit = l.Unit,
                UnitPrice = l.UnitPrice,
                VatRate = vatRate,
                NetAmount = net,
                GrossAmount = net + vat
            };
        }).ToList();

        // Remove old line items and replace
        _db.InvoiceLineItems.RemoveRange(invoice.LineItems);
        _db.InvoiceLineItems.AddRange(newItems);

        invoice.IssueDate = request.IssueDate;
        invoice.ServiceDate = request.ServiceDate;
        invoice.DueDate = request.DueDate;
        invoice.VatTreatment = request.VatTreatment;
        invoice.Currency = request.Currency;
        invoice.ExchangeRate = nbpRate?.MidRate;
        invoice.ExchangeRateDate = nbpRate?.RateDate;
        invoice.ExchangeRateTableNumber = nbpRate?.TableNumber;
        invoice.TotalNet = newItems.Sum(l => l.NetAmount);
        invoice.TotalVat = newItems.Sum(l => l.GrossAmount - l.NetAmount);
        invoice.TotalGross = newItems.Sum(l => l.GrossAmount);
        invoice.XmlContent = null; // force XML regeneration on next submit
        invoice.UpdatedAt = DateTimeOffset.UtcNow;
        invoice.LineItems = newItems;

        await _db.SaveChangesAsync(cancellationToken);

        return invoice.ToDto();
    }
}
