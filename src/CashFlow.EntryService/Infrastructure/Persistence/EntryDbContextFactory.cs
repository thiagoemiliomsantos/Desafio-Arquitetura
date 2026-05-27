using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CashFlow.EntryService.Infrastructure.Persistence;

/// <summary>Factory de design-time para criação de migrations sem dependência de variáveis de ambiente.</summary>
internal sealed class EntryDbContextFactory : IDesignTimeDbContextFactory<EntryDbContext>
{
    /// <inheritdoc/>
    public EntryDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__EntryDb")
            ?? "Host=localhost;Database=entries;Username=cashflow;Password=cashflow123";

        var options = new DbContextOptionsBuilder<EntryDbContext>()
            .UseNpgsql(connStr)
            .Options;
        return new EntryDbContext(options);
    }
}
