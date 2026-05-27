using CashFlow.EntryService.Domain.Entities;

namespace CashFlow.EntryService.Domain.Repositories;

/// <summary>Repositório de mensagens outbox pendentes de publicação.</summary>
public interface IOutboxRepository
{
    /// <summary>Persiste uma nova mensagem outbox no contexto corrente.</summary>
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);

    /// <summary>Retorna até <paramref name="limit"/> mensagens não publicadas, ordenadas por data de criação.</summary>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int limit, CancellationToken ct = default);

    /// <summary>Persiste as alterações pendentes no banco de dados.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
