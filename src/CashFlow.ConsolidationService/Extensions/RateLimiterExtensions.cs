using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CashFlow.ConsolidationService.Extensions;

/// <summary>Extensões de configuração de rate limiting para o serviço de consolidado.</summary>
public static class RateLimiterExtensions
{
    /// <summary>
    /// Registra uma política de sliding window com limite de 50 requisições a cada 10 segundos
    /// e fila de até 10 requisições em espera.
    /// </summary>
    public static IServiceCollection AddDefaultRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(opt =>
        {
            opt.AddSlidingWindowLimiter("default", limiterOpt =>
            {
                limiterOpt.PermitLimit = 50;
                limiterOpt.Window = TimeSpan.FromSeconds(10);
                limiterOpt.SegmentsPerWindow = 5;
                limiterOpt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOpt.QueueLimit = 10;
            });
            opt.RejectionStatusCode = 429;
        });
        return services;
    }
}
