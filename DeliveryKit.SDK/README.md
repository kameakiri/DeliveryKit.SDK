# DeliveryKit.SDK

DeliveryKit.SDK は、配送システムを構築するための .NET SDK です。
配送ロジック、ログ基盤、ログアーカイブ、AI 前処理を統合した企業向けの開発キットです。

**Pro / Enterprise プラン向けに提供するのは「開発の型」を示すサンプルであり、
弊社の配送システム本体（プラン別機能・複数キャリア連携・DB永続化・
所有者ベースのアクセス制御を含むデータモデル等）そのものではありません。**
`DeliveryKit.Core`はメモリ上にのみデータを保持する簡易実装です。

---

## ✨ Features

- **DeliveryKit.Core**
  - 配送作成 / 配送取得（メモリ上の簡易実装）
  - 必須項目・制御文字の入力検証（本番相当のロジックそのものではなく型の提示）

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

- **DeliveryKit.ApiTemplate**（兄弟プロジェクトとして提供）
  - 上記コンポーネントを使ったWeb APIの組み方一式（JWT認証・`[Authorize]`・
    入力検証済みエンドポイント）。詳細は `DeliveryKit.ApiTemplate/README.md` を参照。

---

## 📁 Project Structure

```
DeliveryKit.SDK/
├─ src/
│   ├─ DeliveryKit.Core/
│   ├─ DeliveryKit.Logging/
│   ├─ DeliveryKit.LogArchiver/
│   └─ DeliveryKit.AI.Pipeline/
└─ samples/
    └─ BasicDeliveryApp/
```

---

## 🚀 Getting Started

### 1. Install

API プロジェクトから `DeliveryKit.SDK`（または個別に `DeliveryKit.Core` / `DeliveryKit.Logging`）を参照します。

### 2. Register Services (Program.cs)

```csharp
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddSingleton<IDeliveryLogger>(sp =>
    new DeliveryLogger(Path.Combine(AppContext.BaseDirectory, "Logs")));
```

認証（JWT発行・`[Authorize]`での保護）の組み方は、このSDK単体には含まれていません。
`DeliveryKit.ApiTemplate`が実際の配線例です。**配送APIを認証なしで公開しないでください。**

### 3. Use in Controller

```csharp
[Authorize]
[HttpPost("create")]
public IActionResult CreateDelivery([FromBody] CreateDeliveryRequest request)
{
    _logger.Info("CreateDelivery called", new { request });
    var result = _deliveryService.CreateDelivery(request);
    return result.Success ? Ok(result) : BadRequest(result);
}
```

### 4. Test with curl（要: 事前にログインしてトークンを取得）

```
POST /api/delivery/create
Authorization: Bearer <token>

{
  "address": "東京都千代田区1-1-1",
  "recipientName": "山田太郎",
  "recipientPhone": "090-0000-0000",
  "order": {
    "orderId": "ORD-001",
    "senderName": "サンプル株式会社",
    "senderAddress": "大阪府大阪市1-1-1",
    "recipientName": "山田太郎",
    "recipientAddress": "東京都千代田区1-1-1",
    "requestedDeliveryDate": "2026-07-26T10:00:00"
  },
  "package": {
    "weight": 1.5,
    "height": 20,
    "width": 15,
    "depth": 10,
    "description": "サンプル荷物"
  }
}
```

## 📚 Documentation
詳細は `docs` フォルダを参照してください。

## 📄 License
このプロジェクトは MIT ライセンスのもとで公開されています。

## 📝 Changelog
変更履歴は `CHANGELOG.md` を参照してください。
