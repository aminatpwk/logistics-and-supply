using LogisticsTracker.Inventory.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LogisticsTracker.Inventory.Cache
{
    public class InventoryCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<InventoryCacheService> _logger;

        private static readonly DistributedCacheEntryOptions _readOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        public InventoryCacheService(IDistributedCache cache, ILogger<InventoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        {
            try
            {
                var bytes = await _cache.GetAsync(ProductKey(productId), ct);
                return bytes is null ? null : JsonSerializer.Deserialize<InventoryItem>(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache read failed for product {ProductId}", productId);
                return null;
            }
        }

        public async Task SetAsync(InventoryItem item, CancellationToken ct = default)
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(item);
                await _cache.SetAsync(ProductKey(item.ProductId), bytes, _readOptions, ct);
                await _cache.SetAsync(SkuKey(item.StockKeepingUnit), bytes, _readOptions, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache write failed for product {ProductId}", item.ProductId);
            }
        }

        public async Task<InventoryItem?> GetBySkuAsync(string sku, CancellationToken ct = default)
        {
            try
            {
                var bytes = await _cache.GetAsync(SkuKey(sku), ct);
                return bytes is null ? null : JsonSerializer.Deserialize<InventoryItem>(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache read failed for SKU {SKU}", sku);
                return null;
            }
        }

        public async Task InvalidateAsync(Guid productId, string sku, CancellationToken ct = default)
        {
            try
            {
                await _cache.RemoveAsync(ProductKey(productId), ct);
                await _cache.RemoveAsync(SkuKey(sku), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache invalidation failed for product {ProductId}", productId);
            }
        }

        private static string ProductKey(Guid productId) => $"inventory:product:{productId}";
        private static string SkuKey(string sku) => $"inventory:sku:{sku}";
    }
}
