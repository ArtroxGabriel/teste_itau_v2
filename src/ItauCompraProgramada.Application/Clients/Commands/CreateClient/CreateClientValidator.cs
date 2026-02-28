using FluentValidation;

namespace ItauCompraProgramada.Application.Clients.Commands.CreateClient;

public class CreateClientValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cpf).NotEmpty().Matches(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.MonthlyContribution).GreaterThanOrEqualTo(100m)
            .WithMessage("Monthly contribution must be at least R$ 100,00.");
        RuleFor(x => x.CorrelationId).NotEmpty();
    }
}
