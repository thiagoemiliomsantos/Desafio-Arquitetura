using CashFlow.ConsolidationService.Domain.Repositories;
using CashFlow.SharedKernel.Handlers;

namespace CashFlow.ConsolidationService.Application.Queries;

/// <summary>Handler que executa <see cref="GetDailySummaryQuery"/> e retorna o consolidado diário.</summary>
public class GetDailySummaryQueryHandler(IDailySummaryRepository repo)
    : IQueryHandler<GetDailySummaryQuery, DailySummaryDto?>
{
    /// <inheritdoc/>
    public async Task<DailySummaryDto?> HandleAsync(GetDailySummaryQuery query, CancellationToken ct)
    {
        var summary = await repo.GetByDateAsync(query.Date, ct);
        if (summary is null) return null;
        return new DailySummaryDto(summary.Date, summary.TotalCredits, summary.TotalDebits, summary.Balance, summary.UpdatedAt);
    }
}
