# DeliveryKit.Core

DeliveryKit.Core は配送ロジックを提供する SDK コンポーネントです。

> ⚠️ これはサンプル実装です。データはプロセスのメモリ上（`Dictionary`）にのみ保持され、
> 再起動で消えます。弊社の配送システム本体が持つプラン別機能・複数キャリア連携・
> DB永続化・所有者ベースのアクセス制御は含まれていません。
> リクエストDTOの形（便に1つの `OrderId`/`SenderName`/`SenderAddress` +
> **届け先ごと**の `Destinations[].RecipientName`/`RecipientPhone`/`Address`/`Packages[]`）は
> 本家APIと揃えていますが、これは契約の型を示すためで、実装の中身まで
> 一致しているわけではありません。
>
> **この形は2026-10に変わりました。** 以前は配送1件＝届け先1つで、受取人・住所を
> 配送本体と `Order` の両方に持っていました。本家が1便で複数の届け先を回る形へ
> 変わったあともSDKが追従しておらず、**古い形で本家のAPIを呼ぶと
> 「届け先を1つ以上指定してください。」（field: `destinations`）で弾かれます。**

## Features

- 配送作成（`CreateDelivery`）：必須項目と制御文字混入のチェックを行う
  - 必須（便）：`OrderId` / `SenderName` / `SenderAddress` / `CarrierId`
    （`CarrierId` は `yamato` / `sagawa` / `japanpost` のいずれか。**本家でも省略できない**——このSDKは業者へ登録しないが、契約として要る）
  - 必須（届け先）：`Destinations` が1件以上あり、各件の `RecipientName` /
    `RecipientPhone` / `Address` が空でなく、`Packages` が1件以上ある
    （`Destinations`は省略しても既定の空リストが入るため、**省略＝届け先なし**になる。
    本家APIと同じくここで弾く）
  - 項目名は本家と同じ `Destinations[0].Address` の形で返る。
    画面はこの名前でどの欄を赤くするかを決められる
  - 制御文字の拒否：上記に `Notes`（便・届け先の両方）と
    `Destinations[].Packages[].Description` を加えた文字列項目すべて。
    送り状やログにそのまま出る値のため、改行等が混ざると表示崩れやログ改ざんの
    起点になり得る（タブのみ許容）
  - エラーは `DeliveryValidationException` として捕捉され、`result.Success = false`、
    `result.Message` に `"フィールド名: 内容"`、`result.Field` に項目名が入る
    （例外は外へ出ない）。**Field は API から 400 を返すためのもの**で、
    以前これが無かったために、テンプレートが同じエンドポイントから
    2つの形のエラーを返していた（CHANGELOG 参照）
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

- `CreateDeliveryRequest`（`OrderId`, `SenderName`, `SenderAddress`,
  `RequestedDeliveryDate`, `Notes?`, `CarrierId`, `Destinations: List<CreateDeliveryDestinationRequest>`,
  `IdempotencyKey: Guid?`）
- `CreateDeliveryDestinationRequest`（`RecipientName`, `RecipientPhone`, `Address`,
  `Notes?`, `Packages: List<DeliveryPackageRequest>`）
  — **回る順番は受け取らない。** 配列の順に、サーバーが番号を振る
- `DeliveryPackageRequest`（`Weight`, `Height`, `Width`, `Depth`, `Description`）
- `DeliveryInfo`（保存済み配送情報。`Id`, 上記フィールド一式, `Status`, `CreatedAt`）
- `DeliveryResult`（`Success`, `Message`, `Field`, `Delivery`）

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
    OrderId = "ORD-001",
    SenderName = "サンプル株式会社",
    SenderAddress = "大阪府大阪市1-1-1",
    RequestedDeliveryDate = DateTime.UtcNow,
    CarrierId = "yamato",
    IdempotencyKey = Guid.NewGuid(),
    Destinations = new List<CreateDeliveryDestinationRequest>
    {
        new()
        {
            RecipientName = "山田太郎",
            RecipientPhone = "090-0000-0000",
            Address = "東京都千代田区1-1-1",
            Packages = new List<DeliveryPackageRequest>
            {
                new() { Weight = 1.5m, Height = 20, Width = 15, Depth = 10, Description = "サンプル荷物" }
            }
        },
        new()
        {
            RecipientName = "鈴木花子",
            RecipientPhone = "090-1111-1111",
            Address = "東京都千代田区2-2-2",
            Notes = "不在時は宅配ボックス",
            Packages = new List<DeliveryPackageRequest>
            {
                new() { Weight = 0.8m, Height = 10, Width = 10, Depth = 5, Description = "サンプル荷物2" }
            }
        }
    }
});

if (!result.Success)
{
    // result.Message に "フィールド名: エラー内容"、result.Field に項目名が入る。
    // **API から 400 を返すときは Field をそのまま使うこと。**
    // Message から文字列を切り出す必要は無い（DeliveryKit.ApiTemplate の
    // DeliveryController.CreateDelivery が `{ error, field }` で返している）。
}
```
