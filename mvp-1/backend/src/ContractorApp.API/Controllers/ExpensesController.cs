using ContractorApp.Application.Features.Expenses.Commands.CreateExpense;
using ContractorApp.Application.Features.Expenses.Commands.DeleteExpense;
using ContractorApp.Application.Features.Expenses.Commands.UpdateExpense;
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
}
