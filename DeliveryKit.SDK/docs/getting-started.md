# Getting Started with DeliveryKit.SDK

> 最初から動くものが欲しい場合は、このSDKを直接組み込むより
> `DeliveryKit.ApiTemplate`をコピーして始める方が早いです
> （JWT認証・`[Authorize]`・入力検証が最初から配線済み）。

## 1. Install

`DeliveryKit.SDK` をソリューションに追加し、API プロジェクトから参照します。

## 2. Register Services (Program.cs)

```csharp
// 必ず Singleton で登録すること（Scopedにすると、リクエストのたびにこのインスタンスごと
// 作り直され、内部の保存先が毎回空になって「作成した配送がGETで見つからない」という
// 不具合になる。DeliveryService.csのコメント参照）。
builder.Services.AddSingleton<IDeliveryService, DeliveryService>();
builder.Services.AddSingleton<IDeliveryLogger>(sp =>
    new DeliveryLogger(Path.Combine(AppContext.BaseDirectory, "Logs")));
```

`DeliveryLogger`の第1引数はログ出力先ディレクトリ（省略時は実行ディレクトリ配下の`Logs`）、
第2引数はカテゴリ名（省略時は`"core"`）です。

## 3. Add Authentication

このSDK自体は認証機能を提供しません。**配送APIを認証なしで公開しないでください。**
JWT認証の組み方は `DeliveryKit.ApiTemplate`（`Program.cs` / `Controllers/AuthController.cs`）を
参照してください。

## 4. Use in Controller

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DeliveryController : ControllerBase
{
    [HttpPost("create")]
    public IActionResult CreateDelivery([FromBody] CreateDeliveryRequest request)
    {
        _logger.Info("CreateDelivery called", new { request });
        var result = _deliveryService.CreateDelivery(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
```

`CreateDelivery`は`Address`/`RecipientName`/`RecipientPhone`が空、または制御文字を
含む場合に`result.Success = false`を返します（`DeliveryValidationException`を内部で捕捉）。

## 5. Test with curl（要: 事前にログインしてトークンを取得）

```
POST /api/auth/login
{
  "username": "demo",
  "password": "ChangeMe123!"
}
```

```
POST /api/delivery/create
Authorization: Bearer <ログインで取得したtoken>

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
