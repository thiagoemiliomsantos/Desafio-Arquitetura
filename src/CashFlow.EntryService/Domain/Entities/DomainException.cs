namespace CashFlow.EntryService.Domain.Entities;

/// <summary>Exceção lançada quando uma regra de domínio é violada.</summary>
/// <param name="message">Mensagem descrevendo a violação da regra.</param>
public class DomainException(string message) : Exception(message);
