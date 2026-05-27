using FluentValidation;

namespace CashFlow.EntryService.Application.Commands;

/// <summary>Valida o <see cref="CreateEntryRequest"/> na fronteira da API antes do mapeamento para o command.</summary>
public class CreateEntryRequestValidator : AbstractValidator<CreateEntryRequest>
{
    private static readonly HashSet<string> ValidTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Debit", "Credit" };

    /// <summary>Define as regras de validação do request HTTP.</summary>
    public CreateEntryRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("O tipo do lançamento é obrigatório.")
            .Must(t => ValidTypes.Contains(t)).WithMessage("O tipo deve ser 'Debit' ou 'Credit'.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor do lançamento deve ser positivo.");

        RuleFor(x => x.Date)
            .NotEqual(DateOnly.MinValue).WithMessage("A data de competência é obrigatória.");
    }
}
