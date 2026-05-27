using Serilog;

namespace CashFlow.ConsolidationService.Extensions;

/// <summary>Extensões de configuração de logging estruturado com Serilog.</summary>
public static class LoggingExtensions
{
    /// <summary>Configura Serilog como provider de logging, lendo a configuração de <c>appsettings.json</c>.</summary>
    /// <param name="builder">Builder da aplicação web.</param>
    public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();
        return builder;
    }
}
