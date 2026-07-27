# DeliveryKit.Logging

DeliveryKit.Logging は、配送システム向けのログ基盤です。

## Features

- 1MB ローテーションログ
- JSON 形式で統一
- カテゴリ別ディレクトリ（core / api / error / audit / access）
- 日本時間（JST）でのタイムスタンプ

## Interface

```csharp
public interface IDeliveryLogger
{
    void Info(string message, object? data = null);
    void Warn(string message, object? data = null);
    void Error(string message, object? data = null);
}

## Usage Example

```csharp
var logger = new DeliveryLogger(@"C:\DeliveryKit\Api\Logs");
logger.Info("CreateDelivery called", new { orderId = "ORD-001" });

## Log Format

{
  "Timestamp": "2026-07-26 17:10:00",
  "Level": "INFO",
  "Message": "CreateDelivery called",
  "Data": { "orderId": "ORD-001" }
}
