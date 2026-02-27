using ItauCompraProgramada.Application.Interfaces;
using ItauCompraProgramada.Application.Services;
using ItauCompraProgramada.Domain.Interfaces;
using ItauCompraProgramada.Infrastructure.ExternalServices;
using ItauCompraProgramada.Infrastructure.Persistence;
using ItauCompraProgramada.Infrastructure.Persistence.Repositories;
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
        services.AddScoped<IQuoteService, QuoteService>();

        return services;
    }
}
