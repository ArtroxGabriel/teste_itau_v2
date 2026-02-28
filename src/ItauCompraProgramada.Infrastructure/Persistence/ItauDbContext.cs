using ItauCompraProgramada.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace ItauCompraProgramada.Infrastructure.Persistence;

public class ItauDbContext : DbContext
{
    public ItauDbContext(DbContextOptions<ItauDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<GraphicAccount> GraphicAccounts => Set<GraphicAccount>();
    public DbSet<Custody> Custodies => Set<Custody>();
    public DbSet<RecommendationBasket> RecommendationBaskets => Set<RecommendationBasket>();
    public DbSet<BasketItem> BasketItems => Set<BasketItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<Distribution> Distributions => Set<Distribution>();
    public DbSet<IREvent> IREvents => Set<IREvent>();
    public DbSet<StockQuote> StockQuotes => Set<StockQuote>();
    public DbSet<Rebalancing> Rebalancings => Set<Rebalancing>();
    public DbSet<StoredEvent> StoredEvents => Set<StoredEvent>();
    public DbSet<ContributionUpdate> ContributionUpdates => Set<ContributionUpdate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ItauDbContext).Assembly);
    }
}