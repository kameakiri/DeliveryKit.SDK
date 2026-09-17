# DeliveryKit.Logging

DeliveryKit.Logging は、配送システム向けのログ基盤です。

## Features

- 1MB ローテーションログ
- JSON 形式で統一
- カテゴリ別ディレクトリ

  **カテゴリは固定ではありません（監査で発覚。以前はここに
  「core / api / error / audit / access」と5つ挙げていました）。**
  `DeliveryLogger` のコンストラクタに渡した文字列がそのままディレクトリ名になり、
  `Info` / `Warn` はその名前、`Error` だけは常に `error` へ書きます。
  用途ごとにインスタンスを分けてください（例: `new DeliveryLogger(category: "audit")`）。

  なお `DeliveryKit.LogArchiver` が走査するのは
  `core` / `api` / `error` / `security` / `audit` / `access` の6つです。
  **これ以外の名前を使うと、隔離も削除もされません。**
- 日本時間（JST）でのタイムスタンプ

## Interface

```csharp
public interface IDeliveryLogger
{
    void Info(string message, object? data = null);
    void Warn(string message, object? data = null);
    void Error(string message, object? data = null);
}
```

## Usage Example

```csharp
// 第1引数（出力先ディレクトリ）は省略可。省略時は実行ディレクトリ配下の "Logs"。
// 特定ドライブ・特定ユーザー構成の絶対パスを前提にすると配布先の環境で
// 動かなくなるため、固定の絶対パスはハードコードしないこと。
var logger = new DeliveryLogger(category: "api");
logger.Info("CreateDelivery called", new { orderId = "ORD-001" });
```

## Log Format

```json
{
  "Timestamp": "2026-07-26 17:10:00",
  "Level": "INFO",
  "Message": "CreateDelivery called",
  "Data": { "orderId": "ORD-001" }
}
```

## `data` に個人情報を渡さないこと（監査で発覚、docs追記）

`Info` / `Warn` / `Error` の第2引数は**そのままJSONへ直列化されて、平文でディスクに残ります。**
配送を扱う以上、手近にあるのは受取人の氏名・住所・電話番号です。
`logger.Info("CreateDelivery called", request)` と書けば、それが丸ごと残ります。

```csharp
// 悪い例：リクエストをそのまま渡す
logger.Info("CreateDelivery called", request);

// 良い例：追跡に必要な識別子だけを渡す
logger.Info("CreateDelivery called", new { orderId = request.OrderId });
```

外部APIの応答も同じです。**エラー応答の本文には、送った内容がそのまま
返ってくることがあります**（400番台で「この住所は不正です」と住所ごと返す実装は珍しくない）。
例外の `Message` に応答本文を混ぜてログへ流すと、そこから漏れます。
本家のDeliveryKitでも同じことが起きており、応答本文をログ向けの文言から外し、
例外の別プロパティへ分離して直しました。

このSDKには次のものが**ありません**。

- 個人情報を落とす仕組み（マスキング・項目の除外）
- 保持期間の自動適用（`DeliveryKit.LogArchiver` を別に動かし、
  さらに `purgeDays` を指定して初めて消えます。`archiver.md` 参照）
- 暗号化（ファイルはそのまま読めます）

つまり、**何を渡すかだけが唯一の防御**です。
個人情報保護方針で「ログに氏名・住所は記録しません」と掲げる場合、
その約束を守れるかどうかは呼び出し側の書き方だけで決まります。

## 制約（監査で発覚、docs追記）

- `getting-started.md`が案内する通り`IDeliveryLogger`は`AddSingleton`で登録し、1プロセス内の
  全リクエストで同一インスタンス・同一ログファイルを共有する構成を前提にしている。
  同時書き込み自体は内部で排他制御（`lock`）しているため、**同一プロセス内**であれば
  安全にログを取りこぼさず記録できる。
- ただし、この排他制御は`.NET`プロセス内でのみ有効。**複数プロセスが同じログ出力先
  パスへ同時に書き込む構成**（例: 同一ホスト上で複数ワーカープロセス/コンテナが
  同じ`Logs`ディレクトリをマウントして共有する構成）には効かず、`File.AppendAllText`
  本来のIOException・書き込み破損のリスクがそのまま残る。1プロセス=1ログ出力先の
  構成を前提にすること。
