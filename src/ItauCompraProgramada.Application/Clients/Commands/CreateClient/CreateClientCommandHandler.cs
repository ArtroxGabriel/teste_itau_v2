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

        await clientRepository.AddAsync(client);
        await clientRepository.SaveChangesAsync();

        return new CreateClientResponse(
            client.Id,
            client.Name,
            client.Cpf,
            client.Email,
            client.MonthlyContribution,
            client.IsActive,
            client.AdhesionDate,
            client.GraphicAccount!.Id,
            client.GraphicAccount.AccountNumber,
            client.GraphicAccount.Type.ToString(),
            client.GraphicAccount.CreatedAt);
    }
}