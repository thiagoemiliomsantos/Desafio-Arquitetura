# CashFlow — Desafio de Arquitetura de Software

Sistema de controle de fluxo de caixa com arquitetura de microsserviços, SOLID, design patterns e alta disponibilidade.

## O Problema

Uma empresa precisa de um sistema para controlar o fluxo de caixa diário — registrar débitos e créditos — e gerar um relatório consolidado do saldo do dia. O serviço de consolidação **não pode** impactar o serviço de lançamentos: se o consolidado cair, lançamentos continuam funcionando.

## Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                         API Gateway                         │
│              (Rate Limiting • Auth JWT • Routing)           │
└────────────────┬────────────────────┬───────────────────────┘
                 │                    │
    ┌────────────▼──────────┐  ┌──────▼──────────────────┐
    │     EntryService      │  │   ConsolidationService  │
    │  POST /api/entries    │  │  GET /api/consolidation │
    │  GET  /api/entries    │  │  (saldo diário)         │
    │                       │  │                         │
    │  ┌─────────────────┐  │  │  ┌───────────────────┐  │
    │  │  PostgreSQL DB  │  │  │  │   PostgreSQL DB   │  │
    │  │  (entries)      │  │  │  │  (consolidation)  │  │
    │  └─────────────────┘  │  │  └───────────────────┘  │
    │  ┌─────────────────┐  │  │  ┌───────────────────┐  │
    │  │  Outbox Table   │──┼──┼─►│  RabbitMQ         │  │
    │  └─────────────────┘  │  │  │  Consumer         │  │
    └───────────────────────┘  └─────────────────────────┘
                 │                         ▲
                 └──── RabbitMQ ───────────┘
                    (EntryCreated event)
```

### Decisões de arquitetura

| Decisão | Escolha | Motivo |
|---------|---------|--------|
| Padrão | Clean Architecture + CQRS | Separação de responsabilidades, testabilidade |
| Comunicação | RabbitMQ (async) | Desacopla serviços; consolidado pode cair |
| Confiabilidade | Outbox Pattern | Garante entrega mesmo se broker cair |
| Resiliência | Polly Circuit Breaker | Impede cascata de falhas |
| ORM | EF Core + Repository | Abstração de persistência, migrations |
| Auth | JWT Bearer | Stateless, escalável horizontalmente |
| Testes | xUnit + Moq + FluentAssertions | Amplamente adotados no ecossistema .NET |

## Requisitos Não-Funcionais

- **Throughput:** 50 req/s no pico
- **Perda máxima:** ≤ 5% de lançamentos
- **Disponibilidade:** Consolidado pode falhar sem afetar lançamentos
- **Segurança:** JWT obrigatório em todos os endpoints, rate limiting por IP
- **Observabilidade:** traces distribuídos (OpenTelemetry), logs estruturados (Serilog)

> **Rate limiting vs. throughput:** o rate limiter está configurado em 50 req/10 s **por IP** como proteção contra abuso (DoS). A capacidade total de 50 req/s é atingida com múltiplos clientes simultâneos e/ou escalonando horizontalmente os serviços (`docker compose up --scale entry-service=N`). Os dois conceitos são independentes: o rate limit protege cada IP individualmente; o throughput agrega todas as instâncias.

## Como rodar

```bash
docker compose up --build
```

Swagger UI disponível em desenvolvimento:
- EntryService: http://localhost:5001/swagger
- ConsolidationService: http://localhost:5002/swagger

Obtenha um token JWT em `POST /api/auth/token` (credenciais: `dev` / `dev`) e use o botão **Authorize** no Swagger UI.

## Endpoints

### EntryService (porta 5001)

```
POST /api/entries         — registrar débito ou crédito
GET  /api/entries?date=   — listar lançamentos do dia
POST /api/auth/token          — [DEV] gerar token JWT
```

### ConsolidationService (porta 5002)

```
GET /api/consolidation?date=    — saldo consolidado do dia
```

## Estrutura do Projeto

```
src/
├── CashFlow.SharedKernel/             — contratos compartilhados, eventos de domínio
├── CashFlow.EntryService/             — serviço de lançamentos
│   ├── Domain/                        — entidades, regras de negócio
│   ├── Application/                   — CQRS: commands, queries, handlers
│   ├── Infrastructure/                — EF Core, RabbitMQ, Outbox
│   └── Endpoints/                     — Minimal API endpoints
├── CashFlow.ConsolidationService/     — serviço de consolidação diária
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   └── Endpoints/
└── Tests/
    ├── CashFlow.EntryService.Tests/
    └── CashFlow.ConsolidationService.Tests/
```

## Cobertura de Testes

Objetivo: ≥ 80% nos handlers de Application. Rodar com:

```bash
dotnet test src/CashFlow.sln --collect:"XPlat Code Coverage"
```

## Configuração de Produção

### Autenticação JWT

| Configuração | Desenvolvimento | Produção |
|---|---|---|
| `Jwt:Key` | Valor fixo em `appsettings.json` | Variável de ambiente ou secrets manager¹ |
| Emissor de tokens | `POST /api/auth/token` (dev only) | IDP externo² |
| Rotação de chaves | Não aplicável | Rotacionar periodicamente via secrets manager |

¹ AWS Secrets Manager, Azure Key Vault, HashiCorp Vault ou equivalente. A chave deve ter **no mínimo 256 bits** e nunca deve ser versionada em repositório.

² Em produção, remova a dependência do endpoint `/api/auth/token` (já desabilitado fora de `IsDevelopment()`) e configure o `JwtBearer` com `Authority` apontando para o IDP:

```csharp
opt.Authority = "https://seu-idp.exemplo.com";
opt.Audience  = "cashflow-clients";
// Remova IssuerSigningKey — o middleware buscará as chaves públicas do IDP via OIDC discovery
```

Provedores recomendados: **Keycloak** (self-hosted), **Azure AD B2C**, **Auth0**.

---

### Observabilidade (OpenTelemetry)

Em desenvolvimento os traces são exportados para o console. Em produção, substitua o exporter em `TelemetryExtensions.cs`:

**1. Adicione o pacote OTLP:**
```bash
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

**2. Troque `AddConsoleExporter()` por `AddOtlpExporter()`:**
```csharp
.WithTracing(tracing => tracing
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddOtlpExporter(opts =>
        opts.Endpoint = new Uri(configuration["Otel:Endpoint"]!)))
```

**3. Configure a variável de ambiente:**
```yaml
# docker-compose (produção)
OTEL__ENDPOINT: "http://jaeger:4317"   # Jaeger
# ou
OTEL__ENDPOINT: "http://tempo:4317"    # Grafana Tempo
# ou use o endpoint OTLP do Datadog / New Relic / Dynatrace
```

Backends recomendados: **Jaeger** ou **Grafana Tempo** (self-hosted), **Datadog**, **New Relic**.
