using System.Threading.Tasks;
using VSCThemesStore.WebApi.Domain.Models;
using MongoDB.Driver;

namespace VSCThemesStore.WebApi.Domain.Repositories
{
    public interface IExtensionsMetadataRepository
    {
        Task<QueryResult<ExtensionMetadata>> GetItems(StoreQuery query);
        Task<ExtensionMetadata> GetExtensionMetadata(string id);
        Task Create(ExtensionMetadata extensionMetadata);
        Task<bool> Update(ExtensionMetadata extensionMetadata);
        Task<bool> UpdateStatistics(string extensionId, Statistics statistics);
        Task<bool> ChangeExtensionType(string id, ExtensionType itemType);
        Task<bool> Delete(string id);
    }

    public class ExtensionsMetadataRepository : IExtensionsMetadataRepository
    {
        private readonly IStoreContext _context;

        public ExtensionsMetadataRepository(IStoreContext context) => _context = context;

        public async Task<QueryResult<ExtensionMetadata>> GetItems(StoreQuery query)
        {
            var filter = query.Filter;

            var totalCount = await _context.ExtensionsMetadata.Find(filter).CountDocumentsAsync();
            var items = await _context.ExtensionsMetadata
                .Find(filter)
                .Sort(query.Sorting)
                .Skip((query.PageNumber * query.PageSize) - query.PageSize)
                .Limit(query.PageSize)
                .ToListAsync();

            return new QueryResult<ExtensionMetadata>
            {
                TotalCount = (int)totalCount,
                Items = items
            };
        }

        public Task<ExtensionMetadata> GetExtensionMetadata(string id)
        {
            var filter = Builders<ExtensionMetadata>.Filter.Eq(m => m.Id, id);

            return _context.ExtensionsMetadata
                .Find(filter)
                .FirstOrDefaultAsync();
        }

        public async Task Create(ExtensionMetadata extensionMetadata) =>
            await _context.ExtensionsMetadata.InsertOneAsync(extensionMetadata);

        public async Task<bool> Update(ExtensionMetadata extensionMetadata)
        {
            var filter = Builders<ExtensionMetadata>.Filter
                .Where(i => i.Id == extensionMetadata.Id);
            var updater = Builders<ExtensionMetadata>.Update
                .Set(i => i.Name, extensionMetadata.Name)
                .Set(i => i.DisplayName, extensionMetadata.DisplayName)
                .Set(i => i.Description, extensionMetadata.Description)
                .Set(i => i.PublisherName, extensionMetadata.PublisherName)
                .Set(i => i.PublisherDisplayName, extensionMetadata.PublisherDisplayName)
                .Set(i => i.Version, extensionMetadata.Version)
                .Set(i => i.LastUpdated, extensionMetadata.LastUpdated)
                .Set(i => i.IconDefault, extensionMetadata.IconDefault)
                .Set(i => i.IconSmall, extensionMetadata.IconSmall)
                .Set(i => i.AssetUri, extensionMetadata.AssetUri)
                .Set(i => i.Statistics.InstallCount, extensionMetadata.Statistics.InstallCount)
                .Set(i => i.Statistics.Downloads, extensionMetadata.Statistics.Downloads)
                .Set(i => i.Statistics.AverageRating, extensionMetadata.Statistics.AverageRating)
                .Set(i => i.Statistics.WeightedRating, extensionMetadata.Statistics.WeightedRating)
                .Set(i => i.Statistics.RatingCount, extensionMetadata.Statistics.RatingCount)
                .Set(i => i.Statistics.TrendingDaily, extensionMetadata.Statistics.TrendingDaily)
                .Set(i => i.Statistics.TrendingWeekly, extensionMetadata.Statistics.TrendingWeekly)
                .Set(i => i.Statistics.TrendingMonthly, extensionMetadata.Statistics.TrendingMonthly);

            var result = await _context.ExtensionsMetadata
                .UpdateOneAsync(filter, updater);

            return result.IsAcknowledged && result.ModifiedCount == 1;
        }

        public async Task<bool> UpdateStatistics(string extensionId, Statistics statistics)
        {
            var filter = Builders<ExtensionMetadata>.Filter
                .Where(i => i.Id == extensionId);
            var updater = Builders<ExtensionMetadata>.Update
                .Set(i => i.Statistics.InstallCount, statistics.InstallCount)
                .Set(i => i.Statistics.Downloads, statistics.Downloads)
                .Set(i => i.Statistics.AverageRating, statistics.AverageRating)
                .Set(i => i.Statistics.WeightedRating, statistics.WeightedRating)
                .Set(i => i.Statistics.RatingCount, statistics.RatingCount)
                .Set(i => i.Statistics.TrendingDaily, statistics.TrendingDaily)
                .Set(i => i.Statistics.TrendingWeekly, statistics.TrendingWeekly)
                .Set(i => i.Statistics.TrendingMonthly, statistics.TrendingMonthly);

            var result = await _context.ExtensionsMetadata
                .UpdateOneAsync(filter, updater);

            return result.IsAcknowledged && result.ModifiedCount == 1;
        }

        public async Task<bool> ChangeExtensionType(string id, ExtensionType itemType)
        {
            var filter = Builders<ExtensionMetadata>.Filter
                .Where(i => i.Id == id);
            var updater = Builders<ExtensionMetadata>.Update
                .Set(i => i.Type, itemType);

            var result = await _context.ExtensionsMetadata
                .UpdateOneAsync(filter, updater);

            return result.IsAcknowledged && result.ModifiedCount == 1;
        }

        public async Task<bool> Delete(string id)
        {
            FilterDefinition<ExtensionMetadata> filter = Builders<ExtensionMetadata>.Filter
                .Eq(m => m.Id, id);
            DeleteResult deleteResult = await _context.ExtensionsMetadata
                .DeleteOneAsync(filter);

            return deleteResult.IsAcknowledged
                && deleteResult.DeletedCount > 0;
        }
    }
}
