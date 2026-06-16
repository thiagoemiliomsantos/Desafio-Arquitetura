using CashFlow.EntryService.Domain.Repositories;
using CashFlow.EntryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.EntryService.Extensions;

/// <summary>Extensões de configuração de banco de dados e repositórios.</summary>
public static class DatabaseExtensions
{
    /// <summary>Registra o <see cref="EntryDbContext"/> via Npgsql e os repositórios associados.</summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação (chave <c>ConnectionStrings:EntryDb</c>).</param>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EntryDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("EntryDb")));

        services.AddScoped<IEntryRepository, EntryRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        return services;
    }

    /// <summary>Executa as migrations pendentes do <see cref="EntryDbContext"/> na inicialização da aplicação.</summary>
    /// <param name="app">Aplicação web configurada.</param>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EntryDbContext>();
        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();
    }
}
