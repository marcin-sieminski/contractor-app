using System.ComponentModel;
using ContractorApp.Application.Features.Clients.Commands.CreateClient;
using ContractorApp.Application.Features.Clients.Commands.UpdateClient;
using ContractorApp.Application.Features.Clients.Queries.GetClients;
using ContractorApp.Application.Features.Clients.Queries.LookupCompanyByNip;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

[McpServerToolType]
public class ClientTools(ISender mediator)
{
    [McpServerTool(Name = "get_clients")]
    [Description("Returns all clients with their active projects.")]
    public async Task<object> GetClients(CancellationToken ct)
        => await mediator.Send(new GetClientsQuery(), ct);

    [McpServerTool(Name = "lookup_company_by_nip")]
    [Description("Looks up a Polish company by NIP using the Biała Lista MF registry. Returns company name, address, and REGON. Spaces and dashes in NIP are ignored.")]
    public async Task<object?> LookupCompanyByNip(
        [Description("NIP number (10 digits, spaces and dashes are ignored)")] string nip,
        CancellationToken ct)
        => await mediator.Send(new LookupCompanyByNipQuery(nip), ct);

    [McpServerTool(Name = "create_client")]
    [Description("Dodaje nowego klienta. Wskazówka: najpierw użyj lookup_company_by_nip, aby pobrać " +
                 "nazwę i adres po NIP, a potem przekaż je tutaj.")]
    public async Task<object> CreateClient(
        [Description("Nazwa firmy.")] string name,
        [Description("NIP (10 cyfr).")] string nip,
        [Description("REGON (opcjonalnie).")] string? regon,
        [Description("Ulica i numer (opcjonalnie).")] string? street,
        [Description("Miasto (opcjonalnie).")] string? city,
        [Description("Kod pocztowy (opcjonalnie).")] string? postalCode,
        [Description("Kraj (kod ISO, np. PL, DE). Domyślnie PL.")] string? country,
        [Description("Czy podatnik VAT UE (reverse charge). Domyślnie false.")] bool? isEuVatPayer,
        CancellationToken ct)
        => await mediator.Send(new CreateClientCommand(
            name, nip, regon, street, city, postalCode, country ?? "PL", isEuVatPayer ?? false), ct);

    [McpServerTool(Name = "update_client")]
    [Description("Aktualizuje dane klienta (po GUID). Pola pominięte pozostają bez zmian.")]
    public async Task<object> UpdateClient(
        [Description("GUID klienta z get_clients.")] Guid clientId,
        [Description("Nowa nazwa (opcjonalnie).")] string? name,
        [Description("Nowy NIP (opcjonalnie).")] string? nip,
        [Description("Nowy REGON (opcjonalnie).")] string? regon,
        [Description("Nowa ulica (opcjonalnie).")] string? street,
        [Description("Nowe miasto (opcjonalnie).")] string? city,
        [Description("Nowy kod pocztowy (opcjonalnie).")] string? postalCode,
        [Description("Nowy kraj (opcjonalnie).")] string? country,
        [Description("Czy podatnik VAT UE (opcjonalnie).")] bool? isEuVatPayer,
        CancellationToken ct)
    {
        var clients = await mediator.Send(new GetClientsQuery(), ct);
        var current = clients.FirstOrDefault(x => x.Id == clientId)
            ?? throw new InvalidOperationException("Nie znaleziono klienta o podanym GUID.");

        return await mediator.Send(new UpdateClientCommand(
            clientId,
            name ?? current.Name,
            nip ?? current.Nip,
            regon ?? current.Regon,
            street ?? current.Street,
            city ?? current.City,
            postalCode ?? current.PostalCode,
            country ?? current.Country,
            isEuVatPayer ?? current.IsEuVatPayer), ct);
    }
}
