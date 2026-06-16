using CashFlow.EntryService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CashFlow.EntryService.Extensions;

/// <summary>Extensões de health check para o serviço de lançamentos.</summary>
public static class HealthExtensions
{
    /// <summary>
    /// Registra dois checks:<br/>
    /// • <c>live</c> — processo em execução (sem I/O).<br/>
    /// • <c>ready</c> — banco de dados acessível.
    /// </summary>
    public static IServiceCollection AddServiceHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("live", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<EntryDbHealthCheck>("database", tags: ["ready"]);
        return services;
    }

    /// <summary>
    /// Mapeia <c>/health/live</c> (liveness) e <c>/health/ready</c> (readiness).
    /// Ambos são públicos e excluídos do rate limiter.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready")
        }).AllowAnonymous();

        return app;
    }
}

/// <summary>Verifica a conectividade com o banco de dados do EntryService.</summary>
file sealed class EntryDbHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EntryDbContext>();
            await db.Database.CanConnectAsync(ct);
            return HealthCheckResult.Healthy("Banco de dados acessível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao conectar ao banco de dados.", ex);
        }
    }
}
