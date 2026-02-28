using System.Reflection;

using FluentValidation;

using ItauCompraProgramada.Application.Behaviors;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace ItauCompraProgramada.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(ResiliencyBehavior<,>));
        });

        return services;
    }
}