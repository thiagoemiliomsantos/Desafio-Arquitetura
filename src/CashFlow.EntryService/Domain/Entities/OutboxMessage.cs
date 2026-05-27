namespace CashFlow.EntryService.Domain.Entities;

/// <summary>Mensagem persistida na tabela outbox para publicação assíncrona no broker.</summary>
public class OutboxMessage
{
    /// <summary>Identificador único da mensagem.</summary>
    public Guid Id { get; private set; }

    /// <summary>Nome do tipo de evento serializado.</summary>
    public string EventType { get; private set; } = default!;

    /// <summary>Payload JSON do evento.</summary>
    public string Payload { get; private set; } = default!;

    /// <summary>Indica se a mensagem já foi publicada no broker.</summary>
    public bool Published { get; private set; }

    /// <summary>Timestamp UTC de criação da mensagem.</summary>
    public DateTime CreatedAt { get; private set; }

    private OutboxMessage() { }

    /// <summary>Cria uma nova mensagem de outbox com status pendente.</summary>
    /// <param name="eventType">Nome do tipo de evento.</param>
    /// <param name="payload">Payload JSON serializado do evento.</param>
    public static OutboxMessage Create(string eventType, string payload) =>
        new()
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            Published = false,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>Marca a mensagem como publicada com sucesso.</summary>
    public void MarkAsPublished() => Published = true;
}
