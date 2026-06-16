using Polly;
using Polly.Retry;

namespace CashFlow.EntryService.Infrastructure.Messaging;

/// <summary>Políticas Polly para publicação de mensagens no RabbitMQ.</summary>
public static class PublishingPolicies
{
    /// <summary>
    /// Constrói o pipeline de retry: máx. 3 tentativas com backoff exponencial e jitter.
    /// Parametrizado para permitir delay zero em testes sem alterar comportamento em produção.
    /// </summary>
    public static ResiliencePipeline Build(TimeSpan? retryDelay = null) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = retryDelay ?? TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = retryDelay is null
            })
            .Build();
}
