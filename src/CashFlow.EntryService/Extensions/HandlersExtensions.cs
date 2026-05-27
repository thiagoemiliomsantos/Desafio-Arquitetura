using CashFlow.EntryService.Application.Commands;
using CashFlow.EntryService.Application.Queries;
using CashFlow.SharedKernel.Handlers;
using CashFlow.SharedKernel.Results;
using FluentValidation;

namespace CashFlow.EntryService.Extensions;

/// <summary>Extensões de registro dos handlers CQRS e validadores no container de injeção de dependência.</summary>
public static class HandlersExtensions
{
    /// <summary>Registra os handlers de commands e queries, e os validadores FluentValidation do serviço.</summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateEntryCommand, Result<CreateEntryResult>>, CreateEntryCommandHandler>();
        services.AddScoped<IQueryHandler<ListEntriesQuery, IReadOnlyList<EntryDto>>, ListEntriesQueryHandler>();
        services.AddScoped<IValidator<CreateEntryRequest>, CreateEntryRequestValidator>();
        return services;
    }
}
