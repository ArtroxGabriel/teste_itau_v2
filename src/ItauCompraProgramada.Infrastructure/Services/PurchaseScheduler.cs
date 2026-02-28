using System;
using System.Threading;
using System.Threading.Tasks;

using ItauCompraProgramada.Application.Purchases.Commands.ExecutePurchaseMotor;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ItauCompraProgramada.Infrastructure.Services;

public class PurchaseScheduler(
    IServiceProvider serviceProvider,
    ILogger<PurchaseScheduler> logger) : BackgroundService
{
    private static readonly int[] ScheduledDays = { 5, 15, 25 };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Purchase Scheduler Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            if (ScheduledDays.Contains(now.Day))
            {
                logger.LogInformation("Scheduled day {Day} detected. Triggering Purchase Motor...", now.Day);

                using var scope = serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var correlationId = $"PurchaseMotor-{now:yyyy-MM-dd}";
                var command = new ExecutePurchaseMotorCommand(now, correlationId);

                try
                {
                    await mediator.Send(command, stoppingToken);
                    logger.LogInformation("Purchase Motor executed successfully for {Date}.", now.ToString("yyyy-MM-dd"));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing Purchase Motor for {Date}.", now.ToString("yyyy-MM-dd"));
                }
            }

            // Sleep until next day or check every hour
            // For the prototype, let's check every hour but avoid double execution via CorrelationId/ResiliencyBehavior
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}