using LogisticsTracker.Inventory.Models;
using LogisticsTracker.Inventory.Models.DTOs;
using LogisticsTracker.Inventory.Repository;
using LogisticsTracker.Inventory.Service;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IInventoryRepository, InMemoryInventoryRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Logging.AddConsole();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var inventoryApi = app.MapGroup("/api/inventory")
    .WithTags("Inventory")
    .WithOpenApi();

//APIs
inventoryApi.MapPost("/", async Task<Results<Created<InventoryItemResponse>, BadRequest<string>>> (CreateInventoryItemRequest request, IInventoryService service, CancellationToken ct) =>
{
    try
    {
        var item = await service.CreateInventoryItemAsync(request, ct);
        var response = InventoryItemResponse.FromInventoryItem(item);
        return TypedResults.Created($"/api/inventory/{item.ProductId}", response);
    }
    catch (ArgumentException ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
})
.WithName("CreateInventoryItem")
.WithSummary("Create a new inventory item")
.WithDescription("Creates a new inventory item with stock tracking");

inventoryApi.MapGet("/{productId:guid}", async Task<Results<Ok<InventoryItemResponse>, NotFound>> (Guid productId, IInventoryService service, CancellationToken ct) =>
{
    var item = await service.GetInventoryItemAsync(productId, ct);
    return item is not null ? TypedResults.Ok(InventoryItemResponse.FromInventoryItem(item)) : TypedResults.NotFound();
})
.WithName("GetInventoryByProductId")
.WithSummary("Get inventory by product ID")
.WithDescription("Retrieves inventory information for a specific product");

inventoryApi.MapGet("/sku/{sku}", async Task<Results<Ok<InventoryItemResponse>, NotFound>> (string sku, IInventoryService service, CancellationToken ct) =>
{
    var item = await service.GetInventoryItemAsync(sku, ct);
    return item is not null ? TypedResults.Ok(InventoryItemResponse.FromInventoryItem(item)) : TypedResults.NotFound();
})
.WithName("GetInventoryBySku")
.WithSummary("Get inventory by SKU")
.WithDescription("Retrieves inventory information by stock keeping unit");

inventoryApi.MapGet("/", async Task<Ok<List<InventoryItemResponse>>> (
    IInventoryService service,
    CancellationToken ct) =>
{
    var items = await service.GetAllInventoryAsync(ct);
    return TypedResults.Ok(items);
})
.WithName("GetAllInventory")
.WithSummary("Get all inventory items")
.WithDescription("Retrieves all inventory items with current stock levels");

inventoryApi.MapGet("/low-stock", async Task<Ok<List<LowStockItemResponse>>> (IInventoryService service, CancellationToken ct) =>
{
    var items = await service.GetLowStockItemsAsync(ct);
    return TypedResults.Ok(items);
})
.WithName("GetLowStockItems")
.WithSummary("Get low stock items")
.WithDescription("Retrieves items that are below their reorder point");

inventoryApi.MapPut("/{productId:guid}/stock", async Task<Results<Ok<InventoryItemResponse>, NotFound, BadRequest<string>>> (Guid productId,
    UpdateStockRequest request,
    IInventoryService service,
    CancellationToken ct) =>
{
    try
    {
        var item = await service.UpdateStockAsync(productId, request, ct);
        return TypedResults.Ok(InventoryItemResponse.FromInventoryItem(item));
    }
    catch (KeyNotFoundException)
    {
        return TypedResults.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
    catch (ArgumentException ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
})
.WithName("UpdateStock")
.WithSummary("Update stock levels")
.WithDescription("Updates stock quantity for a product (receipt, shipment, adjustment, etc.)");

inventoryApi.MapPost("/reserve", async Task<Results<Ok<InventoryReservation>, BadRequest<string>>> (ReserveInventoryRequest request, IInventoryService service, CancellationToken ct) =>
{
    try
    {
        var reservation = await service.ReserveInventoryAsync(request, ct);
        return TypedResults.Ok(reservation);
    }
    catch (KeyNotFoundException ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
})
.WithName("ReserveInventory")
.WithSummary("Reserve inventory for an order")
.WithDescription("Reserves inventory quantity for a specific order");

inventoryApi.MapPost("/release/{reservationId:guid}", async Task<Results<Ok, NotFound>> (Guid reservationId, IInventoryService service, CancellationToken ct) =>
{
    var released = await service.ReleaseReservationAsync(reservationId, ct);
    return released ? TypedResults.Ok() : TypedResults.NotFound();
})
.WithName("ReleaseReservation")
.WithSummary("Release a reservation")
.WithDescription("Releases a reservation and returns inventory to available stock");

app.Run();

[JsonSerializable(typeof(CreateInventoryItemRequest))]
[JsonSerializable(typeof(UpdateStockRequest))]
[JsonSerializable(typeof(ReserveInventoryRequest))]
[JsonSerializable(typeof(ReleaseReservationRequest))]
[JsonSerializable(typeof(InventoryItemResponse))]
[JsonSerializable(typeof(List<InventoryItemResponse>))]
[JsonSerializable(typeof(LowStockItemResponse))]
[JsonSerializable(typeof(List<LowStockItemResponse>))]
[JsonSerializable(typeof(InventoryReservation))]
[JsonSerializable(typeof(InventoryItem))]
[JsonSerializable(typeof(StockCheckResponse))]
[JsonSerializable(typeof(Dictionary<Guid, StockCheckResponse>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
