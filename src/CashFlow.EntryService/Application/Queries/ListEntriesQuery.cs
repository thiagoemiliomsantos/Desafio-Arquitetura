using CashFlow.EntryService.Domain.Entities;

namespace CashFlow.EntryService.Application.Queries;

/// <summary>Query para listar os lançamentos de uma data específica.</summary>
/// <param name="Date">Data de competência dos lançamentos a listar.</param>
public record ListEntriesQuery(DateOnly Date);

/// <summary>Projeção de lançamento retornada nas consultas.</summary>
/// <param name="Id">Identificador único do lançamento.</param>
/// <param name="Type">Tipo do lançamento: <c>Debit</c> ou <c>Credit</c>.</param>
/// <param name="Amount">Valor do lançamento.</param>
/// <param name="Description">Descrição opcional do lançamento.</param>
/// <param name="Date">Data de competência do lançamento.</param>
/// <param name="CreatedAt">Timestamp UTC de criação do registro.</param>
public record EntryDto(Guid Id, EntryType Type, decimal Amount, string? Description, DateOnly Date, DateTime CreatedAt);
