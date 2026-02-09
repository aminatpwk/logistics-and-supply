using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var kafka = builder.AddKafka("kafka").WithKafkaUI().WithDataVolume();
var redis = builder.AddRedis("redis").WithDataVolume();
var postgres = builder.AddPostgres("postgres").WithPgAdmin();

var inventoryDb = postgres.AddDatabase("logisticsinventory");
var ordersDb = postgres.AddDatabase("logisticsorders");

var ordersService = builder.AddProject<LogisticsTracker_Orders>("orders-service")
    .WithReference(kafka)
    .WithReplicas(1)
    .WithHttpEndpoint(port: 5298, name: "orders")
    .WithHttpsEndpoint(port: 7199, name: "ordershttps")
    .WithEnvironment("InventoryServiceUrl", "http://localhost:5142");
var inventoryService = builder.AddProject<LogisticsTracker_Inventory>("inventory-service")
    .WithReference(kafka)
    .WithReference(redis)
    .WithReplicas(1)
    .WithHttpEndpoint(port: 5142, name: "inventory");

builder.Build().Run();
