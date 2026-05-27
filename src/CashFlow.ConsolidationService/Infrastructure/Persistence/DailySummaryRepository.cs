using CashFlow.ConsolidationService.Domain.Entities;
using CashFlow.ConsolidationService.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

namespace CashFlow.ConsolidationService.Infrastructure.Persistence;

/// <summary>Implementação EF Core do repositório de consolidados diários.</summary>
public class DailySummaryRepository(ConsolidationDbContext db) : IDailySummaryRepository
{
    /// <inheritdoc/>
    public async Task<DailySummary?> GetByDateAsync(DateOnly date, CancellationToken ct = default) =>
        await db.DailySummaries.FindAsync([date], ct);

    /// <inheritdoc/>
    public Task UpsertAsync(DailySummary summary, CancellationToken ct = default)
    {
        // Entidade nova (Detached): adiciona ao tracker para INSERT no SaveChangesAsync.
        // Entidade existente (carregada via FindAsync): EF já rastreia as mudanças de ApplyEntry;
        // ambas são persistidas atomicamente com o ProcessedEvent no mesmo SaveChangesAsync.
        if (db.Entry(summary).State == EntityState.Detached)
            db.DailySummaries.Add(summary);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<bool> IsEventAlreadyProcessedAsync(Guid eventId, CancellationToken ct = default) =>
        await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);

    /// <inheritdoc/>
    public async Task RegisterProcessedEventAsync(Guid eventId, CancellationToken ct = default) =>
        await db.ProcessedEvents.AddAsync(ProcessedEvent.Register(eventId), ct);

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
