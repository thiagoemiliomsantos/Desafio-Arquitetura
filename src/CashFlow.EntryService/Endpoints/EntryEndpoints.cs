using CashFlow.EntryService.Application.Commands;
using CashFlow.EntryService.Application.Queries;
using CashFlow.EntryService.Domain.Entities;
using CashFlow.EntryService.Extensions;
using CashFlow.SharedKernel.Handlers;
using CashFlow.SharedKernel.Results;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.EntryService.Endpoints;

/// <summary>Registro dos endpoints de lançamentos na Minimal API.</summary>
public static class EntryEndpoints
{
    /// <summary>Mapeia as rotas do grupo <c>/api/entries</c>.</summary>
    /// <param name="app">Builder de rotas da aplicação.</param>
    public static IEndpointRouteBuilder MapEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/entries")
            .RequireAuthorization()
            .RequireRateLimiting("default");

        group.MapPost("/", async (
            CreateEntryRequest request,
            IValidator<CreateEntryRequest> validator,
            ICommandHandler<CreateEntryCommand, Result<CreateEntryResult>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(
                    validation.ToDictionary(),
                    title: "Um ou mais campos são inválidos.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            var entryType = Enum.Parse<EntryType>(request.Type, ignoreCase: true);
            var command = new CreateEntryCommand(entryType, request.Amount, request.Description, request.Date);

            var result = await handler.HandleAsync(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/entries?date={result.Value!.Date:yyyy-MM-dd}", result.Value)
                : Results.Problem(title: "Erro de domínio", detail: result.Error, statusCode: 400);
        })
        .Produces<CreateEntryResult>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSwaggerDoc(
            summary: "Registra um lançamento de débito ou crédito",
            requestExample: new CreateEntryRequest("Credit", 1250.75m, "Venda balcão — NF 4521", new DateOnly(2025, 5, 25)),
            responseExamples:
            [
                (201, new CreateEntryResult(
                    Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"), EntryType.Credit, 1250.75m, new DateOnly(2025, 5, 25))),
                (422, new HttpValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { "Type",   ["O tipo deve ser 'Debit' ou 'Credit'."] },
                    { "Amount", ["O valor do lançamento deve ser positivo."] }
                })
                {
                    Title = "Um ou mais campos são inválidos.",
                    Status = 422
                }),
                (400, new ProblemDetails
                {
                    Title = "Erro de domínio",
                    Status = 400,
                    Detail = "O valor do lançamento deve ser positivo."
                })
            ])
        .WithRequestTimeout(TimeSpan.FromSeconds(2));

        group.MapGet("/", async (
            DateOnly date,
            IQueryHandler<ListEntriesQuery, IReadOnlyList<EntryDto>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new ListEntriesQuery(date), ct);
            return Results.Ok(result);
        })
        .Produces<IReadOnlyList<EntryDto>>(StatusCodes.Status200OK)
        .WithSwaggerDoc(
            summary: "Lista os lançamentos por data (formato: yyyy-MM-dd)",
            responseExamples:
            [
                (200, new EntryDto[]
                {
                    new(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"), EntryType.Credit, 1250.75m, "Venda balcão — NF 4521",
                        new DateOnly(2025, 5, 25), new DateTime(2025, 5, 25, 9, 14, 32, DateTimeKind.Utc)),
                    new(Guid.Parse("9b2c1f48-3e7a-4d8b-a1c6-5f0e3d9a2b7e"), EntryType.Debit, 380.50m, "Pagamento fornecedor — NF 892",
                        new DateOnly(2025, 5, 25), new DateTime(2025, 5, 25, 11, 47, 05, DateTimeKind.Utc))
                })
            ]);

        return app;
    }
}
