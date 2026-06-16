using CashFlow.ConsolidationService.Application.Commands;
using CashFlow.ConsolidationService.Application.Queries;
using CashFlow.SharedKernel.Handlers;

namespace CashFlow.ConsolidationService.Extensions;

/// <summary>Extensões de registro dos handlers CQRS no container de injeção de dependência.</summary>
public static class HandlersExtensions
{
    /// <summary>Registra os handlers de commands e queries do serviço de consolidado.</summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetDailySummaryQuery, DailySummaryDto?>, GetDailySummaryQueryHandler>();
        services.AddScoped<IEntryEventProcessor, EntryEventProcessor>();
        return services;
    }
}
