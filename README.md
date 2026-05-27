# CashFlow — Desafio de Arquitetura de Software

Sistema de controle de fluxo de caixa com arquitetura de microsserviços, SOLID, design patterns e alta disponibilidade.

## O Problema

Uma empresa precisa de um sistema para controlar o fluxo de caixa diário — registrar débitos e créditos — e gerar um relatório consolidado do saldo do dia. O serviço de consolidação **não pode** impactar o serviço de lançamentos: se o consolidado cair, lançamentos continuam funcionando.

## Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                         API Gateway                          │
│              (Rate Limiting • Auth JWT • Routing)            │
└────────────────┬────────────────────┬───────────────────────┘
                 │                    │
    ┌────────────▼──────────┐  ┌──────▼──────────────────┐
    │     EntryService      │  │   ConsolidationService   │
    │  POST /api/entries│  │  GET /api/consolidation    │
    │  GET  /api/entries│  │  (saldo diário)          │
    │                       │  │                          │
    │  ┌─────────────────┐  │  │  ┌───────────────────┐  │
    │  │  PostgreSQL DB  │  │  │  │   PostgreSQL DB   │  │
    │  │  (entries)      │  │  │  │  (consolidation)  │  │
    │  └─────────────────┘  │  │  └───────────────────┘  │
    │  ┌─────────────────┐  │  │  ┌───────────────────┐  │
    │  │  Outbox Table   │──┼──┼─►│  RabbitMQ         │  │
    │  └─────────────────┘  │  │  │  Consumer         │  │
    └───────────────────────┘  └──────────────────────────┘
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
