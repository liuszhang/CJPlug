using CJ.Plug.PlugMarketApi.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.PlugMarketApi.Services
{
    public class PlugMarketService : IPlugMarketService
    {
        private readonly MainDbContext _dbContext;

        public PlugMarketService(MainDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbContext.Database.EnsureCreatedAsync();
        }
        public async Task<MarketPlug> CreateMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default)
        {
            _dbContext.Set<MarketPlug>().Add(request);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return request;
        }
        public async Task<IEnumerable<MarketPlug>> GetMarketPlugsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<MarketPlug>().ToListAsync(cancellationToken);
        }
        public async Task<MarketPlug?> GetMarketPlugByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<MarketPlug>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
        public async Task<bool> DeleteMarketPlugAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Set<MarketPlug>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null)
            {
                return false;
            }
            _dbContext.Set<MarketPlug>().Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        public async Task<MarketPlug> UpdateMarketPlugAsync(int id, MarketPlug request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Set<MarketPlug>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null)
            {
                return null;
            }
            entity.Name = request.Name;
            entity.Description = request.Description;
            entity.Type = request.Type;
            entity.Status = request.Status;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

    }
}
