using CashFlow.EntryService.Domain.Entities;
using CashFlow.EntryService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.EntryService.Infrastructure.Persistence;

/// <summary>Implementação EF Core do repositório de lançamentos.</summary>
public class EntryRepository(EntryDbContext db) : IEntryRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(Entry entry, CancellationToken ct = default) =>
        await db.Entries.AddAsync(entry, ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Entry>> GetByDateAsync(DateOnly date, CancellationToken ct = default) =>
        await db.Entries.Where(e => e.Date == date).ToListAsync(ct);

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
