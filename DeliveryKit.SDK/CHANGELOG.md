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
