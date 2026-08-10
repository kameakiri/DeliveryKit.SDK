# 📘 archiver.md  
（ログ隔離バッチ）

```md
# DeliveryKit.LogArchiver

DeliveryKit.LogArchiver は、180日経過したログを安全に隔離するバッチ処理です。

## Features

- ログ削除ではなく「隔離」
- File.Move のみを使用（安全）
- カテゴリ別ディレクトリに対応

## How It Works

1. ログフォルダを走査
2. 最終更新日が 180 日前のログを検出
3. LogsArchive フォルダへ移動

## Usage

```
DeliveryKit.LogArchiver.exe [basePath] [archivePath] [days]
```

引数は省略可能。省略時は実行ディレクトリ配下の `Logs` / `LogsArchive`、保持期間180日を使う。
環境変数 `DELIVERYKIT_LOG_PATH` / `DELIVERYKIT_LOG_ARCHIVE_PATH` でも上書きできる。
特定ドライブ・特定ユーザー構成の絶対パスをコード側にハードコードしないこと。

Windows タスクスケジューラで 1 日 1 回実行することを推奨します。

