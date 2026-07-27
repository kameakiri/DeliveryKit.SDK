# DeliveryKit.SDK

DeliveryKit.SDK は、配送システムを構築するための .NET SDK です。  
配送ロジック、ログ基盤、ログアーカイブ、AI 前処理を統合した企業向けの開発キットです。

---

## ✨ Features

- **DeliveryKit.Core**  
  - 配送作成 / 配送取得  
  - シンプルな配送ロジック

- **DeliveryKit.Logging**  
  - JSON ログ出力  
  - 1MB ローテーション  
  - カテゴリ別ログ管理（core / api / error / audit / access）

- **DeliveryKit.LogArchiver**  
  - 180日経過ログの安全な隔離  
  - Windows タスクスケジューラで運用可能

- **DeliveryKit.AI.Pipeline**  
  - ログの正規化  
  - AI 学習用の前処理

---

## 📁 Project Structure

DeliveryKit.SDK/
├─ DeliveryKit.Core/
├─ DeliveryKit.Logging/
├─ DeliveryKit.LogArchiver/
└─ DeliveryKit.AI.Pipeline/


---

## 🚀 Getting Started

### 1. Install

API プロジェクトから DeliveryKit.SDK を参照します。

### 2. Register Services (Program.cs)

```csharp
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddSingleton<IDeliveryLogger>(sp =>
    new DeliveryLogger(@"ログを配置する場所"));

### 3. Use in Controller

```csharp
[HttpPost("create")]
public IActionResult CreateDelivery([FromBody] CreateDeliveryRequest request)
{
    _logger.Info("CreateDelivery called", new { request });
    var result = _deliveryService.CreateDelivery(request);
    return Ok(result);
}

### 4. Test with Postman

コード
POST /api/delivery/create
{
  "orderId": "ORD-001",
  "address": "東京都千代田区",
  "requestedDate": "2026-07-26T10:00:00"
}

📚 Documentation
詳細は docs フォルダを参照してください。

📄 License
このプロジェクトは MIT ライセンスのもとで公開されています。

📝 Changelog
変更履歴は CHANGELOG.md を参照してください。
