using Events.Inventory;
using Events.Extensions;
using LogisticsTracker.Orders.Clients;
using LogisticsTracker.Orders.DbContext;
using LogisticsTracker.Orders.EventHandlers;
using LogisticsTracker.Orders.Models;
using LogisticsTracker.Orders.Models.DTOs;
using LogisticsTracker.Orders.Repository;
using LogisticsTracker.Orders.Service;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
//for Native AOT compatibility
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
var connectionString = builder.Configuration.GetConnectionString("OrdersDb") ?? "Host=localhost;Database=logisticsorders;Username=postgres;Password=postgres";
builder.Services.AddDbContext<OrdersDbContext>(options =>options.UseNpgsql(connectionString));
builder.Services.AddHttpClient<IInventoryClient, InventoryHttpClient>(client =>
{
    var inventoryUrl = builder.Configuration.GetValue<string>("InventoryServiceUrl") ?? "http://localhost:5142";
    client.BaseAddress = new Uri(inventoryUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigureHttpClient((serviceProvider, client) =>
{
    if (client.BaseAddress == null)
    {
        var inventoryUrl = builder.Configuration.GetValue<string>("services:inventory:http:0")
            ?? builder.Configuration.GetValue<string>("services:inventory:https:0")
            ?? "http://inventory";
        client.BaseAddress = new Uri(inventoryUrl);
    }
});

builder.Services.AddScoped<IOrderRepository, PostgresOrderRepository>();
builder.Services.AddScoped<IOrdersService, OrdersService>();

builder.Services.AddKafkaEventPublisher(builder.Configuration);
builder.Services.AddKafkaEventConsumer<InventoryReleasedConsumer, InventoryReleasedEvent>(
    builder.Configuration,
    groupId: "orders-service",
    topics: "logistics.inventory.released");
builder.Services.AddKafkaEventConsumer<LowStockAlertConsumer, LowStockAlertEvent>(
    builder.Configuration,
    groupId: "orders-service",
    topics: "logistics.low.stock.alert");
builder.Services.AddEventHandler<InventoryReleasedHandler, InventoryReleasedEvent>();
builder.Services.AddEventHandler<LowStockAlertHandler, LowStockAlertEvent>();
//builder.Services.AddSagas();


builder.Logging.AddConsole();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

var ordersApi = app.MapGroup("/api/orders")
    .WithTags("Orders")
    .WithOpenApi();

ordersApi.MapPost("/", async Task<Results<Created<OrderResponse>, BadRequest<string>>> (CreateOrderRequest request, IOrdersService orderService, CancellationToken ct) =>
{
    try
    {
        var order = await orderService.CreateOrderAsync(request, ct);
        var response = OrderResponse.FromOrder(order);
        return TypedResults.Created($"/api/orders/{order.Id}", response);
    }
    catch (InvalidOperationException ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
})
.WithName("CreateOrder")
.WithSummary("Create a new order")
.WithDescription("Creates a new order and returns the order details");

ordersApi.MapGet("/{id:guid}", async Task<Results<Ok<OrderResponse>, NotFound>> (Guid id, IOrdersService orderService, CancellationToken ct) =>
{
    var order = await orderService.GetOrderAsync(id, ct);
    return order is not null ? TypedResults.Ok(OrderResponse.FromOrder(order)) : TypedResults.NotFound();
})
.WithName("GetOrderById")
.WithSummary("Get order by ID")
.WithDescription("Retrieves a specific order by its unique identifier");

ordersApi.MapGet("/number/{orderNumber}", async Task<Results<Ok<OrderResponse>, NotFound>> (string orderNumber, IOrdersService orderService, CancellationToken ct) =>
{
    var order = await orderService.GetOrderAsync(orderNumber, ct);
    return order is not null ? TypedResults.Ok(OrderResponse.FromOrder(order)) : TypedResults.NotFound();
})
.WithName("GetOrderByNumber")
.WithSummary("Get order by order number")
.WithDescription("Retrieves a specific order by its order number (e.g., ORD-20260130-001234)");

ordersApi.MapGet("/", async Task<Ok<PagedResponse<OrderResponse>>> ([AsParameters] OrderQueryParameters queryParams, IOrdersService orderService, CancellationToken ct) =>
{
    var pagedOrders = await orderService.GetOrdersAsync(queryParams, ct);
    return TypedResults.Ok(pagedOrders);
})
.WithName("GetOrders")
.WithSummary("Get all orders")
.WithDescription("Retrieves a paginated list of orders with optional filtering");

ordersApi.MapPut("/{id:guid}/status", async Task<Results<Ok<OrderResponse>, NotFound, BadRequest<string>>> (Guid id, UpdateOrderStatusRequest request, IOrdersService orderService, CancellationToken ct) =>
{
    try
    {
        var order = await orderService.UpdateOrderStatusAsync(id, request, ct);
        return TypedResults.Ok(OrderResponse.FromOrder(order));
    }
    catch (KeyNotFoundException)
    {
        return TypedResults.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
})
.WithName("UpdateOrderStatus")
.WithSummary("Update order status")
.WithDescription("Updates the status of an existing order");

ordersApi.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound, BadRequest<string>>> (Guid id, IOrdersService orderService, CancellationToken ct) =>
{
    try
    {
        var cancelled = await orderService.CancelOrderAsync(id, ct);
        return cancelled ? TypedResults.NoContent() : TypedResults.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
})
.WithName("CancelOrder")
.WithSummary("Cancel order")
.WithDescription("Cancels an order (only if not shipped or delivered)");

//app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow })
//    .WithName("HealthCheck")
//    .WithTags("Health");

app.Run();

[JsonSerializable(typeof(CreateOrderRequest))]
[JsonSerializable(typeof(UpdateOrderStatusRequest))]
[JsonSerializable(typeof(OrderResponse))]
[JsonSerializable(typeof(PagedResponse<OrderResponse>))]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(OrderItem))]
[JsonSerializable(typeof(Address))]
[JsonSerializable(typeof(List<OrderResponse>))]
[JsonSerializable(typeof(Dictionary<Guid, bool>))]
[JsonSerializable(typeof(OrderQueryParameters))]
[JsonSerializable(typeof(List<Guid>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}