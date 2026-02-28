using ItauCompraProgramada.Application.Common.Interfaces;

using MediatR;

namespace ItauCompraProgramada.Application.Clients.Commands.CreateClient;

public record CreateClientCommand(
    string Name,
    string Cpf,
    string Email,
    decimal MonthlyContribution,
    string CorrelationId) : IRequest<CreateClientResponse>, ICorrelatedRequest;

public record CreateClientResponse(
    long Id, 
    string Name, 
    string Cpf, 
    string Email, 
    decimal MonthlyContribution,
    bool IsActive,
    DateTime JoinedAt,
    long GraphicAccountId,
    string GraphicAccountNumber,
    string GraphicAccountType,
    DateTime GraphicAccountCreatedAt);