# Tasks: [Nome da Feature]

**Spec:** `.specify/specs/F-XX-[nome]-spec.md`  
**Feature ID:** F-XX  
**Data:** YYYY-MM-DD  
**Status:** Pendente | Em progresso | Concluído

---

## Checklist de pré-implementação

Antes de iniciar, confirme:

- [ ] Spec aprovada (status: Aprovado)
- [ ] Constitution lida: `.specify/memory/constitution.md`
- [ ] ADRs relevantes revisados
- [ ] Contratos de API definidos na spec
- [ ] Critérios de aceitação mensuráveis

---

## Tarefas

### T-01 — Domain

- [ ] Criar/modificar entidade em `Domain/`
- [ ] Adicionar/ajustar `DomainException` se necessário
- [ ] Teste unitário da entidade (invariantes)

**Arquivos afetados:**
- `src/CashFlow.[Service]/Domain/`

**Critério de conclusão:** Todos os invariantes cobertos por testes unitários.

---

### T-02 — Application (Command ou Query)

- [ ] Criar `[Name]Command.cs` ou `[Name]Query.cs`
- [ ] Criar `[Name]CommandHandler.cs` ou `[Name]QueryHandler.cs`
- [ ] Handler retorna `Result<T>` (commands) ou `T?` (queries)
- [ ] Teste unitário do handler com repositório mockado

**Arquivos afetados:**
- `src/CashFlow.[Service]/Application/`

**Critério de conclusão:** Handler testado isoladamente (sem banco, sem HTTP).

---

### T-03 — Validação (FluentValidation)

- [ ] Criar `[Name]RequestValidator.cs`
- [ ] Cobrir campos obrigatórios, formato e restrições numéricas
- [ ] Teste unitário do validator

**Arquivos afetados:**
- `src/CashFlow.[Service]/Application/` ou `Endpoints/`

**Critério de conclusão:** `422` retornado com mapa de erros por campo em todos os cenários de entrada inválida.

---

### T-04 — Infrastructure

- [ ] Criar/modificar repositório em `Infrastructure/`
- [ ] Criar/ajustar migration EF Core se schema mudou
- [ ] Registrar no DI em `Program.cs`

**Arquivos afetados:**
- `src/CashFlow.[Service]/Infrastructure/`

**Critério de conclusão:** Migration aplicável sem erro; repositório testado com Testcontainers.

---

### T-05 — Endpoint (Minimal API)

- [ ] Adicionar `app.Map[Verb]` em `Endpoints/`
- [ ] Registrar validator e handler via DI
- [ ] Aplicar `.RequireAuthorization()`
- [ ] Mapear `Result.IsFailure` → `Results.Problem(..., statusCode: 400)`
- [ ] Mapear `null` (query) → `Results.NotFound()`
- [ ] Teste de integração do endpoint (happy path + erros)

**Arquivos afetados:**
- `src/CashFlow.[Service]/Endpoints/`

**Critério de conclusão:** Todos os critérios de aceitação da spec verificados via testes de integração.

---

### T-06 — Evento / Outbox (se aplicável)

- [ ] Definir/atualizar `[EventName]Event` em `SharedKernel`
- [ ] Serializar evento no handler e gravar em `outbox`
- [ ] Verificar idempotência no consumer (`processed_events`)
- [ ] Teste de integração end-to-end (event publicado → consumido → `daily_summaries` atualizado)

**Arquivos afetados:**
- `src/CashFlow.SharedKernel/Events/`
- `src/CashFlow.[Service]/Infrastructure/`

**Critério de conclusão:** F-04-AC-01 (idempotência) verificado por teste.

---

### T-07 — Verificação final

- [ ] Todos os critérios de aceitação da spec marcados como verificados
- [ ] Cobertura de testes ≥ 80% no handler (verificar com `dotnet test --collect:"XPlat Code Coverage"`)
- [ ] Nenhum dado sensível em logs (revisão manual)
- [ ] Constitution relida: nenhuma violação introduzida
- [ ] Spec atualizada se houve desvio durante a implementação

---

## Rastreabilidade

| Tarefa | Critério de aceite coberto |
|---|---|
| T-01 | AC-0X |
| T-02 | AC-0X |
| T-03 | AC-0X (422) |
| T-04 | AC-0X |
| T-05 | AC-0X, AC-0X |
| T-06 | F-04-AC-01 |
