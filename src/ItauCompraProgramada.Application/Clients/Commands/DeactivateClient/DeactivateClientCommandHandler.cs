using ItauCompraProgramada.Domain.Interfaces;
using MediatR;

namespace ItauCompraProgramada.Application.Clients.Commands.DeactivateClient;

public class DeactivateClientCommandHandler(IClientRepository clientRepository) 
    : IRequestHandler<DeactivateClientCommand, DeactivateClientResponse>
{
    public async Task<DeactivateClientResponse> Handle(DeactivateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(request.ClientId);
        
        if (client == null)
        {
            throw new KeyNotFoundException($"Cliente nao encontrado.");
        }

        if (!client.IsActive)
        {
            throw new InvalidOperationException("Cliente ja inativo.");
        }

        client.Deactivate();
        await clientRepository.SaveChangesAsync();

        return new DeactivateClientResponse(
            client.Id,
            client.Name,
            client.IsActive,
            DateTime.UtcNow,
            "Adesao encerrada. Sua posicao em custodia foi mantida."
        );
    }
}