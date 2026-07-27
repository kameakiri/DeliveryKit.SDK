# DeliveryKit.Core

DeliveryKit.Core は配送ロジックを提供する SDK コンポーネントです。

## Features

- 配送作成（CreateDelivery）
- 配送情報取得（GetDelivery）
- メモリストアによる簡易管理（サンプル実装）

## Interfaces

### IDeliveryService

```csharp
public interface IDeliveryService
{
    DeliveryResult CreateDelivery(CreateDeliveryRequest request);
    DeliveryInfo? GetDelivery(string id);
}

### Models
CreateDeliveryRequest

DeliveryInfo

DeliveryResult

### Usage Example

```csharp
var service = new DeliveryService();

var result = service.CreateDelivery(new CreateDeliveryRequest
{
    OrderId = "ORD-001",
    Address = "東京都千代田区",
    RequestedDate = DateTime.Now
});
