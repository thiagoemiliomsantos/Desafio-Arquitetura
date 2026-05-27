using CashFlow.EntryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.EntryService.Infrastructure.Persistence;

/// <summary>Contexto EF Core do serviço de lançamentos.</summary>
public class EntryDbContext(DbContextOptions<EntryDbContext> options) : DbContext(options)
{
    /// <summary>Conjunto de lançamentos financeiros.</summary>
    public DbSet<Entry> Entries => Set<Entry>();

    /// <summary>Conjunto de mensagens outbox pendentes de publicação.</summary>
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Entry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<string>();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => x.Date).HasDatabaseName("IX_entries_Date");
            e.ToTable("entries");
        });

        model.Entity<OutboxMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Published, x.CreatedAt }).HasDatabaseName("IX_outbox_Published_CreatedAt");
            e.ToTable("outbox");
        });
    }
}
