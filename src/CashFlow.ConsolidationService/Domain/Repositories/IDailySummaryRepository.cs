using CashFlow.ConsolidationService.Domain.Entities;

namespace CashFlow.ConsolidationService.Domain.Repositories;

/// <summary>Repositório de consolidados diários e controle de idempotência de eventos.</summary>
public interface IDailySummaryRepository
{
    /// <summary>Retorna o consolidado de uma data, ou <c>null</c> se ainda não existir.</summary>
    Task<DailySummary?> GetByDateAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Insere ou atualiza atomicamente o consolidado da data correspondente.</summary>
    Task UpsertAsync(DailySummary summary, CancellationToken ct = default);

    /// <summary>Verifica se o evento já foi processado anteriormente (idempotência).</summary>
    Task<bool> IsEventAlreadyProcessedAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>Registra o evento como processado para evitar reprocessamento.</summary>
    Task RegisterProcessedEventAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>Persiste as alterações pendentes no banco de dados.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
