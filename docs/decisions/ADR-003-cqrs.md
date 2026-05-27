# ADR-003: CQRS sem Event Sourcing

**Status:** Aceito  
**Data:** 2026-05-22  
**Atualizado:** 2026-05-22 — mecanismo de dispatch alterado para handlers via DI (ver ADR-006)

## Contexto

CQRS (Command Query Responsibility Segregation) separa operações de escrita (commands) das de leitura (queries). Frequentemente vem acompanhado de Event Sourcing (armazenar eventos em vez do estado atual).

## Decisão

Aplicar CQRS sem Event Sourcing. O estado atual é persistido diretamente no banco relacional. O dispatch de commands e queries é feito via interfaces genéricas injetadas por DI — sem biblioteca de mediator (ver ADR-006 para o racional da remoção do MediatR).

## Consequências

**Positivas:**
- Handlers pequenos, responsabilidade única, fáceis de testar
- Sem acoplamento entre escrita e leitura — cada um pode evoluir independentemente
- Simples de entender: POST → CommandHandler → Repository; GET → QueryHandler → Repository
- Zero dependência externa para o mecanismo de dispatch

**Negativas:**
- Sem histórico de eventos auditável nativamente (seria necessário auditoria separada)
- Event Sourcing daria reconstrução de estado "de graça" — aqui não se aplica
- Pipeline behaviors precisam ser decorators DI explícitos, não automáticos

## Alternativa considerada

CRUD simples nos endpoints — mais rápido de implementar, porém handlers de application ficam acoplados a detalhes de infraestrutura e são difíceis de testar isoladamente.
