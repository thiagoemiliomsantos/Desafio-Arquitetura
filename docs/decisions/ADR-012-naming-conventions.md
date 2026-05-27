# ADR-012: Convenções de Nomenclatura

**Status:** Aceito  
**Data:** 2026-05-22  

## Contexto

O projeto envolve conceitos de domínio com termos consolidados em português (lançamento, consolidado, débito, crédito) e é desenvolvido em C#, linguagem cujo ecossistema e documentação oficial usam inglês. É necessário definir uma fronteira clara para evitar mistura de idiomas no mesmo arquivo ou classe.

## Decisão

**Todo o código em inglês** — identificadores, nomes de classes, métodos, propriedades, variáveis, parâmetros, tipos, namespaces, assemblies, nomes de tabelas e colunas.

**Mensagens em português** — comentários de código, mensagens de log, mensagens de exceção, descrições de erro e textos retornados na API (campos `detail`, `title` do ProblemDetails).

### Mapeamento de termos de domínio

| Domínio (PT) | Código (EN) | Referência |
|-------------|-------------|-----------|
| Lançamento | Entry | General Ledger Entry — IFRS, SAP, QuickBooks |
| Consolidado diário | Daily Summary / Consolidation | Financial consolidation — GAAP |
| Débito | Debit | Padrão universal |
| Crédito | Credit | Padrão universal |

## Consequências

**Positivas:**
- Consistência total: assembly, namespace, classe, método, tabela e coluna seguem a mesma convenção
- Código legível por desenvolvedores sem contexto do domínio financeiro brasileiro
- Logs, traces e stack traces em produção usam nomes consistentes com o código
- Nomes de tabela derivam naturalmente das entidades (EF Core convention-based mapping)

**Negativas:**
- Termos como `Entry` e `Consolidation` são mais genéricos que `Lançamento` e `Consolidado` — compensado com documentação de domínio (ADRs, README)

## Alternativas consideradas

**Nomes de serviço em português (`CashFlow.LancamentosService`)** — descartado. O argumento original ("termos sem equivalente preciso") não se sustenta: `Entry` é terminologia padrão em sistemas financeiros globais (IFRS, SAP, QuickBooks). Além disso, nomes de assembly aparecem em stack traces, spans OpenTelemetry e dashboards de observabilidade, onde a inconsistência com identificadores em inglês cria fricção desnecessária.

**Tudo em português** — descartado por conflitar com as convenções do ecossistema .NET e dificultar leitura por desenvolvedores sem familiaridade com o domínio.
