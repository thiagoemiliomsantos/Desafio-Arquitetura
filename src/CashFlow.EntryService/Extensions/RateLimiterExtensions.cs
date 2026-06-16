using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CashFlow.EntryService.Extensions;

/// <summary>Extensões de configuração de rate limiting para o serviço de lançamentos.</summary>
public static class RateLimiterExtensions
{
    /// <summary>
    /// Registra uma política de sliding window particionada por IP de origem:
    /// 50 requisições a cada 10 segundos por cliente, fila de até 10 requisições em espera.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    public static IServiceCollection AddDefaultRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(opt =>
        {
            opt.AddPolicy("default", httpCtx =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpCtx.Connection.RemoteIpAddress ?? IPAddress.Loopback,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 50,
                        Window = TimeSpan.FromSeconds(10),
                        SegmentsPerWindow = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));
            opt.RejectionStatusCode = 429;
        });
        return services;
    }
}
