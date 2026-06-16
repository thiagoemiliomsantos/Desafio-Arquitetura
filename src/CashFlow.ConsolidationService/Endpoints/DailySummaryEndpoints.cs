using CashFlow.ConsolidationService.Application.Queries;
using CashFlow.ConsolidationService.Extensions;
using CashFlow.SharedKernel.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.ConsolidationService.Endpoints;

/// <summary>Registro dos endpoints de consolidado diário na Minimal API.</summary>
public static class DailySummaryEndpoints
{
    /// <summary>Mapeia as rotas do grupo <c>/api/consolidation</c>.</summary>
    /// <param name="app">Builder de rotas da aplicação.</param>
    public static IEndpointRouteBuilder MapDailySummaryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/consolidation")
            .RequireAuthorization()
            .RequireRateLimiting("default");

        group.MapGet("/", async (
            string? date,
            IQueryHandler<GetDailySummaryQuery, DailySummaryDto?> handler,
            CancellationToken ct) =>
        {
            if (date is null || !DateOnly.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                return Results.Problem(
                    title: "Data inválida.",
                    detail: "Informe a data no formato yyyy-MM-dd.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            var result = await handler.HandleAsync(new GetDailySummaryQuery(parsedDate), ct);
            return result is null
                ? Results.Problem(title: "Sem dados", detail: $"Nenhum consolidado para {parsedDate:yyyy-MM-dd}.", statusCode: 404)
                : Results.Ok(result);
        })
        .Produces<DailySummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSwaggerDoc(
            summary: "Retorna o consolidado diário de uma data (formato: yyyy-MM-dd)",
            responseExamples:
            [
                (200, new DailySummaryDto(
                    new DateOnly(2025, 5, 25), 8350.75m, 2180.50m, 6170.25m,
                    new DateTime(2025, 5, 25, 17, 32, 11, DateTimeKind.Utc))),
                (404, new ProblemDetails
                {
                    Title = "Sem dados",
                    Status = 404,
                    Detail = "Nenhum consolidado para 2025-05-25."
                })
            ])
        .WithRequestTimeout(TimeSpan.FromSeconds(5));

        return app;
    }
}
