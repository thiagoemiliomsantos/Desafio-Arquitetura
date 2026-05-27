# ADR-007: RabbitMQ como Broker de Mensagens

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

A comunicação entre EntryService e ConsolidationService é assíncrona (ADR-001). Precisamos de um broker que suporte publicação/consumo confiável de eventos, seja executável localmente (Docker) e tenha suporte maduro no ecossistema .NET.

## Decisão

Usar RabbitMQ com o cliente `RabbitMQ.Client` (oficial). Nos testes, o broker é simulado com implementação InMemory para eliminar dependência de infraestrutura nos testes unitários e de integração rápidos. Testcontainers provê RabbitMQ real para testes de integração lentos.

## Consequências

**Positivas:**
- Amplamente adotado, documentação extensa, cliente .NET oficial e mantido
- Suporte nativo a exchanges, filas duráveis, dead-letter queues
- Imagem Docker oficial leve; fácil de rodar localmente e em CI
- Combinação natural com o Outbox Pattern (ADR-002): eventos ficam seguros no banco até o broker estar disponível

**Negativas:**
- Ponto único de falha (SPOF) em produção: um único nó RabbitMQ que caia derruba toda a comunicação assíncrona entre os serviços. Para mitigar, produção exige um cluster com pelo menos 3 nós e filas em modo quorum (Quorum Queues), que replicam mensagens entre os nós. Neste projeto de desafio, um único nó é suficiente; em produção real, este item seria pré-requisito de infraestrutura
- Protocolo AMQP tem curva de aprendizado maior que HTTP/SSE
- Mensagens grandes podem pressionar memória do broker

## Ciclo de vida dos canais (IChannel)

`IConnection` é `Singleton` — uma conexão TCP por processo, compartilhada. `IChannel` **não é registrado no DI**: cada `BackgroundService` (`OutboxPublisher`, `EntryConsumer`) cria seu próprio canal no startup via `connection.CreateChannelAsync()` e o mantém aberto por todo o ciclo de vida do serviço, descartando-o no `Dispose`.

**Por que não registrar `IChannel` como `Scoped`:** canais criados a cada escopo de DI implicam abertura e fechamento de canal a cada ciclo do `OutboxPublisher` (a cada 5 s) e a cada mensagem consumida — operação cara no protocolo AMQP. Canais de longa duração, um por `BackgroundService`, eliminam esse custo.

**Por que não `Singleton`:** `IChannel` não é thread-safe no `RabbitMQ.Client`. Um canal `Singleton` compartilhado entre publisher e consumer causaria condições de corrida. A solução correta é um canal por consumidor/publisher, sem compartilhamento.

## Alternativa considerada

**Azure Service Bus / Amazon SQS** — descartados por introduzir acoplamento a cloud vendor específico. **Kafka** — descartado por ser superdimensionado para o volume de eventos do projeto (< 50 req/s).
