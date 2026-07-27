# Getting Started with DeliveryKit.SDK

## 1. Install

DeliveryKit.SDK をソリューションに追加し、API プロジェクトから参照します。

## 2. Register Services (Program.cs)

```csharp
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddSingleton<IDeliveryLogger>(sp =>
    new DeliveryLogger(@"C:\DeliveryKit\Api\Logs"));

## 3. Use in Controller

```csharp
[HttpPost("create")]
public IActionResult CreateDelivery([FromBody] CreateDeliveryRequest request)
{
    _logger.Info("CreateDelivery called", new { request });
    var result = _deliveryService.CreateDelivery(request);
    return Ok(result);
}

## 4. Test with Postman

POST /api/delivery/create
{
  "orderId": "ORD-001",
  "address": "東京都千代田区",
  "requestedDate": "2026-07-26T10:00:00"
}
