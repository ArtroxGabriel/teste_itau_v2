using System.Text.Json;

using ItauCompraProgramada.Application.Common.Interfaces;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Repositories;

using MediatR;

namespace ItauCompraProgramada.Application.Behaviors;

public class ResiliencyBehavior<TRequest, TResponse>(IEventLogRepository eventLogRepository)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICorrelatedRequest
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var existingEvent = await eventLogRepository.GetByCorrelationIdAsync(request.CorrelationId);

        if (existingEvent != null && existingEvent.Status == "Succeeded")
        {
            if (!string.IsNullOrEmpty(existingEvent.ResponsePayload))
            {
                return JsonSerializer.Deserialize<TResponse>(existingEvent.ResponsePayload)!;
            }
            // If it's a command that doesn't return anything (TResponse is Unit)
            return default!;
        }

        var response = await next();

        var responsePayload = JsonSerializer.Serialize(response);
        var storedEvent = new StoredEvent(
            typeof(TRequest).Name,
            JsonSerializer.Serialize(request),
            request.CorrelationId,
            "Succeeded",
            responsePayload);

        await eventLogRepository.SaveAsync(storedEvent);

        return response;
    }
}