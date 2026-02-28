using System;

using ItauCompraProgramada.Application.Common.Interfaces;

using MediatR;

namespace ItauCompraProgramada.Application.Purchases.Commands.ExecutePurchaseMotor;

public record ExecutePurchaseMotorCommand(DateTime ExecutionDate, string CorrelationId) : IRequest, ICorrelatedRequest;