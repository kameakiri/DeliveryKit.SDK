# DeliveryKit.AI.Pipeline

ログ行の**最低限のクリーンアップ**を行うサンプル実装です。

> ## 名前の割に、していることは少ない
>
> **この名前から期待されるような処理は入っていません。**（監査で発覚）
> `Normalize()` がしているのは次の2つだけです。
>
> - 空行（空白のみの行を含む）を除去する
> - 各行の前後の空白を `Trim` する
>
> 以前このページは「AI 学習用のクリーンデータ生成」「LLM 学習用のログ整形」
> 「ノイズ除去」「時系列データの維持」を Features / Purpose として挙げていましたが、
> **実装はそのどれも行っていません。** 実装の全文は下に載せてあるとおりです。
>
> DeliveryKit 本体でも同じ指摘があり、単純な規則ベースの機能から「AI」の語を外し、
> 計算方法を画面に明示する形へ直しています
> （`ARCHITECTURE.md` 12章「名前どおりに動いていない機能」）。
> このSDKは顧客提供物なので、同じ基準で書き直します。

> ## ログを外部のAIサービスへ渡す前に
>
> **ログには個人データが入ります。** DeliveryKit 本体のログには操作した利用者名と
> IPアドレスが含まれ、`DeliveryKit.Logging` の `Info/Warn/Error` は任意のオブジェクトを
> `Data` としてそのまま書き出します（`docs/logging.md`）。
>
> **`Normalize()` はマスキングを一切行いません。** 空行を消して前後の空白を落とすだけで、
> 氏名・住所・電話番号・取引先名はそのまま通ります。
>
> 外部のAIサービスへ渡す場合は、**渡してよい情報かどうかを判断し、マスキングを
> 自分で実装してください。** 個人データを第三者へ提供することになるため、
> 利用者への説明と同意の範囲も確認が要ります。

## 何をするか

```csharp
public IEnumerable<string> Normalize(IEnumerable<string> logs)
{
    foreach (var line in logs)
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        yield return line.Trim();
    }
}
```

これが実装の全文です。行の順序は入力のまま変わりません。

## 使い方

```csharp
var preprocessor = new LogPreprocessor();
var normalized = preprocessor.Normalize(
    File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Logs", "core", "2026-07-26.log"))
);
```

## ここから先は自分で書く

用途に応じて必要になるもの（いずれも**このSDKには入っていません**）：

- **機密情報のマスキング**（上記）
- トークナイズ・チャンク分割（モデルごとに要件が違う）
- JSONとしての解析（ログは1行1JSONで書かれているので、文字列のままより扱いやすい）
- 期間・カテゴリでの絞り込み
- 重複の除去

`LogPreprocessor` は、そうした処理を足していく起点として置いてあります。
