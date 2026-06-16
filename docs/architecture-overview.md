# Visão Geral da Arquitetura — CashFlow

## Princípios Guia

1. **Independência de serviços** — EntryService nunca chama ConsolidationService
2. **Comunicação assíncrona** — eventos via RabbitMQ; nenhum serviço bloqueia o outro
3. **Outbox Pattern** — garante "at-least-once delivery" mesmo com broker indisponível
4. **Clean Architecture** — dependências apontam para dentro (Domain → Application → Infrastructure)
5. **CQRS** — commands (escrita) e queries (leitura) têm handlers separados

---

## Fluxo de Lançamento

```
Cliente
  │
  ▼
POST /api/entries                      (Minimal API endpoint)
  │  valida JWT + rate limit
  ▼
ICommandHandler<CreateEntryCommand>    (injetado via DI)
  │
  ├─► EntryRepository.AddAsync()      (PostgreSQL — tabela entries)
  │
  └─► OutboxRepository.AddAsync(event)     (PostgreSQL — tabela outbox, mesma transação)

[Background Worker]
  │  OutboxPublisher (hosted service, a cada 5s)
  ▼
RabbitMQ exchange: cashflow.entries
  │
  ▼
ConsolidationService (consumer)
  │
  ▼
DailySummaryRepository.UpsertAsync()        (PostgreSQL — tabela daily_summaries)
```

---

## Fluxo de Consulta Consolidada

```
Cliente
  │
  ▼
GET /api/consolidation?date=2024-01-15       (Minimal API endpoint)
  │  valida JWT
  ▼
IQueryHandler<GetDailySummaryQuery, DailySummaryDto>  (injetado via DI)
  │
  ▼
DailySummaryRepository.GetByDateAsync()     (PostgreSQL — leitura direta)
  │
  ▼
Response: { date, totalCreditos, totalDebitos, saldo }
```

---

## Resiliência

### EntryService
- **Rate Limiting:** `SlidingWindowRateLimiter` — 50 req/10s por IP
- **Timeout:** 2s por request de escrita no banco
- **Retry:** 3 tentativas com backoff exponencial (Polly) para publicação no Outbox Worker

### ConsolidationService
- **Circuit Breaker (Polly):** abre quando ≥ 50% das requisições falham em uma janela de 30s (mínimo de 5 tentativas); permanece aberto por 30s
- **Retry:** 3 tentativas com jitter para o consumer do RabbitMQ
- **Idempotência:** cada evento carrega `EventId` (Guid); consumer verifica antes de processar

---

## Segurança

- JWT Bearer em todos os endpoints (`.RequireAuthorization()` no Minimal API)
- Validação de `audience`, `issuer` e `lifetime`
- Rate limiting por IP para mitigar DDoS simples

---

## Observabilidade

| Sinal | Ferramenta | Destino |
|-------|-----------|---------|
| Traces | OpenTelemetry SDK | Console (dev) / OTLP (prod — Jaeger, Grafana Tempo, Datadog) |
| Logs | Serilog | Console estruturado (arquivo rotativo: futuro) |
| Métricas | OpenTelemetry Metrics | Prometheus (futuro) |

Campos emitidos em cada log de request (via `UseSerilogRequestLogging`):
- `TraceId`, `SpanId` — enriquecidos automaticamente da `Activity` corrente
- `UserId` — extraído do claim JWT (`ClaimTypes.Name`)
- `RequestMethod`, `RequestPath`, `StatusCode`, `Elapsed` (ms)

---

## Banco de Dados

### EntryService — database `entries`

```sql
CREATE TABLE entries (
  "Id"          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  "Type"        VARCHAR(7) NOT NULL CHECK ("Type" IN ('Debit','Credit')),
  "Amount"      DECIMAL(18,2) NOT NULL CHECK ("Amount" > 0),
  "Description" VARCHAR(500),
  "Date"        DATE NOT NULL,
  "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE outbox (
  "Id"        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  "EventType" VARCHAR(200) NOT NULL,
  "Payload"   TEXT NOT NULL,
  "Published" BOOLEAN NOT NULL DEFAULT FALSE,
  "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### ConsolidationService — database `consolidation`

```sql
CREATE TABLE daily_summaries (
  "Date"         DATE PRIMARY KEY,
  "TotalCredits" DECIMAL(18,2) NOT NULL DEFAULT 0,
  "TotalDebits"  DECIMAL(18,2) NOT NULL DEFAULT 0,
  "UpdatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE processed_events (
  "EventId"     UUID PRIMARY KEY,
  "ProcessedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## Decisões de Arquitetura (ADRs)

- [ADR-001: Microsserviços vs. Monolito Modular](decisions/ADR-001-microservices.md)
- [ADR-002: Outbox Pattern para confiabilidade](decisions/ADR-002-outbox.md)
- [ADR-003: CQRS sem Event Sourcing](decisions/ADR-003-cqrs.md)
- [ADR-004: Minimal API vs Controllers MVC](decisions/ADR-004-minimal-api.md)
- [ADR-005: Plataforma .NET 10](decisions/ADR-005-dotnet10.md)
- [ADR-006: CQRS sem MediatR — Handlers via DI Manual](decisions/ADR-006-no-mediatr.md)
- [ADR-007: RabbitMQ como Broker de Mensagens](decisions/ADR-007-rabbitmq.md)
- [ADR-008: PostgreSQL + EF Core como Camada de Persistência](decisions/ADR-008-postgresql-efcore.md)
- [ADR-009: Polly para Resiliência](decisions/ADR-009-polly.md)
- [ADR-010: JWT Bearer para Autenticação](decisions/ADR-010-jwt.md)
- [ADR-011: OpenTelemetry + Serilog para Observabilidade](decisions/ADR-011-observability.md)
- [ADR-012: Convenções de Nomenclatura](decisions/ADR-012-naming-conventions.md)
- [ADR-013: Estratégia de Validação em Duas Camadas](decisions/ADR-013-validation-strategy.md)
- [ADR-014: Result Pattern para Handlers de Command](decisions/ADR-014-result-pattern.md)
