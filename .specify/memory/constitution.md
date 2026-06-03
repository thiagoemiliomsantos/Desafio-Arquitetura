# Constitution — CashFlow

Princípios inegociáveis que se aplicam a todos os componentes do projeto.
Qualquer código gerado ou modificado deve respeitar estas regras sem exceção.

---

## 1. Arquitetura

- **Clean Architecture:** dependências apontam para dentro — Infrastructure → Application → Domain. Nunca o inverso.
- **CQRS sem MediatR:** commands e queries são despachados via interfaces genéricas (`ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`) registradas diretamente no DI. Não introduzir MediatR ou qualquer dispatcher genérico externo.
- **Minimal API:** endpoints via `app.MapGet/MapPost`. Sem Controllers MVC. Sem `[ApiController]`.
- **Database-per-Service:** `EntryService` usa o banco `entries`; `ConsolidationService` usa o banco `consolidation`. Nenhum serviço acessa o banco do outro.
- **Outbox Pattern obrigatório:** eventos de domínio só chegam ao broker via tabela `outbox`, na mesma transação do comando. Nunca publicar direto no broker dentro de um command handler.
- **Comunicação assíncrona exclusiva:** `EntryService` nunca chama `ConsolidationService` via HTTP. A única comunicação é via RabbitMQ.

## 2. Idioma

- **Identificadores de código** (classes, métodos, variáveis, propriedades, namespaces): inglês.
- **Nomes de serviços de domínio** (`CashFlow.EntryService`, `CashFlow.ConsolidationService`): exceção — mantidos em português por serem termos de domínio estabelecidos.
- **Comentários de código, mensagens de log, mensagens de erro e exceção**: português.

## 3. Segurança

- JWT Bearer obrigatório em todos os endpoints: `.RequireAuthorization()` sem exceção.
- Nenhum dado sensível em logs — Serilog com destructuring mascarado.
- Rate limiting por IP: `SlidingWindowRateLimiter` — 50 req / 10 s.
- Chave JWT nunca versionada em repositório; em produção, usar secrets manager externo.

## 4. Resiliência

- **EntryService:** timeout de 2 s nas operações de escrita; retry com backoff exponencial (3 tentativas) no Outbox Worker.
- **ConsolidationService:** Circuit Breaker Polly — 5 falhas consecutivas → aberto por 30 s; retry com jitter (3 tentativas) no consumer RabbitMQ.
- Idempotência no consumer: verificar `EventId` na tabela `processed_events` antes de processar.

## 5. Validação

- **Camada 1 — FluentValidation** (fronteira HTTP): campos obrigatórios, formato, restrições numéricas → `422 Unprocessable Entity` com mapa de erros por campo.
- **Camada 2 — Domain invariants** (entidades): `DomainException` capturada pelo handler → `Result.Fail` → `400 Bad Request`.
- Sobreposição entre camadas é intencional (defesa em profundidade), não duplicação a ser removida.
- Exceções de infraestrutura (banco, broker) **não** são capturadas como `Result.Fail` — propagam ao middleware de erro global.

## 6. Result Pattern

- Command handlers retornam `Result<T>` (definido em `CashFlow.SharedKernel`).
- Query handlers retornam `T?` (nullable) — `null` significa "não encontrado", que é semanticamente correto.
- Endpoints nunca expõem `DomainException` diretamente — mapeiam `IsFailure` para `ProblemDetails`.

## 7. Testes

- Cobertura mínima de **80%** nos handlers de Application (commands e queries).
- Stack: xUnit + Moq + FluentAssertions + Testcontainers.
- RabbitMQ simulado com InMemory nos testes de integração.
- Nenhum teste de handler deve depender do banco real — usar repositório mockado ou in-memory.

## 8. Observabilidade

- Traces via OpenTelemetry SDK (Jaeger local / OTLP em produção).
- Logs estruturados via Serilog. Campos obrigatórios por request: `TraceId`, `SpanId`, `UserId`, `Endpoint`, `StatusCode`, `DurationMs`.
- Métricas via OpenTelemetry Metrics (destino Prometheus — planejado).

## 9. Exceções de domínio na API

- `DomainException` nunca atravessa a fronteira HTTP. O handler a captura, converte em `Result.Fail`, e o endpoint mapeia para `ProblemDetails` com `statusCode: 400`.

## 10. Evolução desta constitution

Qualquer proposta de mudança a estas regras deve ser registrada como um novo ADR em `docs/decisions/` com status `Proposto` antes de ser implementada.
