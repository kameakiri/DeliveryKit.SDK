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
        // requestオブジェクト全体（住所・受取人氏名・電話番号などPII）をそのまま
        // ログに書き込まない。記録するのは追跡に必要な最小限の項目（例: orderId）に
        // 限定する（DeliveryKit.ApiTemplate.Controllers.DeliveryControllerと同じ方針）。
        var orderId = request.OrderId;
        _logger.Info("CreateDelivery called",
            new { orderId, destinations = request.Destinations?.Count ?? 0 });
        var result = _deliveryService.CreateDelivery(request);
        if (!result.Success)
        {
            // **失敗の形を1つにすること。** モデルのバインドに失敗した場合、
            // ASP.NET はコントローラーへ入る前に応答を返す。`DeliveryKit.ApiTemplate`
            // はそれを `{ error, field }` に揃えてあるので、こちらも同じ形で返す。
            // `BadRequest(result)` と書くと `{ success, message, delivery }` になり、
            // **同じエンドポイントから2つの形のエラーが返る。**
            return BadRequest(new { error = result.Message, field = result.Field ?? string.Empty });
        }
        return Ok(result);
    }
}
```

`CreateDelivery`が`result.Success = false`を返す条件（`DeliveryValidationException`を
内部で捕捉）：

- **必須項目が空**：便の `OrderId` / `SenderName` / `SenderAddress`、および
  各届け先の `Destinations[i].RecipientName` / `RecipientPhone` / `Address`
- **届け先が無い**：`Destinations` が空（省略しても既定の空リストが入るため、
  **省略＝届け先なし**になります）
- **荷物が無い**：`Destinations[i].Packages` が空（1軒に複数口あってよい）
- **制御文字を含む**：上記に `Notes`（便・届け先の両方）と
  `Destinations[i].Packages[j].Description` を加えた文字列項目すべて（タブのみ許容）
- **`IdempotencyKey` が未指定**（クライアントが1操作につき1つ生成するGuid）

`result.Field` に項目名、`result.Message` に `"項目名: 内容"` が入ります。
詳細は `docs/core.md`。

## 5. Test with curl（要: 事前にログインしてトークンを取得）

> **下の資格情報は開発用の既定値です。** `DeliveryKit.ApiTemplate` は、
> 開発環境以外で `Sample:Username` / `Sample:PasswordHash` が未設定のまま起動すると
> **例外を投げて止まります**（ソース中の既定値をそのまま本番相当の環境で
> 使わせないため）。動作確認を終えたら、環境変数等で自分の値を設定してください。

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
  "orderId": "ORD-001",
  "senderName": "サンプル株式会社",
  "senderAddress": "大阪府大阪市1-1-1",
  "requestedDeliveryDate": "2026-07-26T10:00:00",
  "destinations": [
    {
      "recipientName": "山田太郎",
      "recipientPhone": "090-0000-0000",
      "address": "東京都千代田区1-1-1",
      "packages": [
        { "weight": 1.5, "height": 20, "width": 15, "depth": 10, "description": "サンプル荷物" }
      ]
    }
  ],
  "idempotencyKey": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

`idempotencyKey`はクライアントが1回の作成操作につき1回だけ生成するGuid（README.md参照）。
