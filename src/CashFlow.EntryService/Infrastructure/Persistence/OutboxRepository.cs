using CashFlow.EntryService.Domain.Entities;
using CashFlow.EntryService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.EntryService.Infrastructure.Persistence;

/// <summary>Implementação EF Core do repositório de mensagens outbox.</summary>
public class OutboxRepository(EntryDbContext db) : IOutboxRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default) =>
        await db.Outbox.AddAsync(message, ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int limit, CancellationToken ct = default) =>
        await db.Outbox
            .Where(o => !o.Published)
            .OrderBy(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
