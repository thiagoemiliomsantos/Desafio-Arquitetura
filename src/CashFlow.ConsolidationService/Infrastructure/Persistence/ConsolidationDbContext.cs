using CashFlow.ConsolidationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.ConsolidationService.Infrastructure.Persistence;

/// <summary>Contexto EF Core do serviço de consolidado diário.</summary>
public class ConsolidationDbContext(DbContextOptions<ConsolidationDbContext> options) : DbContext(options)
{
    /// <summary>Conjunto de resumos diários consolidados.</summary>
    public DbSet<DailySummary> DailySummaries => Set<DailySummary>();

    /// <summary>Conjunto de eventos já processados para garantia de idempotência.</summary>
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<DailySummary>(e =>
        {
            e.HasKey(x => x.Date);
            e.Property(x => x.TotalCredits).HasPrecision(18, 2);
            e.Property(x => x.TotalDebits).HasPrecision(18, 2);
            e.ToTable("daily_summaries");
        });

        model.Entity<ProcessedEvent>(e =>
        {
            e.HasKey(x => x.EventId);
            e.ToTable("processed_events");
        });
    }
}
