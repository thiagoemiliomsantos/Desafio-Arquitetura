using CashFlow.ConsolidationService.Domain.Repositories;
using CashFlow.ConsolidationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.ConsolidationService.Extensions;

/// <summary>Extensões de configuração de banco de dados e repositórios.</summary>
public static class DatabaseExtensions
{
    /// <summary>Registra o <see cref="ConsolidationDbContext"/> via Npgsql e os repositórios associados.</summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação (chave <c>ConnectionStrings:ConsolidationDb</c>).</param>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ConsolidationDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("ConsolidationDb")));

        services.AddScoped<IDailySummaryRepository, DailySummaryRepository>();
        return services;
    }

    /// <summary>Executa as migrations pendentes do <see cref="ConsolidationDbContext"/> na inicialização da aplicação.</summary>
    /// <param name="app">Aplicação web configurada.</param>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConsolidationDbContext>();
        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();
    }
}
