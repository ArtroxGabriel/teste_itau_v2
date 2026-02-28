using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Domain.Interfaces;

public interface IRecommendationBasketRepository
{
    Task<RecommendationBasket?> GetActiveAsync();
    Task AddAsync(RecommendationBasket basket);
    Task SaveChangesAsync();
}