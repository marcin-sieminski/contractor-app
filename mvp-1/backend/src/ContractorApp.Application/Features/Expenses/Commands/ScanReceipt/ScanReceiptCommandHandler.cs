using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Entities;
using MediatR;

namespace ContractorApp.Application.Features.Expenses.Commands.ScanReceipt;

public class ScanReceiptCommandHandler : IRequestHandler<ScanReceiptCommand, ScanReceiptResult>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IReceiptExtractionService _extractor;

    public ScanReceiptCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IReceiptExtractionService extractor)
    {
        _db = db;
        _currentUser = currentUser;
        _extractor = extractor;
    }

    public async Task<ScanReceiptResult> Handle(ScanReceiptCommand request, CancellationToken cancellationToken)
    {
        var extraction = await _extractor.ExtractAsync(
            request.FileBytes, request.ContentType, request.Provider, cancellationToken);

        var receipt = new ExpenseReceipt
        {
            UserId = _currentUser.UserId,
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.FileBytes.LongLength,
            Data = request.FileBytes,
            ExtractedJson = extraction.RawJson,
            Provider = extraction.Provider,
            Model = extraction.Model,
        };

        _db.ExpenseReceipts.Add(receipt);
        await _db.SaveChangesAsync(cancellationToken);

        return new ScanReceiptResult(receipt.Id, extraction);
    }
}
