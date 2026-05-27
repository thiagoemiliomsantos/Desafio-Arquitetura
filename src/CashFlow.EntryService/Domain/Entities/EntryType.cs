namespace CashFlow.EntryService.Domain.Entities;

/// <summary>Tipo de lançamento financeiro.</summary>
public enum EntryType
{
    /// <summary>Lançamento de débito (saída de caixa).</summary>
    Debit,

    /// <summary>Lançamento de crédito (entrada de caixa).</summary>
    Credit
}
