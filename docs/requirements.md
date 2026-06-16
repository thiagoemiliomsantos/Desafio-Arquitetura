# Requirements — CashFlow

**Versão:** 1.0  
**Última atualização:** 2026-06-03  
**Status:** Baseline (cobre o sistema atual)

Este documento descreve os requisitos do sistema do ponto de vista do usuário/consumidor da API.
É a fonte de verdade para critérios de aceitação. O código deve ser derivado daqui, não o contrário.

---

## Contexto do domínio

Uma empresa precisa controlar o fluxo de caixa diário: registrar débitos e créditos ao longo do dia e consultar o saldo consolidado ao final. O serviço de consolidação **não pode** impactar o serviço de lançamentos — se o consolidado cair, os lançamentos continuam funcionando.

---

## Atores

| Ator | Descrição |
|---|---|
| **Operador** | Sistema ou usuário autenticado que registra e consulta lançamentos |
| **Gestor** | Usuário autenticado que consulta o saldo consolidado do dia |

---

## Funcionalidades

### F-01 — Registrar lançamento

**Cenário:** Um operador autenticado registra um débito ou crédito no sistema.

**Pré-condição:** Token JWT válido presente no header `Authorization: Bearer <token>`.

**Fluxo principal:**
1. Operador envia `POST /api/entries` com tipo (`Debit` ou `Credit`), valor e data.
2. Sistema valida o formato da requisição.
3. Sistema persiste o lançamento e registra o evento `EntryCreated` na tabela `outbox` na mesma transação.
4. Sistema retorna `201 Created` com os dados do lançamento criado.

**Critérios de aceitação:**

| # | Critério | Verificação |
|---|---|---|
| F-01-AC-01 | Lançamento com `Type`, `Amount > 0` e `Date` válidos retorna `201` com `id` gerado | Teste de integração |
| F-01-AC-02 | Campo `Type` ausente ou inválido retorna `422` com campo `type` no mapa de erros | Teste unitário (validator) |
| F-01-AC-03 | `Amount ≤ 0` retorna `422` com campo `amount` no mapa de erros | Teste unitário (validator) |
| F-01-AC-04 | Requisição sem token JWT retorna `401` | Teste de integração |
| F-01-AC-05 | Evento `EntryCreated` é gravado na tabela `outbox` na mesma transação do lançamento | Teste de integração |
| F-01-AC-06 | Throughput de 50 req/s é sustentado sem degradação observável | Teste de carga |
| F-01-AC-07 | Requisição acima do rate limit (50/10 s por IP) retorna `429` | Teste de integração |
| F-01-AC-08 | Resposta em menos de 2 s sob carga normal | Teste de integração com timeout |

---

### F-02 — Listar lançamentos do dia

**Cenário:** Um operador autenticado consulta todos os lançamentos de uma data específica.

**Pré-condição:** Token JWT válido.

**Fluxo principal:**
1. Operador envia `GET /api/entries?date=YYYY-MM-DD`.
2. Sistema retorna lista (possivelmente vazia) de lançamentos da data informada.

**Critérios de aceitação:**

| # | Critério | Verificação |
|---|---|---|
| F-02-AC-01 | Data com lançamentos existentes retorna `200` com lista não vazia | Teste de integração |
| F-02-AC-02 | Data sem lançamentos retorna `200` com lista vazia `[]` | Teste de integração |
| F-02-AC-03 | Parâmetro `date` ausente retorna `422` | Teste unitário (validator) |
| F-02-AC-04 | Formato de data inválido retorna `422` | Teste unitário (validator) |
| F-02-AC-05 | Requisição sem token JWT retorna `401` | Teste de integração |

---

### F-03 — Consultar saldo consolidado do dia

**Cenário:** Um gestor autenticado consulta o saldo diário consolidado (total de créditos, débitos e saldo líquido).

**Pré-condição:** Token JWT válido. O saldo é calculado de forma assíncrona pelo `ConsolidationService` ao consumir eventos do `EntryService`.

**Fluxo principal:**
1. Gestor envia `GET /api/consolidation?date=YYYY-MM-DD`.
2. Sistema retorna o saldo consolidado da data: `totalCredits`, `totalDebits`, `balance`.

**Critérios de aceitação:**

| # | Critério | Verificação |
|---|---|---|
| F-03-AC-01 | Data com resumo consolidado existente retorna `200` com `{ date, totalCredits, totalDebits, balance }` | Teste de integração |
| F-03-AC-02 | Data sem resumo retorna `404` com `ProblemDetails` | Teste de integração |
| F-03-AC-03 | `balance` = `totalCredits` - `totalDebits` | Teste unitário (domínio) |
| F-03-AC-04 | Requisição sem token JWT retorna `401` | Teste de integração |
| F-03-AC-05 | `ConsolidationService` indisponível não impede `EntryService` de receber lançamentos | Teste de resiliência |

---

### F-04 — Processamento assíncrono de eventos (interno)

**Cenário:** O sistema propaga lançamentos registrados para o serviço de consolidação de forma confiável, mesmo em caso de falha temporária do broker.

**Fluxo principal:**
1. `OutboxPublisher` (background worker) lê eventos não publicados da tabela `outbox` a cada 5 s.
2. Publica no exchange `cashflow.entries` do RabbitMQ.
3. Marca evento como `Published = true`.
4. `ConsolidationService` consome o evento e executa upsert na tabela `daily_summaries`.

**Critérios de aceitação:**

| # | Critério | Verificação |
|---|---|---|
| F-04-AC-01 | Evento com `EventId` já presente em `processed_events` é descartado sem reprocessamento | Teste de integração |
| F-04-AC-02 | Falha na publicação aciona retry com backoff exponencial (máx. 3 tentativas) | Teste unitário (Polly policy) |
| F-04-AC-03 | Quando ≥ 50% das chamadas falham em uma janela de 30 s (mínimo 5 tentativas), o Circuit Breaker abre por 30 s | Teste unitário (Polly policy) |
| F-04-AC-04 | Perda de lançamentos ≤ 5% mesmo com broker temporariamente indisponível | Teste de resiliência |

---

## Requisitos não-funcionais

| ID | Requisito | Valor-alvo | Verificação |
|---|---|---|---|
| NF-01 | Throughput | 50 req/s sustentados no `EntryService` | Teste de carga |
| NF-02 | Latência de escrita | ≤ 2 s (p99) no `POST /api/entries` | Teste de carga |
| NF-03 | Perda máxima de lançamentos | ≤ 5% | Teste de resiliência com broker instável |
| NF-04 | Independência de falhas | `ConsolidationService` pode cair sem impactar `EntryService` | Teste de resiliência |
| NF-05 | Segurança | JWT obrigatório em todos os endpoints | Revisão de código + testes |
| NF-06 | Observabilidade | Trace distribuído presente em 100% das requisições | Validação manual (Jaeger) |
| NF-07 | Cobertura de testes | ≥ 80% nos handlers de Application | `dotnet test --collect:"XPlat Code Coverage"` |

---

## Fora de escopo (v1.0)

- Autenticação de usuários finais (IDP externo — decisão de produção, não de desenvolvimento)
- Relatórios históricos multi-dia
- Exportação de dados (CSV, PDF)
- Interface gráfica (sistema headless, API-only)
- Métricas em Prometheus (planejado para v1.1)
