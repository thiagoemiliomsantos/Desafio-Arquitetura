using CashFlow.EntryService.Domain.Repositories;
using CashFlow.SharedKernel.Handlers;

namespace CashFlow.EntryService.Application.Queries;

/// <summary>Handler que executa <see cref="ListEntriesQuery"/> e retorna os lançamentos da data informada.</summary>
public class ListEntriesQueryHandler(IEntryRepository repo)
    : IQueryHandler<ListEntriesQuery, IReadOnlyList<EntryDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<EntryDto>> HandleAsync(ListEntriesQuery query, CancellationToken ct)
    {
        var entries = await repo.GetByDateAsync(query.Date, ct);
        return entries
            .Select(e => new EntryDto(e.Id, e.Type, e.Amount, e.Description, e.Date, e.CreatedAt))
            .ToList();
    }
}
