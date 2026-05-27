using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CashFlow.ConsolidationService.Infrastructure.Persistence;

/// <summary>Factory de design-time para criação de migrations sem dependência de variáveis de ambiente.</summary>
internal sealed class ConsolidationDbContextFactory : IDesignTimeDbContextFactory<ConsolidationDbContext>
{
    /// <inheritdoc/>
    public ConsolidationDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__ConsolidationDb")
            ?? "Host=localhost;Database=consolidation;Username=cashflow;Password=cashflow123";

        var options = new DbContextOptionsBuilder<ConsolidationDbContext>()
            .UseNpgsql(connStr)
            .Options;
        return new ConsolidationDbContext(options);
    }
}
