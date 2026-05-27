using CashFlow.ConsolidationService.Infrastructure.Messaging;
using RabbitMQ.Client;

namespace CashFlow.ConsolidationService.Extensions;

/// <summary>Extensões de configuração da conexão RabbitMQ e do consumidor de eventos.</summary>
public static class MessagingExtensions
{
    /// <summary>
    /// Registra a factory de conexão RabbitMQ e o <see cref="EntryConsumer"/> como hosted service.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação (chaves <c>RabbitMQ:Host</c>, <c>RabbitMQ:User</c>, <c>RabbitMQ:Password</c>).</param>
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:User"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        });

        services.AddHostedService<EntryConsumer>();
        return services;
    }
}
