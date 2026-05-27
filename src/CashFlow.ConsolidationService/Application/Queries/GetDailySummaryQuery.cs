namespace CashFlow.ConsolidationService.Application.Queries;

/// <summary>Query para obter o consolidado diário de uma data específica.</summary>
/// <param name="Date">Data de competência do consolidado.</param>
public record GetDailySummaryQuery(DateOnly Date);

/// <summary>Projeção do consolidado diário retornada nas consultas.</summary>
/// <param name="Date">Data de competência do consolidado.</param>
/// <param name="TotalCredits">Soma dos créditos do dia.</param>
/// <param name="TotalDebits">Soma dos débitos do dia.</param>
/// <param name="Balance">Saldo líquido do dia (créditos − débitos).</param>
/// <param name="UpdatedAt">Timestamp UTC da última atualização do consolidado.</param>
public record DailySummaryDto(DateOnly Date, decimal TotalCredits, decimal TotalDebits, decimal Balance, DateTime UpdatedAt);
