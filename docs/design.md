# Design Specification — CashFlow

**Versão:** 1.0  
**Última atualização:** 2026-06-03  
**Status:** Baseline (reflete o sistema atual)

Este documento descreve as decisões de design de implementação derivadas de [requirements.md](requirements.md).
Mudanças aqui devem ser rastreáveis a um requisito funcional ou não-funcional.

---

## Componentes e fronteiras

```
┌─────────────────────────────────────────────────────────┐
│               CashFlow.SharedKernel                     │
│  ICommandHandler<TCommand,TResult>                      │
│  IQueryHandler<TQuery,TResult>                          │
│  Result<T>                                              │
│  EntryCreatedEvent                                      │
└───────────────────┬─────────────────────────────────────┘
                    │ referenciado por
        ┌───────────┴───────────┐
        ▼                       ▼
┌───────────────┐       ┌──────────────────────┐
│ EntryService  │       │ ConsolidationService │
│               │       │                      │
│ Endpoints/    │       │ Endpoints/            │
│ Domain/       │       │ Domain/               │
│ Application/  │       │ Application/          │
│ Infrastructure│       │ Infrastructure/       │
└───────┬───────┘       └──────────┬───────────┘
        │ publica                  │ consome
        └──────── RabbitMQ ────────┘
               exchange: cashflow.entries
```

---

## Contratos de API

### EntryService — porta 5001

#### POST /api/entries

**Request:**
```json
{
  "type": "Debit | Credit",
  "amount": 150.00,
  "description": "Pagamento fornecedor",
  "date": "2026-06-03"
}
```

**Response 201:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "Debit",
  "amount": 150.00,
  "description": "Pagamento fornecedor",
  "date": "2026-06-03",
  "createdAt": "2026-06-03T14:30:00Z"
}
```

**Response 422 (validação):**
```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Unprocessable Entity",
  "status": 422,
  "errors": {
    "amount": ["'Amount' deve ser maior que 0."],
    "type": ["'Type' deve ser 'Debit' ou 'Credit'."]
  }
}
```

**Response 400 (domínio):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Erro de domínio",
  "status": 400,
  "detail": "<mensagem da DomainException>"
}
```

---

#### GET /api/entries?date={date}

**Query param:** `date` — formato `YYYY-MM-DD`

**Response 200:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "type": "Credit",
    "amount": 500.00,
    "description": "Venda produto",
    "date": "2026-06-03",
    "createdAt": "2026-06-03T09:00:00Z"
  }
]
```

---

### ConsolidationService — porta 5002

#### GET /api/consolidation?date={date}

**Query param:** `date` — formato `YYYY-MM-DD`

**Response 200:**
```json
{
  "date": "2026-06-03",
  "totalCredits": 1500.00,
  "totalDebits": 300.00,
  "balance": 1200.00
}
```

**Response 404:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Não encontrado",
  "status": 404,
  "detail": "Nenhum consolidado encontrado para a data informada."
}
```

---

## Contrato de evento (RabbitMQ)

**Exchange:** `cashflow.entries`  
**Tipo:** `fanout`  
**Durabilidade:** durable  
**Evento:** `EntryCreatedEvent`

```csharp
public record EntryCreatedEvent(
    Guid     EventId,     // identificador único do evento (usado para idempotência)
    Guid     EntryId,
    string   Type,        // "Debit" | "Credit"
    decimal  Amount,
    DateOnly Date,
    DateTime OccurredAt
);
```

---

## Schemas de banco de dados

### EntryService — database `entries`

```sql
CREATE TABLE entries (
  "Id"          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  "Type"        VARCHAR(7)     NOT NULL CHECK ("Type" IN ('Debit','Credit')),
  "Amount"      DECIMAL(18,2)  NOT NULL CHECK ("Amount" > 0),
  "Description" VARCHAR(500),
  "Date"        DATE           NOT NULL,
  "CreatedAt"   TIMESTAMPTZ    NOT NULL DEFAULT NOW()
);

CREATE TABLE outbox (
  "Id"        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  "EventType" VARCHAR(200) NOT NULL,
  "Payload"   TEXT         NOT NULL,
  "Published" BOOLEAN      NOT NULL DEFAULT FALSE,
  "CreatedAt" TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
```

### ConsolidationService — database `consolidation`

```sql
CREATE TABLE daily_summaries (
  "Date"         DATE          PRIMARY KEY,
  "TotalCredits" DECIMAL(18,2) NOT NULL DEFAULT 0,
  "TotalDebits"  DECIMAL(18,2) NOT NULL DEFAULT 0,
  "UpdatedAt"    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE TABLE processed_events (
  "EventId"     UUID        PRIMARY KEY,
  "ProcessedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## Fluxos de dados

### Lançamento (F-01)

```
POST /api/entries
  → FluentValidation (422 se inválido)
  → CreateEntryCommand
  → CreateEntryCommandHandler
      → Entry.Create(...)         [DomainException → Result.Fail → 400]
      → EntryRepository.AddAsync()
      → OutboxRepository.AddAsync(EntryCreatedEvent)
      [mesma transação EF Core]
  → Result<CreateEntryResult>.Ok
  → 201 Created
```

### Propagação do evento (F-04)

```
OutboxPublisher [hosted service, intervalo 5 s]
  → SELECT não publicados FROM outbox
  → RabbitMQ.Publish(exchange: cashflow.entries)
  → UPDATE outbox SET Published = true
  [retry 3x backoff exponencial se Publish falhar]

ConsolidationService [consumer]
  → Recebe EntryCreatedEvent
  → SELECT FROM processed_events WHERE EventId = ?
  → Se já processado: descarta
  → DailySummaryRepository.UpsertAsync(date, amount, type)
  → INSERT INTO processed_events(EventId)
  [Circuit Breaker: 5 falhas → aberto 30 s]
```

---

## Padrões de implementação obrigatórios

### CQRS via DI manual

```csharp
// Registro no DI (Program.cs)
builder.Services.AddScoped<
    ICommandHandler<CreateEntryCommand, Result<CreateEntryResult>>,
    CreateEntryCommandHandler>();

// Injeção no endpoint
app.MapPost("/api/entries", async (
    CreateEntryRequest req,
    ICommandHandler<CreateEntryCommand, Result<CreateEntryResult>> handler,
    CancellationToken ct) => { ... });
```

### Result Pattern no endpoint

```csharp
var result = await handler.HandleAsync(command, ct);
return result.IsSuccess
    ? Results.Created($"/api/entries/{result.Value!.Id}", result.Value)
    : Results.Problem(title: "Erro de domínio", detail: result.Error, statusCode: 400);
```

### Validação no endpoint (antes do handler)

```csharp
app.MapPost("/api/entries", async (
    CreateEntryRequest req,
    IValidator<CreateEntryRequest> validator,
    ICommandHandler<...> handler,
    CancellationToken ct) =>
{
    var validation = await validator.ValidateAsync(req, ct);
    if (!validation.IsValid)
        return Results.ValidationProblem(validation.ToDictionary());
    // ...
});
```

---

## Decisões de design rastreáveis

| Decisão de design | Requisito rastreado | ADR |
|---|---|---|
| Outbox Pattern | F-04-AC-02, NF-03 | ADR-002 |
| CQRS sem MediatR | Testabilidade, NF-07 | ADR-006 |
| Minimal API | Simplicidade, ADR-004 | ADR-004 |
| Rate Limiting (50/10 s) | NF-01, F-01-AC-07 | ADR-010 |
| Circuit Breaker (5 falhas, 30 s) | F-04-AC-03, NF-04 | ADR-009 |
| Idempotência por EventId | F-04-AC-01 | ADR-002 |
| Validação em duas camadas | F-01-AC-02, F-01-AC-03 | ADR-013 |
| Result Pattern | F-01-AC-03 (400 vs 422) | ADR-014 |
