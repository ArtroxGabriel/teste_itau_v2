using FluentValidation;

namespace ItauCompraProgramada.Application.Admin.Commands.CreateBasket;

/// <summary>
/// Validates RN-014 (exactly 5 assets) and RN-015 (sum = 100%) and RN-016 (each > 0%).
/// </summary>
public class CreateBasketCommandValidator : AbstractValidator<CreateBasketCommand>
{
    public CreateBasketCommandValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome da cesta é obrigatório.");

        RuleFor(x => x.Itens)
            .NotNull()
            .Must(items => items.Count == 5)
            .WithMessage(x => $"A cesta deve conter exatamente 5 ativos. Quantidade informada: {x.Itens?.Count ?? 0}.");

        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.Ticker).NotEmpty().WithMessage("O ticker é obrigatório.");
            item.RuleFor(i => i.Percentual)
                .GreaterThan(0).WithMessage("Cada percentual deve ser maior que 0%.");
        });

        RuleFor(x => x.Itens)
            .Must(items => items != null && items.Sum(i => i.Percentual) == 100m)
            .WithMessage(x => $"A soma dos percentuais deve ser exatamente 100%. Soma atual: {x.Itens?.Sum(i => i.Percentual) ?? 0}%.");
    }
}
