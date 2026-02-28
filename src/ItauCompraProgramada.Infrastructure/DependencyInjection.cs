using ItauCompraProgramada.Application;
using ItauCompraProgramada.Application.Interfaces;
using ItauCompraProgramada.Application.Services;
using ItauCompraProgramada.Domain.Interfaces;
using ItauCompraProgramada.Domain.Repositories;
using ItauCompraProgramada.Infrastructure.ExternalServices;
using ItauCompraProgramada.Infrastructure.Messaging;
using ItauCompraProgramada.Infrastructure.Persistence;
using ItauCompraProgramada.Infrastructure.Persistence.Repositories;
using ItauCompraProgramada.Infrastructure.Services;


using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ItauCompraProgramada.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ItauDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddSingleton<ICotahistParser, CotahistParser>();
        services.AddScoped<IStockQuoteRepository, StockQuoteRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<ICustodyRepository, CustodyRepository>();
        services.AddScoped<IRecommendationBasketRepository, RecommendationBasketRepository>();
        services.AddScoped<IEventLogRepository, EventLogRepository>();
        services.AddScoped<IIREventRepository, IREventRepository>();
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddSingleton<IKafkaProducer, KafkaProducer>();


        services.AddHostedService<PurchaseScheduler>();

        services.AddApplication();

        return services;
    }
}