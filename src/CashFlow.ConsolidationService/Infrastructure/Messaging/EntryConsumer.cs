using CashFlow.ConsolidationService.Domain.Entities;
using CashFlow.ConsolidationService.Domain.Repositories;
using CashFlow.SharedKernel.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CashFlow.ConsolidationService.Infrastructure.Messaging;

/// <summary>
/// Background service que consome eventos de lançamento do RabbitMQ e atualiza o consolidado diário.
/// Implementa circuit breaker e retry via Polly, além de idempotência por <c>EventId</c>.
/// A conexão e o canal são criados assincronamente no startup e fechados ao parar o host.
/// </summary>
public class EntryConsumer(
    IConnectionFactory connectionFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<EntryConsumer> logger
) : BackgroundService
{
    private static readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .Build();

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync("cashflow.entries", ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync("consolidation.entries", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync("consolidation.entries", "cashflow.entries", "#", cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, 10, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) => await ProcessMessageAsync(channel, ea, stoppingToken);

        await channel.BasicConsumeAsync("consolidation.entries", autoAck: false, consumer, stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            await _pipeline.ExecuteAsync(async ct =>
            {
                var entryEvent = JsonSerializer.Deserialize<EntryCreatedEvent>(body)
                    ?? throw new InvalidOperationException("Evento inválido.");

                using var scope = scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IDailySummaryRepository>();

                if (await repo.IsEventAlreadyProcessedAsync(entryEvent.EventId, ct))
                {
                    logger.LogInformation("Evento {EventId} já processado (idempotência).", entryEvent.EventId);
                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                    return;
                }

                var summary = await repo.GetByDateAsync(entryEvent.Date, ct)
                    ?? DailySummary.Create(entryEvent.Date);

                summary.ApplyEntry(entryEvent.Type, entryEvent.Amount);

                await repo.UpsertAsync(summary, ct);
                await repo.RegisterProcessedEventAsync(entryEvent.EventId, ct);
                await repo.SaveChangesAsync(ct);

                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                logger.LogInformation("Evento {EventId} processado — {Date} saldo: {Balance}",
                    entryEvent.EventId, entryEvent.Date, summary.Balance);
            }, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao processar evento. Requeue.");
            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
        }
    }
}
