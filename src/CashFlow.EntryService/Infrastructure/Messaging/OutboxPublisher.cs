using CashFlow.EntryService.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using System.Text;

namespace CashFlow.EntryService.Infrastructure.Messaging;

/// <summary>
/// Background service que publica mensagens pendentes da tabela outbox no RabbitMQ a cada 5 segundos.
/// A conexão e o canal são criados assincronamente no startup e reutilizados por todo o ciclo de vida.
/// </summary>
public class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IConnectionFactory connectionFactory,
    ILogger<OutboxPublisher> logger
) : BackgroundService
{
    private static readonly ResiliencePipeline _retry = PublishingPolicies.Build();

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declara o exchange aqui também: a declaração é idempotente no RabbitMQ,
        // mas garante que o exchange existe antes de qualquer publicação, independente
        // da ordem de startup dos serviços.
        await channel.ExchangeDeclareAsync("cashflow.entries", ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await PublishPendingAsync(channel, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task PublishPendingAsync(IChannel channel, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var pending = await outboxRepo.GetPendingAsync(50, ct);
        if (pending.Count == 0) return;

        foreach (var msg in pending)
        {
            await _retry.ExecuteAsync(async ct =>
            {
                var body = Encoding.UTF8.GetBytes(msg.Payload);
                await channel.BasicPublishAsync(
                    exchange: "cashflow.entries",
                    routingKey: msg.EventType,
                    body: body,
                    cancellationToken: ct
                );
                msg.MarkAsPublished();
                logger.LogInformation("Evento {EventType} {Id} publicado.", msg.EventType, msg.Id);
            }, ct);
        }

        await outboxRepo.SaveChangesAsync(ct);
    }
}
