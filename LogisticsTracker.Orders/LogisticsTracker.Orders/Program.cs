using LogisticsTracker.Orders.Models;
using LogisticsTracker.Orders.Models.DTOs;
using LogisticsTracker.Orders.Repository;
using LogisticsTracker.Orders.Service;
using Microsoft.AspNetCore.Http.HttpResults;
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

// Register repositories and services
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<IOrdersService, OrdersService>();

builder.Logging.AddConsole();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

var ordersApi = app.MapGroup("/api/orders")
    .WithTags("Orders")
    .WithOpenApi();

// POST /api/orders
ordersApi.MapPost("/", async Task<Results<Created<OrderResponse>, BadRequest<string>>> (CreateOrderRequest request,IOrdersService orderService,CancellationToken ct) =>
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

// GET /api/orders/{id} 
ordersApi.MapGet("/{id:guid}", async Task<Results<Ok<OrderResponse>, NotFound>> (Guid id,IOrdersService orderService,CancellationToken ct) =>
{
    var order = await orderService.GetOrderAsync(id, ct);
    return order is not null ? TypedResults.Ok(OrderResponse.FromOrder(order)) : TypedResults.NotFound();
})
.WithName("GetOrderById")
.WithSummary("Get order by ID")
.WithDescription("Retrieves a specific order by its unique identifier");

// GET /api/orders/number/{orderNumber} 
ordersApi.MapGet("/number/{orderNumber}", async Task<Results<Ok<OrderResponse>, NotFound>> (string orderNumber,IOrdersService orderService,CancellationToken ct) =>
{
    var order = await orderService.GetOrderAsync(orderNumber, ct);
    return order is not null ? TypedResults.Ok(OrderResponse.FromOrder(order)) : TypedResults.NotFound();
})
.WithName("GetOrderByNumber")
.WithSummary("Get order by order number")
.WithDescription("Retrieves a specific order by its order number (e.g., ORD-20260130-001234)");

// GET /api/orders 
ordersApi.MapGet("/", async Task<Ok<PagedResponse<OrderResponse>>> ([AsParameters] OrderQueryParameters queryParams,IOrdersService orderService,CancellationToken ct) =>
{
    var pagedOrders = await orderService.GetOrdersAsync(queryParams, ct);
    return TypedResults.Ok(pagedOrders);
})
.WithName("GetOrders")
.WithSummary("Get all orders")
.WithDescription("Retrieves a paginated list of orders with optional filtering");

// PUT /api/orders/{id}/status 
ordersApi.MapPut("/{id:guid}/status", async Task<Results<Ok<OrderResponse>, NotFound, BadRequest<string>>> (Guid id,UpdateOrderStatusRequest request,IOrdersService orderService,CancellationToken ct) =>
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

// DELETE /api/orders/{id} 
ordersApi.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound, BadRequest<string>>> (Guid id,IOrdersService orderService,CancellationToken ct) =>
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

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow })
    .WithName("HealthCheck")
    .WithTags("Health");

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
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}