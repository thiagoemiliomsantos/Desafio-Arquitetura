using CashFlow.ConsolidationService.Domain.Entities;
using CashFlow.ConsolidationService.Domain.Repositories;
using CashFlow.SharedKernel.Events;
using Microsoft.Extensions.Logging;

namespace CashFlow.ConsolidationService.Application.Commands;

/// <summary>Contrato para processamento de eventos de lançamento recebidos do RabbitMQ.</summary>
public interface IEntryEventProcessor
{
    /// <summary>
    /// Processa o evento, aplicando-o ao consolidado diário com garantia de idempotência.
    /// Retorna <c>true</c> se o evento foi processado, <c>false</c> se já havia sido registrado.
    /// </summary>
    Task<bool> ProcessAsync(EntryCreatedEvent entryEvent, CancellationToken ct = default);
}

/// <summary>
/// Aplica um <see cref="EntryCreatedEvent"/> ao consolidado diário.
/// Garante idempotência verificando <c>processed_events</c> antes de persistir.
/// </summary>
public class EntryEventProcessor(
    IDailySummaryRepository repo,
    ILogger<EntryEventProcessor> logger
) : IEntryEventProcessor
{
    /// <inheritdoc/>
    public async Task<bool> ProcessAsync(EntryCreatedEvent entryEvent, CancellationToken ct = default)
    {
        if (await repo.IsEventAlreadyProcessedAsync(entryEvent.EventId, ct))
        {
            logger.LogInformation("Evento {EventId} já processado (idempotência).", entryEvent.EventId);
            return false;
        }

        var summary = await repo.GetByDateAsync(entryEvent.Date, ct)
            ?? DailySummary.Create(entryEvent.Date);

        summary.ApplyEntry(entryEvent.Type, entryEvent.Amount);

        await repo.UpsertAsync(summary, ct);
        await repo.RegisterProcessedEventAsync(entryEvent.EventId, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Evento {EventId} processado — {Date} saldo: {Balance}",
            entryEvent.EventId, entryEvent.Date, summary.Balance);

        return true;
    }
}
