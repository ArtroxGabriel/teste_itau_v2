using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ItauCompraProgramada.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var uniqueId = Guid.NewGuid().ToString();

        logger.LogInformation(
            "Starting Request: {RequestName}, ID: {UniqueId}, Data: {RequestData}",
            requestName,
            uniqueId,
            JsonSerializer.Serialize(request));

        var timer = Stopwatch.StartNew();
        try
        {
            var response = await next();
            timer.Stop();

            logger.LogInformation(
                "Completed Request: {RequestName}, ID: {UniqueId}, Elapsed: {ElapsedMs}ms, Response: {ResponseData}",
                requestName,
                uniqueId,
                timer.ElapsedMilliseconds,
                JsonSerializer.Serialize(response));

            return response;
        }
        catch (Exception ex)
        {
            timer.Stop();
            logger.LogError(
                ex,
                "Failed Request: {RequestName}, ID: {UniqueId}, Elapsed: {ElapsedMs}ms, Error: {ErrorMessage}",
                requestName,
                uniqueId,
                timer.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }
}
