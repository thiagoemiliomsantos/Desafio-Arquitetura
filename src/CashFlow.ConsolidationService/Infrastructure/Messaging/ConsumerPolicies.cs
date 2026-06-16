using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace CashFlow.ConsolidationService.Infrastructure.Messaging;

/// <summary>Políticas Polly para consumo de mensagens do RabbitMQ.</summary>
public static class ConsumerPolicies
{
    /// <summary>
    /// Constrói o pipeline: Circuit Breaker (≥50% falhas em janela de 30 s, abre 30 s)
    /// envolvendo Retry (máx. 3 tentativas, backoff exponencial).
    /// Parametrizado para delay zero em testes sem alterar comportamento em produção.
    /// </summary>
    public static ResiliencePipeline Build(TimeSpan? retryDelay = null) =>
        new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = retryDelay ?? TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = retryDelay is null
            })
            .Build();
}
