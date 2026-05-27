# ADR-013: Estratégia de Validação em Duas Camadas

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

O sistema precisa rejeitar entradas inválidas antes de atingir o domínio (formato, campos obrigatórios) e também proteger invariantes de negócio dentro do domínio (regras que só o domínio conhece). Essas são responsabilidades diferentes e requerem tratamentos diferentes na API: erros de formato devem retornar `422 Unprocessable Entity` com detalhes por campo; erros de negócio devem retornar `400 Bad Request` com uma mensagem de domínio.

## Decisão

Validação em duas camadas independentes:

### Camada 1 — FluentValidation (fronteira da API)

Validada no endpoint, antes de instanciar o domínio. Responsável por:
- Campos obrigatórios e não-nulos
- Restrições de formato (ex: `Type` deve ser `Debit` ou `Credit`)
- Restrições numéricas básicas (ex: `Amount > 0`)

Retorna `422 Unprocessable Entity` com mapa de erros por campo (`application/problem+json`).


### Camada 2 — Domain Exceptions + Result Pattern (domínio)

Invariantes de domínio protegidas nas entidades. `DomainException` é capturada pelo handler e convertida em `Result.Fail`. O endpoint mapeia `IsFailure` para `400 Bad Request`.


### Sobreposição intencional

Algumas regras aparecem em ambas as camadas (ex: `Amount > 0`). Isso é esperado:
- FluentValidation rejeita a requisição rapidamente, antes de qualquer I/O
- O domínio mantém seus invariantes independentemente de quem o chama

A sobreposição é defesa em profundidade, não duplicação acidental.

## Consequências

**Positivas:**
- `422` com erros por campo melhora a experiência do consumidor da API
- `400` com mensagem de domínio preserva semântica de negócio
- Domínio permanece protegido mesmo se chamado fora do contexto HTTP
- FluentValidation é testável isoladamente, sem instanciar o domínio

**Negativas:**
- Alguns termos de domínio (como tipos válidos) aparecem tanto no validator quanto no domínio — requer atenção ao sincronizar mudanças

## Alternativas consideradas

**Validação apenas no domínio via exceções** — descartado. Exceções não carregam erros por campo; `DomainException` retornaria `400` sem indicar qual campo falhou.

**FluentValidation como única camada** — descartado. O domínio precisa proteger seus invariantes independentemente da camada de entrada. Remover `DomainException` tornaria entidades instanciáveis com estado inválido.
