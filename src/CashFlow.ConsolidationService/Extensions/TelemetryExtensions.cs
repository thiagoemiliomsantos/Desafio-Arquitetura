using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CashFlow.ConsolidationService.Extensions;

/// <summary>Extensões de configuração de observabilidade com OpenTelemetry.</summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Registra traces distribuídos via OpenTelemetry com instrumentação automática
    /// de ASP.NET Core e HTTP.
    /// </summary>
    /// <remarks>
    /// <para><b>Desenvolvimento:</b> exporta traces para o console.</para>
    /// <para><b>Produção:</b> substitua <c>AddConsoleExporter()</c> por
    /// <c>AddOtlpExporter(opts => opts.Endpoint = new Uri(config["Otel:Endpoint"]!))</c>
    /// após adicionar o pacote <c>OpenTelemetry.Exporter.OpenTelemetryProtocol</c>.
    /// Configure o endpoint para Jaeger (<c>http://jaeger:4317</c>),
    /// Grafana Tempo, Datadog ou outro backend OTLP compatível.</para>
    /// </remarks>
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
