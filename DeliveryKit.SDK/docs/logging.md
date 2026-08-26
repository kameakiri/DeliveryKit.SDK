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
