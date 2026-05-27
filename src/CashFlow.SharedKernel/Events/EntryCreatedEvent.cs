namespace CashFlow.SharedKernel.Events;

/// <summary>Evento publicado no broker quando um lançamento é criado com sucesso.</summary>
/// <param name="EventId">Identificador único do evento para garantia de idempotência.</param>
/// <param name="EntryId">Identificador do lançamento criado.</param>
/// <param name="Type">Tipo do lançamento: <c>Debit</c> ou <c>Credit</c>.</param>
/// <param name="Amount">Valor do lançamento.</param>
/// <param name="Description">Descrição opcional do lançamento.</param>
/// <param name="Date">Data de competência do lançamento.</param>
/// <param name="OccurredAt">Timestamp UTC em que o evento ocorreu.</param>
public record EntryCreatedEvent(
    Guid EventId,
    Guid EntryId,
    string Type,
    decimal Amount,
    string? Description,
    DateOnly Date,
    DateTime OccurredAt
);
