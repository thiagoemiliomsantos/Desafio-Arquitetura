namespace CashFlow.SharedKernel.Handlers;

/// <summary>Contrato para handlers de commands no padrão CQRS.</summary>
/// <typeparam name="TCommand">Tipo do command a ser processado.</typeparam>
/// <typeparam name="TResult">Tipo do resultado retornado após execução.</typeparam>
public interface ICommandHandler<TCommand, TResult>
{
    /// <summary>Executa o command e retorna o resultado.</summary>
    /// <param name="command">Dados do command a ser executado.</param>
    /// <param name="ct">Token de cancelamento.</param>
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}

/// <summary>Contrato para handlers de queries no padrão CQRS.</summary>
/// <typeparam name="TQuery">Tipo da query.</typeparam>
/// <typeparam name="TResult">Tipo do resultado retornado.</typeparam>
public interface IQueryHandler<TQuery, TResult>
{
    /// <summary>Executa a query e retorna o resultado.</summary>
    /// <param name="query">Dados da query.</param>
    /// <param name="ct">Token de cancelamento.</param>
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}
