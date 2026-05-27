using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CashFlow.ConsolidationService.Extensions;

/// <summary>Extensões de configuração de observabilidade com OpenTelemetry.</summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Registra traces distribuídos via OpenTelemetry com instrumentação automática
    /// de ASP.NET Core e HTTP, exportando para o console em desenvolvimento.
    /// Em produção, substituir o exporter por OTLP (Jaeger, Tempo, etc.).
    /// </summary>
    public static IServiceCollection AddTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("CashFlow.ConsolidationService"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter());

        return services;
    }
}
