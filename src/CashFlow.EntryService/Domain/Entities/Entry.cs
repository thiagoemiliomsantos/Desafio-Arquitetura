namespace CashFlow.EntryService.Domain.Entities;

/// <summary>Entidade que representa um lançamento financeiro (débito ou crédito).</summary>
public class Entry
{
    /// <summary>Identificador único do lançamento.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tipo do lançamento.</summary>
    public EntryType Type { get; private set; }

    /// <summary>Valor do lançamento.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Descrição opcional do lançamento.</summary>
    public string? Description { get; private set; }

    /// <summary>Data de competência do lançamento.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Timestamp UTC de criação do registro.</summary>
    public DateTime CreatedAt { get; private set; }

    private Entry() { }

    /// <summary>Cria um novo lançamento validando as regras de domínio.</summary>
    /// <param name="type">Tipo do lançamento.</param>
    /// <param name="amount">Valor (deve ser positivo).</param>
    /// <param name="description">Descrição opcional.</param>
    /// <param name="date">Data de competência.</param>
    /// <exception cref="DomainException">Quando o valor for menor ou igual a zero, ou quando a data de competência não for informada.</exception>
    public static Entry Create(EntryType type, decimal amount, string? description, DateOnly date)
    {
        if (amount <= 0)
            throw new DomainException("O valor do lançamento deve ser positivo.");
        if (date == DateOnly.MinValue)
            throw new DomainException("A data de competência é obrigatória.");

        return new Entry
        {
            Id = Guid.NewGuid(),
            Type = type,
            Amount = amount,
            Description = description,
            Date = date,
            CreatedAt = DateTime.UtcNow
        };
    }
}
