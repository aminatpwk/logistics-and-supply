using LogisticsTracker.Orders.Clients.Records;
using LogisticsTracker.Orders.Models;

namespace LogisticsTracker.Orders.Clients
{
    public class InventoryHttpClient : IInventoryClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<InventoryHttpClient> _logger;

        public InventoryHttpClient(HttpClient httpClient, ILogger<InventoryHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<StockCheckResult> CheckStockAvailabilityAsync(List<OrderItem> items, CancellationToken cancellationToken = default)
        {
            var results = new List<StockCheckItemResult>();
            var allAvailable = true;
            foreach (var item in items)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"/api/inventory/{item.ProductId}", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var inventory = await response.Content.ReadFromJsonAsync<InventoryItemDto>(cancellationToken);
                        if (inventory != null)
                        {
                            var canFulfill = inventory.QuantityAvailable >= item.Quantity;
                            results.Add(new StockCheckItemResult(
                                item.ProductId,
                                inventory.StockKeepingUnit,
                                item.Quantity,
                                inventory.QuantityAvailable,
                                canFulfill,
                                canFulfill ? null : $"Insufficient stock. Available: {inventory.QuantityAvailable}, Requested: {item.Quantity}"
                            ));

                            if (!canFulfill)
                            {
                                allAvailable = false;
                            }
                        }
                        else
                        {
                            results.Add(new StockCheckItemResult(
                                item.ProductId,
                                item.StockKeepingUnit,
                                item.Quantity,
                                0,
                                false,
                                "Product not found in inventory"
                            ));
                            allAvailable = false;
                        }
                    }
                    else
                    {
                        results.Add(new StockCheckItemResult(
                            item.ProductId,
                            item.StockKeepingUnit,
                            item.Quantity,
                            0,
                            false,
                            $"Failed to check inventory: {response.StatusCode}"
                        ));
                        allAvailable = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking stock for product {ProductId}", item.ProductId);
                    results.Add(new StockCheckItemResult(
                        item.ProductId,
                        item.StockKeepingUnit,
                        item.Quantity,
                        0,
                        false,
                        $"Error checking inventory: {ex.Message}"
                    ));
                    allAvailable = false;
                }
            }
            var message = allAvailable ? "All items available" : "Some items are not available";
            return new StockCheckResult(allAvailable, results, message);
        }

        public async Task<List<ReservationResult>> ReserveInventoryForOrderAsync(Guid orderId, List<OrderItem> items, CancellationToken cancellationToken = default)
        {
            var results = new List<ReservationResult>();
            foreach (var item in items)
            {
                try
                {
                    var reserveRequest = new
                    {
                        productId = item.ProductId,
                        orderId = orderId,
                        quantity = item.Quantity
                    };

                    var response = await _httpClient.PostAsJsonAsync("/api/inventory/reserve", reserveRequest, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var reservation = await response.Content.ReadFromJsonAsync<InventoryReservationDto>(cancellationToken);
                        if (reservation != null)
                        {
                            results.Add(new ReservationResult(
                                reservation.Id,
                                reservation.ProductId,
                                reservation.StockKeepingUnit,
                                reservation.Quantity,
                                true
                            ));
                        }
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("Failed to reserve {Quantity} units of product {ProductId}: {Error}",
                            item.Quantity, item.ProductId, errorMessage);
                        results.Add(new ReservationResult(
                            Guid.Empty,
                            item.ProductId,
                            item.StockKeepingUnit,
                            item.Quantity,
                            false,
                            errorMessage
                        ));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reserving inventory for product {ProductId}", item.ProductId);
                    results.Add(new ReservationResult(
                        Guid.Empty,
                        item.ProductId,
                        item.StockKeepingUnit,
                        item.Quantity,
                        false,
                        ex.Message
                    ));
                }
            }

            return results;
        }

        public async Task<bool> ReleaseOrderReservationsAsync(Guid orderId, List<Guid> reservationIds, CancellationToken cancellationToken = default)
        {
            var allReleased = true;

            foreach (var reservationId in reservationIds)
            {
                try
                {
                    var response = await _httpClient.PostAsync($"/api/inventory/release/{reservationId}", null, cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to release reservation {ReservationId}: {StatusCode}",
                            reservationId, response.StatusCode);
                        allReleased = false;
                    }
                    else
                    {
                        _logger.LogInformation("Released reservation {ReservationId}", reservationId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error releasing reservation {ReservationId}", reservationId);
                    allReleased = false;
                }
            }

            return allReleased;
        }
    }

    #region internal DTOs
    internal record InventoryItemDto(
    Guid Id,
    Guid ProductId,
    string StockKeepingUnit,
    string Name,
    int QuantityAvailable,
    int QuantityReserved);

    internal record InventoryReservationDto(
    Guid Id,
    Guid ProductId,
    string StockKeepingUnit,
    Guid OrderId,
    int Quantity,
    DateTimeOffset ReservedAt);
    #endregion
}
