# DeliveryKit.AI.Pipeline

DeliveryKit.AI.Pipeline は、ログを AI モデルに投入するための前処理を提供します。

## Features

- ログの正規化
- 空行除去
- トリム処理
- AI 学習用のクリーンデータ生成

## Example

```csharp
var preprocessor = new LogPreprocessor();
var normalized = preprocessor.Normalize(
    File.ReadLines(@"ログを配置する場所")
);

What Normalize() Does
Normalize() はログを AI 学習に使えるように最低限クリーンアップする処理です。

空行を除去

空白だけの行を除去

各行の前後の空白を Trim

クリーンなログ行を返す

## 実装

```csharp
public IEnumerable<string> Normalize(IEnumerable<string> logs)
{
    foreach (var line in logs)
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        yield return line.Trim();
    }
}
## Purpose

LLM 学習用のログ整形

ノイズ除去

時系列データの維持

AI モデルが扱いやすいクリーンデータ生成
