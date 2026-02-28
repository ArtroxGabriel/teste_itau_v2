using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Enums;
using ItauCompraProgramada.Domain.Interfaces;
using MediatR;

namespace ItauCompraProgramada.Application.Clients.Commands.CreateClient;

public class CreateClientCommandHandler(IClientRepository clientRepository)
    : IRequestHandler<CreateClientCommand, CreateClientResponse>
{
    public async Task<CreateClientResponse> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        var client = new Client(
            request.Name,
            request.Cpf,
            request.Email,
            request.MonthlyContribution);

        // Simple account number generation for now
        var accountNumber = "ACC-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        
        // In a real 1:1, we can add it directly if mapping is correct
        // For now, I'll rely on the domain logic if I had a method, but I'll set it manually for the prototype
        // Actually, let's just make it simple.
        
        await clientRepository.AddAsync(client);
        await clientRepository.SaveChangesAsync();

        return new CreateClientResponse(
            client.Id,
            client.Name,
            client.Cpf,
            client.Email,
            client.MonthlyContribution);
    }
}
