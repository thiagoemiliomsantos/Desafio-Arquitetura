namespace CashFlow.EntryService.Application.Commands;

using CashFlow.EntryService.Domain.Entities;

/// <summary>Payload HTTP para criação de um lançamento.</summary>
/// <param name="Type">Tipo do lançamento: <c>Debit</c> (saída) ou <c>Credit</c> (entrada).</param>
/// <param name="Amount">Valor em reais, com até 2 casas decimais. Deve ser positivo.</param>
/// <param name="Description">Descrição opcional do lançamento (ex: número da NF, fornecedor).</param>
/// <param name="Date">Data de competência no formato <c>yyyy-MM-dd</c> (ISO 8601).</param>
public record CreateEntryRequest(string Type, decimal Amount, string? Description, DateOnly Date);

/// <summary>Command para criar um novo lançamento financeiro.</summary>
/// <param name="Type">Tipo do lançamento.</param>
/// <param name="Amount">Valor do lançamento (deve ser positivo).</param>
/// <param name="Description">Descrição opcional do lançamento.</param>
/// <param name="Date">Data de competência do lançamento.</param>
public record CreateEntryCommand(
    EntryType Type,
    decimal Amount,
    string? Description,
    DateOnly Date
);

/// <summary>Resultado da criação de um lançamento.</summary>
/// <param name="Id">Identificador único gerado para o lançamento.</param>
/// <param name="Type">Tipo do lançamento persistido.</param>
/// <param name="Amount">Valor do lançamento persistido.</param>
/// <param name="Date">Data de competência do lançamento persistido.</param>
public record CreateEntryResult(Guid Id, EntryType Type, decimal Amount, DateOnly Date);
