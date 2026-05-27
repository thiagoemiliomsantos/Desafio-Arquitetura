namespace CashFlow.ConsolidationService.Domain.Entities;

/// <summary>Agregado que representa o consolidado financeiro de um dia.</summary>
public class DailySummary
{
    /// <summary>Data de competência do consolidado.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Soma dos créditos do dia.</summary>
    public decimal TotalCredits { get; private set; }

    /// <summary>Soma dos débitos do dia.</summary>
    public decimal TotalDebits { get; private set; }

    /// <summary>Saldo líquido do dia (créditos − débitos).</summary>
    public decimal Balance => TotalCredits - TotalDebits;

    /// <summary>Timestamp UTC da última atualização do consolidado.</summary>
    public DateTime UpdatedAt { get; private set; }

    private DailySummary() { }

    /// <summary>Cria um consolidado zerado para a data informada.</summary>
    /// <param name="date">Data de competência.</param>
    public static DailySummary Create(DateOnly date) =>
        new() { Date = date, TotalCredits = 0, TotalDebits = 0, UpdatedAt = DateTime.UtcNow };

    /// <summary>Aplica um lançamento ao consolidado, incrementando créditos ou débitos.</summary>
    /// <param name="type">Tipo do lançamento: <c>Credit</c> ou <c>Debit</c>.</param>
    /// <param name="amount">Valor do lançamento.</param>
    public void ApplyEntry(string type, decimal amount)
    {
        if (type == "Credit")
            TotalCredits += amount;
        else if (type == "Debit")
            TotalDebits += amount;
        else
            throw new InvalidOperationException($"Tipo de lançamento desconhecido: '{type}'.");

        UpdatedAt = DateTime.UtcNow;
    }
}
