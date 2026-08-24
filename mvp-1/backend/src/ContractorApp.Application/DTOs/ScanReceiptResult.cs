using ContractorApp.Application.Common.Interfaces;

namespace ContractorApp.Application.DTOs;

/// <summary>Zwracane po zeskanowaniu paragonu: id zapisanego skanu + rozpoznane dane do prefillu formularza.</summary>
public record ScanReceiptResult(Guid ReceiptId, ReceiptExtractionResult Extracted);
