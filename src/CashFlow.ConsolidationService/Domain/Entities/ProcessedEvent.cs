namespace CashFlow.ConsolidationService.Domain.Entities;

/// <summary>Registro de evento processado, usado para garantir idempotência no consumidor.</summary>
public class ProcessedEvent
{
    /// <summary>Identificador único do evento já processado.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Timestamp UTC em que o evento foi processado.</summary>
    public DateTime ProcessedAt { get; private set; }

    private ProcessedEvent() { }

    /// <summary>Cria um registro de evento processado com o timestamp atual.</summary>
    /// <param name="eventId">Identificador do evento processado.</param>
    public static ProcessedEvent Register(Guid eventId) =>
        new() { EventId = eventId, ProcessedAt = DateTime.UtcNow };
}
