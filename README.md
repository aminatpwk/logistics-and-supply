# LogisticsTracker - Microservices Architecture

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Native AOT](https://img.shields.io/badge/Native%20AOT-Enabled-success)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
[![Microservices](https://img.shields.io/badge/Architecture-Microservices-orange)](https://microservices.io/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-512BD4)](https://asp.net/)
[![Status](https://img.shields.io/badge/Status-Active%20Development-brightgreen)](https://github.com)

A logistics tracking system built with **.NET 10** and **C# 14** demonstrating modern microservices patterns and the latest language features.

---

## Overview

LogisticsTracker is a **microservices-based logistics management system** designed to showcase modern .NET development practices. The project demonstrates:

- **Microservices Architecture** 
- **Service-to-Service Communication** - HTTP-based integration
- **Domain-Driven Design** 
- **Modern C# Features** - C# 14 preview features
- **.NET 10 Capabilities** - Native AOT, TimeProvider, improved APIs

---

## Technologies Used

### Core Framework
- **.NET 10** 
- **C# 14 (Preview)**
- **ASP.NET Core Minimal APIs** 
- **Native AOT** 

### Libraries & Tools
- **System.Text.Json** 
- **HttpClient** 
- **OpenAPI** 
- **TimeProvider**

### Infrastructure
- **Apache Kafka** 
- **PostgreSQL** 
- **Redis** 
- **.NET Aspire** 

---

## Services

### 1. Orders Service

**Purpose:** Manage customer orders and order lifecycle

**Endpoints:**
```
POST   /api/orders              - Create order
GET    /api/orders              - List orders (paginated)
GET    /api/orders/{id}         - Get order by ID
GET    /api/orders/number/{num} - Get order by order number
PUT    /api/orders/{id}/status  - Update order status
DELETE /api/orders/{id}         - Cancel order
```

---

### 2. Inventory Service

**Purpose:** Manage product inventory and stock reservations

**Endpoints:**
```
POST   /api/inventory              - Create inventory item
GET    /api/inventory              - List all inventory
GET    /api/inventory/{productId}  - Get by product ID
GET    /api/inventory/sku/{sku}    - Get by SKU
GET    /api/inventory/low-stock    - Get low stock items
PUT    /api/inventory/{id}/stock   - Update stock levels
POST   /api/inventory/reserve      - Reserve inventory
POST   /api/inventory/release/{id} - Release reservation
```

---
Inventory and Orders services orchestrated through Aspire:

<img width="928" height="694" alt="image" src="https://github.com/user-attachments/assets/8edc1312-ce56-4b31-99c4-10d5acdeac30" />

---

## License

This project is created for educational purposes to demonstrate modern .NET and C# features.


---

*treat people with kindness :)*
