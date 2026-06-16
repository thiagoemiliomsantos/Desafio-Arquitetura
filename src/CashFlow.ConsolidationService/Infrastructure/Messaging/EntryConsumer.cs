using CashFlow.ConsolidationService.Application.Commands;
using CashFlow.SharedKernel.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CashFlow.ConsolidationService.Infrastructure.Messaging;

/// <summary>
/// Background service que consome eventos de lançamento do RabbitMQ e atualiza o consolidado diário.
/// A lógica de negócio e idempotência são delegadas ao <see cref="IEntryEventProcessor"/>.
/// Circuit breaker e retry são gerenciados por <see cref="ConsumerPolicies"/>.
/// </summary>
public class EntryConsumer(
    IConnectionFactory connectionFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<EntryConsumer> logger
) : BackgroundService
{
    private static readonly ResiliencePipeline _pipeline = ConsumerPolicies.Build();

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
                var processor = scope.ServiceProvider.GetRequiredService<IEntryEventProcessor>();

                await processor.ProcessAsync(entryEvent, ct);
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao processar evento. Requeue.");
            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
        }
    }
}
