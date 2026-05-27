using CashFlow.EntryService.Domain.Entities;

namespace CashFlow.EntryService.Domain.Repositories;

/// <summary>Repositório de lançamentos financeiros.</summary>
public interface IEntryRepository
{
    /// <summary>Persiste um novo lançamento no contexto corrente.</summary>
    Task AddAsync(Entry entry, CancellationToken ct = default);

    /// <summary>Retorna todos os lançamentos de uma data específica.</summary>
    Task<IReadOnlyList<Entry>> GetByDateAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Persiste as alterações pendentes no banco de dados.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
