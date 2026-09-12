## 0.3.0

**この版は 0.2.0 から利用者のコードに影響する変更を含みます。**
以下は 0.2.0 の公開以降に入れた変更で、**これまでCHANGELOGへの記載が漏れていました**
（監査で発覚。10件ほど遅れており、そのうち2件は利用者のコードを壊す変更でした。
変更履歴は「上げてよいか」を判断するためのものなので、載っていないと判断できません）。

### 破壊的変更

- **`CreateDeliveryRequest.IdempotencyKey` を必須にしました。**
  未指定のリクエストは400になります。クライアント側で1操作につき1つGuidを生成し、
  再送時は同じ値を送ってください。サーバー側でGuidを作っても、リクエストごとに別の値に
  なるため再送を同じ操作だと判別できません。
- **`DeliveryKit.ApiTemplate` の名前空間を `DeliveryKit.Api.*` から
  `DeliveryKit.ApiTemplate.*` へ変更しました。** 本家APIと同じ名前空間だと、
  両方を参照したときに型が衝突します。テンプレートをコピーして使う場合は
  自分の名前空間へ置き換えてください。

### DeliveryKit.ApiTemplate

- `/health` を追加（`GET` と `HEAD` の両方、認証不要）。稼働監視・ロードバランサの
  ヘルスプローブ用。**`AllowAnonymous` を外すと監視から401になり「常に停止中」と見えます。**
  DBには接続しません（DBの一時的な不調でオリジンごと切り離されるのを避けるため）。
- 本文のバインドに失敗したときの応答を `{ error, field }` に統一。
  既定の ProblemDetails はフレームワークの英語メッセージで、画面にそのまま出せません。
- 未認証で受け付けるPOSTに `[RequestSizeLimit]` を追加。指定しないとKestrelの既定
  （30MB）まで本文を受け取ってモデルバインドが解析します。
- 書き込み系エンドポイントとログインにレート制限、ログイン監査ログを追加。
- JWT検証の `ClockSkew` を明示。
- 冪等性の指針をREADMEに追加。

### DeliveryKit.SDK

- README / `docs/getting-started.md` のサンプルが、ログへ個人情報を書く形になっていたのを是正。
- `docs/core.md` のサンプルを `IdempotencyKey` 必須化に追従。
- `DeliveryKit.LogArchiver` の `Nullable` / `ImplicitUsings` を他プロジェクトと統一。
- READMEがUTF-16で保存されGitHubで文字化けしていたのを修正。

## 0.2.0
- プロジェクト参照が `<None Include>` のままで実際にはビルドされていなかった問題を修正
  （`DeliveryKit.SDK.csproj` / `DeliveryKit.ApiTemplate.csproj` を実際の `ProjectReference` に変更）
- `DeliveryKit.Core` のDTOを本家 `DeliveryKit.Api` の契約（Address/RecipientName/RecipientPhone
  + Order/Packageのネスト構造）に合わせ、必須項目・制御文字の入力検証を追加
- `DeliveryLogger` / `DeliveryKit.LogArchiver` にハードコードされていた絶対パス
  （`C:\DeliveryKit\Api\Logs`）を廃止し、既定は実行ディレクトリ相対、環境変数/引数で上書き可能に変更
  （`DeliveryLogger`の1引数コンストラクタが実際には`basePath`ではなく`category`に
  バインドされていた不整合も修正）
- `DeliveryKit.ApiTemplate` にJWT認証（`AuthController` サンプル + `[Authorize]`）を追加。
  以前は認証なしでAPIを叩ける状態だった
- `samples/BasicDeliveryApp` に `.csproj` が存在せず単体ビルドできなかった問題を修正

## 0.1.0
- 初期バージョン
- DeliveryKit.Core 追加
- DeliveryKit.Logging 追加
- DeliveryKit.LogArchiver 追加
- DeliveryKit.AI.Pipeline 追加
