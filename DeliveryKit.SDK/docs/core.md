# DeliveryKit.Core

DeliveryKit.Core は配送ロジックを提供する SDK コンポーネントです。

> ⚠️ これはサンプル実装です。データはプロセスのメモリ上（`Dictionary`）にのみ保持され、
> 再起動で消えます。弊社の配送システム本体が持つプラン別機能・複数キャリア連携・
> DB永続化・所有者ベースのアクセス制御は含まれていません。
> リクエストDTOの形（`Address`/`RecipientName`/`RecipientPhone` + `Order`/`Package`の
> ネスト構造）は本家APIと揃えていますが、これは契約の型を示すためで、
> 実装の中身まで一致しているわけではありません。

## Features

- 配送作成（`CreateDelivery`）：必須項目と制御文字混入のチェックを行う
  - 必須：`Address` / `RecipientName` / `RecipientPhone`、および注文の
    `Order.OrderId` / `Order.SenderName` / `Order.SenderAddress` /
    `Order.RecipientName` / `Order.RecipientAddress`
    （`Order`は省略しても既定のインスタンスが入るため、**省略＝空の注文**になる。
    本家APIと同じくここで弾く）
  - 制御文字の拒否：上記に `Notes` と `Package.Description` を加えた文字列項目すべて。
    送り状やログにそのまま出る値のため、改行等が混ざると表示崩れやログ改ざんの
    起点になり得る（タブのみ許容）
  - エラーは `DeliveryValidationException` として捕捉され、`result.Success = false`、
    `result.Message` に `"フィールド名: 内容"` が入る（例外は外へ出ない）
- 配送情報取得（`GetDelivery`）
- メモリストアによる簡易管理（サンプル実装）

## Interfaces

### IDeliveryService

```csharp
public interface IDeliveryService
{
    DeliveryResult CreateDelivery(CreateDeliveryRequest request);
    DeliveryInfo? GetDelivery(string id);
}
```

### Models

- `CreateDeliveryRequest`（`Address`, `RecipientName`, `RecipientPhone`, `Notes?`,
  `Order: DeliveryOrderRequest`, `Package: DeliveryPackageRequest`, `IdempotencyKey: Guid?`）
- `DeliveryOrderRequest`（`OrderId`, `SenderName`, `SenderAddress`, `RecipientName`,
  `RecipientAddress`, `RequestedDeliveryDate`）
- `DeliveryPackageRequest`（`Weight`, `Height`, `Width`, `Depth`, `Description`）
- `DeliveryInfo`（保存済み配送情報。`Id`, 上記フィールド一式, `Status`, `CreatedAt`）
- `DeliveryResult`（`Success`, `Message`, `Delivery`）

内部主キー（`Id`等）はリクエストDTOに含めていません。呼び出し側が指定した`Id`で
既存データを上書きできてしまう問題（マスアサインメント）を構造的に防ぐためです。

## Usage Example

> 監査で発覚（Medium）：`CreateDeliveryRequest.IdempotencyKey`が必須化されたコミットで
> README.md/docs/getting-started.mdのサンプルは追従済みだったが、このファイルだけ
> 追従漏れしていた。`IdempotencyKey`を指定せずに呼ぶと`DeliveryValidationException`が
> 投げられ`result.Success = false`になる（ネットワーク再送・二重クリックで同じ配送が
> 2件作成されるのを防ぐための必須パラメータ）。

```csharp
var service = new DeliveryService();

var result = service.CreateDelivery(new CreateDeliveryRequest
{
    Address = "東京都千代田区1-1-1",
    RecipientName = "山田太郎",
    RecipientPhone = "090-0000-0000",
    IdempotencyKey = Guid.NewGuid(),
    Order = new DeliveryOrderRequest
    {
        OrderId = "ORD-001",
        SenderName = "サンプル株式会社",
        SenderAddress = "大阪府大阪市1-1-1",
        RecipientName = "山田太郎",
        RecipientAddress = "東京都千代田区1-1-1",
        RequestedDeliveryDate = DateTime.UtcNow
    },
    Package = new DeliveryPackageRequest
    {
        Weight = 1.5m,
        Height = 20,
        Width = 15,
        Depth = 10,
        Description = "サンプル荷物"
    }
});

if (!result.Success)
{
    // result.Message に "フィールド名: エラー内容" が入る
}
```
