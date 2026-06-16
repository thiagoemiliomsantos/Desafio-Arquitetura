using CashFlow.ConsolidationService.Infrastructure.Messaging;
using CashFlow.ConsolidationService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;

namespace CashFlow.ConsolidationService.Tests.Integration;

/// <summary>
/// Factory de integração que substitui PostgreSQL por InMemory e elimina a dependência
/// do RabbitMQ, permitindo testes do pipeline HTTP completo sem infraestrutura externa.
/// </summary>
public sealed class ConsolidationServiceFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ConfigureTestServices roda APÓS o Program.cs, garantindo que estas
        // substituições se aplicam sobre os serviços já registrados pela aplicação.
        // dbRoot e dbName são criados uma única vez por factory e compartilhados
        // entre todos os escopos (pipeline HTTP e CreateScope do teste).
        // UseInternalServiceProvider isola os serviços EF Core para evitar conflito
        // entre os providers Npgsql (produção) e InMemory (testes).
        var dbRoot = new InMemoryDatabaseRoot();
        var dbName = Guid.NewGuid().ToString();
        var internalProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        builder.ConfigureTestServices(services =>
        {
            RemoveDescriptor<DbContextOptions<ConsolidationDbContext>>(services);

            services.AddDbContext<ConsolidationDbContext>(opt =>
                opt.UseInMemoryDatabase(dbName, dbRoot)
                   .UseInternalServiceProvider(internalProvider));

            RemoveDescriptor<IConnectionFactory>(services);
            services.AddSingleton<IConnectionFactory>(Mock.Of<IConnectionFactory>());

            RemoveHostedService<EntryConsumer>(services);
        });
    }

    private static void RemoveDescriptor<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null) services.Remove(descriptor);
    }

    private static void RemoveHostedService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ImplementationType == typeof(T));
        if (descriptor is not null) services.Remove(descriptor);
    }
}
