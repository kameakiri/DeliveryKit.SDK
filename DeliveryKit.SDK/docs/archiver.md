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

Windows タスクスケジューラで 1 日 1 回実行することを推奨します。

