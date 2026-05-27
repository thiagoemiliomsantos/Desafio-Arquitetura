# Desafio Arquitetura — Projeto C#

## Visão Geral

**Tipo:** Desafio de arquitetura de software  
**Linguagem:** C# (.NET 10)  
**Status:** Em desenvolvimento

---

## O Desafio

Sistema de controle de fluxo de caixa com dois serviços independentes:

1. **CashFlow.EntryService** — recebe lançamentos (débitos e créditos), persiste e publica eventos
2. **CashFlow.ConsolidationService** — consolida o saldo diário; pode cair sem afetar o serviço de lançamentos

### Requisitos não-funcionais

| Requisito | Valor |
|-----------|-------|
| Throughput | 50 req/s no pico |
| Perda máxima | 5% |
| Independência | Consolidado pode falhar sem afetar Lançamentos |
| Resiliência | Circuit Breaker, retry, timeout |
| Segurança | Autenticação JWT, rate limiting |

---

## Arquitetura

Consulte `docs/architecture-overview.md` antes de qualquer modificação estrutural.

- **Padrão:** Clean Architecture + CQRS
- **Comunicação assíncrona:** RabbitMQ (simulado com InMemory nos testes)
- **Persistência:** PostgreSQL via EF Core + Outbox Pattern
- **Resiliência:** Polly (Circuit Breaker, Retry, Timeout)
- **Observabilidade:** OpenTelemetry + Serilog estruturado
- **Testes:** xUnit + Moq + FluentAssertions + Testcontainers

---

## Estrutura do Projeto

```
Desafio Arquitetura/
├── CLAUDE.md                          — instruções do projeto
├── README.md                          — visão geral pública
├── docs/
│   ├── architecture-overview.md       — diagrama e decisões de arquitetura
│   └── decisions/
│       ├── ADR-001 a ADR-011          — Architecture Decision Records
├── src/
│   ├── CashFlow.sln
│   ├── CashFlow.SharedKernel/         — contratos, eventos, interfaces de handler (CQRS)
│   ├── CashFlow.EntryService/   — API de lançamentos (débito/crédito)
│   │   ├── Endpoints/                 — Minimal API endpoints por recurso
│   │   ├── Domain/
│   │   ├── Application/               — Commands, Queries, Handlers (CQRS via DI)
│   │   └── Infrastructure/            — EF Core, RabbitMQ publisher, Outbox
│   ├── CashFlow.ConsolidationService/   — agregação e relatório diário
│   │   ├── Endpoints/                 — Minimal API endpoints por recurso
│   │   ├── Domain/
│   │   ├── Application/
│   │   └── Infrastructure/            — EF Core, RabbitMQ consumer
│   └── Tests/
│       ├── CashFlow.EntryService.Tests/
│       └── CashFlow.ConsolidationService.Tests/
└── graphify-out/                      — grafo de dependências (gerado por graphify)
```

---

## Convenções de Código

- C# idiomático com nullable reference types habilitado
- Async/await em toda a stack de I/O
- Records para DTOs e eventos imutáveis
- **Idioma do código:** identificadores, nomes de classes, métodos, variáveis e propriedades em inglês — ver ADR-012
- **Exceção:** `CashFlow.EntryService` e `CashFlow.ConsolidationService` mantêm nomes em português (termos de domínio estabelecidos)
- **Idioma de mensagens:** comentários de código, mensagens de log, mensagens de erro e exceção em português
- Handlers CQRS via interfaces genéricas injetadas por DI — sem MediatR (ver ADR-006)
- Endpoints expostos via Minimal API — sem Controllers MVC (ver ADR-004)
- Validação via FluentValidation
- Nunca expor exceções de domínio diretamente na API — mapear para ProblemDetails

---

## Contexto de Colaboração

- Antes de modificar contratos de API, verificar impacto no SharedKernel e nos testes
- Ao propor padrões novos, justificar com referência a SOLID ou padrão GoF aplicável
- Ao criar material de arquitetura, oferecer salvar notas em `C:\Obsidian Memories\claude-memories\`
- Manter cobertura de testes ≥ 80% nos handlers de Commands e Queries

---

## graphify — Grafo de Dependências

**Executável:** `C:/Users/User/AppData/Roaming/Python/Python313/Scripts/graphify.exe`

- Leia `graphify-out/graph.json` antes de responder perguntas sobre dependências entre serviços
- Após modificar a estrutura do projeto, rode: `graphify C:\Users\User\Desafio Arquitetura`
- Copiar atualizações para: `C:\Obsidian Memories\claude-memories\graphify\desafio-arquitetura\`
