using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Enums;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Invoices.Commands.GenerateInvoice;

public class GenerateInvoiceCommandHandler : IRequestHandler<GenerateInvoiceCommand, InvoiceDto>
{
    private readonly IApplicationDbContext _db;
    private readonly INbpService _nbp;
    private readonly IInvoiceNumberService _invoiceNumbers;
    private readonly ICurrentUserService _currentUser;

    public GenerateInvoiceCommandHandler(
        IApplicationDbContext db,
        INbpService nbp,
        IInvoiceNumberService invoiceNumbers,
        ICurrentUserService currentUser)
    {
        _db = db;
        _nbp = nbp;
        _invoiceNumbers = invoiceNumbers;
        _currentUser = currentUser;
    }

    public async Task<InvoiceDto> Handle(GenerateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c =>
                c.Id == request.ClientId &&
                c.DeletedAt == null &&
                c.UserId == _currentUser.UserId,
                cancellationToken)
            ?? throw new DomainException($"Klient {request.ClientId} nie istnieje.");

        var entries = await _db.TimeEntries
            .Include(t => t.Project).ThenInclude(p => p.Client)
            .Where(t =>
                request.TimeEntryIds.Contains(t.Id) &&
                t.DeletedAt == null &&
                t.Project.Client.UserId == _currentUser.UserId)
            .ToListAsync(cancellationToken);

        if (entries.Count != request.TimeEntryIds.Count)
            throw new DomainException("Niektóre wpisy czasu nie istnieją lub zostały usunięte.");

        if (entries.Any(e => e.IsInvoiced))
            throw new DomainException("Niektóre wpisy czasu są już na fakturze.");

        if (entries.Any(e => e.IsRunning))
            throw new DomainException("Nie można fakturować uruchomionego timera. Zatrzymaj go najpierw.");

        // Fetch NBP rate for non-PLN invoices
        NbpRateResult? nbpRate = null;
        if (request.Currency != Currency.PLN)
        {
            var rateDate = request.IssueDate.AddDays(-1);
            nbpRate = await _nbp.GetRateAsync(request.Currency.ToString(), rateDate, cancellationToken)
                ?? throw new DomainException($"Nie udało się pobrać kursu NBP dla {request.Currency} na dzień {rateDate}.");
        }

        var vatRate = request.VatTreatment == VatTreatment.Domestic23 ? 0.23m : 0m;

        // Build line items: one per project
        var lineItems = entries
            .GroupBy(e => e.Project)
            .Select((g, idx) =>
            {
                var hours = g.Sum(e => e.GetHours());
                var unitPrice = g.Key.HourlyRate;
                var net = Math.Round(hours * unitPrice, 2);
                var vat = Math.Round(net * vatRate, 2);
                return new InvoiceLineItem
                {
                    LineNumber = idx + 1,
                    Description = $"Usługi IT – {g.Key.Name}",
                    Quantity = hours,
                    Unit = "godz.",
                    UnitPrice = unitPrice,
                    VatRate = vatRate,
                    NetAmount = net,
                    GrossAmount = net + vat
                };
            }).ToList();

        var totalNet = lineItems.Sum(l => l.NetAmount);
        var totalVat = lineItems.Sum(l => l.GrossAmount - l.NetAmount);

        var invoiceNumber = await _invoiceNumbers.GenerateNextAsync(
            request.IssueDate.Year, request.IssueDate.Month, cancellationToken);

        var invoice = new Invoice
        {
            ClientId = request.ClientId,
            InvoiceNumber = invoiceNumber,
            IssueDate = request.IssueDate,
            ServiceDate = request.IssueDate,
            DueDate = request.IssueDate.AddDays(request.PaymentDays),
            Status = InvoiceStatus.Draft,
            VatTreatment = request.VatTreatment,
            Currency = request.Currency,
            ExchangeRate = nbpRate?.MidRate,
            ExchangeRateDate = nbpRate?.RateDate,
            ExchangeRateTableNumber = nbpRate?.TableNumber,
            TotalNet = totalNet,
            TotalVat = totalVat,
            TotalGross = totalNet + totalVat,
            LineItems = lineItems,
            Client = client
        };

        // Mark entries as invoiced
        foreach (var entry in entries)
        {
            entry.IsInvoiced = true;
            entry.InvoiceId = invoice.Id;
        }

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken);

        return invoice.ToDto();
    }
}
