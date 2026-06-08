using ContractorApp.Application.Features.Expenses.Commands.CreateExpense;
using ContractorApp.Application.Features.Expenses.Commands.DeleteExpense;
using ContractorApp.Application.Features.Expenses.Commands.ScanReceipt;
using ContractorApp.Application.Features.Expenses.Commands.UpdateExpense;
using ContractorApp.Application.Features.Expenses.Queries.GetExpenseReceipt;
using ContractorApp.Application.Features.Expenses.Queries.GetExpenses;
using Microsoft.AspNetCore.Mvc;

namespace ContractorApp.API.Controllers;

[Route("api/v1/expenses")]
public class ExpensesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] string? category, CancellationToken ct)
        => Ok(await Mediator.Send(new GetExpensesQuery(from, to, category), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteExpenseCommand(id), ct);
        return NoContent();
    }

    /// <summary>Wgrywa skan/zdjęcie paragonu, rozpoznaje treść i zwraca dane do wypełnienia formularza + id skanu.</summary>
    [HttpPost("scan")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Scan([FromForm] IFormFile file, [FromForm] string? provider, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Nie przesłano pliku." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var result = await Mediator.Send(
            new ScanReceiptCommand(ms.ToArray(), file.FileName, file.ContentType, provider ?? "claude"), ct);
        return Ok(result);
    }

    /// <summary>Zwraca oryginalny plik skanu powiązanego z wydatkiem (do podglądu).</summary>
    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> GetReceipt(Guid id, CancellationToken ct)
    {
        var receipt = await Mediator.Send(new GetExpenseReceiptQuery(id), ct);
        return receipt is null ? NotFound() : File(receipt.Data, receipt.ContentType);
    }
}
