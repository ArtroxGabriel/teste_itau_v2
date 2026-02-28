using ItauCompraProgramada.Domain.Interfaces;
using MediatR;

namespace ItauCompraProgramada.Application.Clients.Commands.UpdateClientContribution;

public class UpdateClientContributionCommandHandler(IClientRepository clientRepository) 
    : IRequestHandler<UpdateClientContributionCommand, UpdateClientContributionResponse>
{
    public async Task<UpdateClientContributionResponse> Handle(UpdateClientContributionCommand request, CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(request.ClientId);
        
        if (client == null)
        {
            throw new KeyNotFoundException($"Cliente com ID {request.ClientId} nao encontrado.");
        }

        var oldContribution = client.MonthlyContribution;
        
        // This will throw ArgumentException if < 100 as per domain rules
        client.UpdateContribution(request.NewMonthlyContribution);
        
        await clientRepository.SaveChangesAsync();

        return new UpdateClientContributionResponse(
            client.Id,
            oldContribution,
            client.MonthlyContribution,
            DateTime.UtcNow,
            "Valor mensal atualizado. O novo valor sera considerado a partir da proxima data de compra."
        );
    }
}