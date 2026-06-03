# Architecture Context — CashFlow

Ponteiro para os artefatos de arquitetura do projeto. Leia estes documentos antes de propor qualquer mudança estrutural.

---

## Documentos primários

| Documento | Propósito |
|---|---|
| [docs/architecture-overview.md](../../docs/architecture-overview.md) | Fluxos de dados, schemas de banco, resiliência, segurança, observabilidade |
| [docs/requirements.md](../../docs/requirements.md) | Cenários de usuário e critérios de aceitação por feature |
| [docs/design.md](../../docs/design.md) | Especificação formal de design: componentes, contratos de API, eventos |

## ADRs (Architecture Decision Records)

Todas as decisões de arquitetura estão em `docs/decisions/`. Antes de propor um padrão novo, verifique se já existe um ADR cobrindo o tema.

| ADR | Decisão |
|---|---|
| ADR-001 | Microsserviços vs. Monolito Modular |
| ADR-002 | Outbox Pattern para confiabilidade de eventos |
| ADR-003 | CQRS sem Event Sourcing |
| ADR-004 | Minimal API vs. Controllers MVC |
| ADR-005 | Plataforma .NET 10 |
| ADR-006 | CQRS sem MediatR — handlers via DI manual |
| ADR-007 | RabbitMQ como broker de mensagens |
| ADR-008 | PostgreSQL + EF Core como camada de persistência |
| ADR-009 | Polly para resiliência |
| ADR-010 | JWT Bearer para autenticação |
| ADR-011 | OpenTelemetry + Serilog para observabilidade |
| ADR-012 | Convenções de nomenclatura |
| ADR-013 | Estratégia de validação em duas camadas |
| ADR-014 | Result Pattern para handlers de command |

## Componentes e responsabilidades

```
CashFlow.SharedKernel
  └── Contratos (ICommandHandler, IQueryHandler)
  └── Eventos de domínio (EntryCreatedEvent)
  └── Result<T>

CashFlow.EntryService
  └── Endpoints/    — Minimal API: POST /api/entries, GET /api/entries
  └── Domain/       — Entidade Entry, DomainException
  └── Application/  — CreateEntryCommand, GetEntriesQuery e handlers
  └── Infrastructure/ — EF Core, OutboxPublisher, RabbitMQ publisher

CashFlow.ConsolidationService
  └── Endpoints/    — Minimal API: GET /api/consolidation
  └── Domain/       — DailySummary
  └── Application/  — GetDailySummaryQuery e handler
  └── Infrastructure/ — EF Core, RabbitMQ consumer, idempotency check
```

## Contrato de evento (RabbitMQ)

Exchange: `cashflow.entries`  
Evento: `EntryCreatedEvent`

```csharp
// CashFlow.SharedKernel/Events/EntryCreatedEvent.cs
public record EntryCreatedEvent(
    Guid   EventId,
    Guid   EntryId,
    string Type,       // "Debit" | "Credit"
    decimal Amount,
    DateOnly Date,
    DateTime OccurredAt
);
```

## Requisitos não-funcionais como restrições de design

| Requisito | Valor | Onde é garantido |
|---|---|---|
| Throughput | 50 req/s | Rate limiter (SlidingWindow, 50/10 s) |
| Perda máxima de lançamentos | ≤ 5% | Outbox Pattern + retry |
| Independência de falhas | ConsolidationService pode cair | Comunicação exclusivamente assíncrona |
| Latência de escrita | ≤ 2 s | Timeout Polly no EntryService |
| Segurança | JWT em todos os endpoints | `.RequireAuthorization()` |
