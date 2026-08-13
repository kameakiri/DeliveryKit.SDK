# DeliveryKit.SDK Architecture

DeliveryKit.SDK は、配送システムを構築するための .NET SDK です。
企業向けの配送 API / バッチ処理 / ログ基盤を統一的に扱えるように設計されています。

> ⚠️ このSDKは「開発の型」を示すサンプルであり、弊社の配送システム本体
> （プラン別機能・複数キャリア連携・DB永続化・IDOR対策等）そのものではありません。
> 詳細は各コンポーネントのドキュメント、および `README.md` を参照してください。

## Overview

DeliveryKit.SDK は以下の 4 コンポーネントで構成されています。

| Component | Description |
|----------|-------------|
| DeliveryKit.Core | 配送ロジック（Create / Get、インメモリのサンプル実装） |
| DeliveryKit.Logging | ログ基盤（1MB ローテーション） |
| DeliveryKit.LogArchiver | 180日経過ログの隔離バッチ |
| DeliveryKit.AI.Pipeline | AI 学習用ログ前処理 |

これとは別に、これらを使ったWeb APIの組み方一式（JWT認証・`[Authorize]`・
入力検証済みエンドポイント）を示すサンプルを `DeliveryKit.ApiTemplate`
（兄弟リポジトリ）として提供しています。

## Design Principles

- API / SDK の責務分離
- 安全なログ運用（削除ではなく隔離）
- AI 活用を前提としたログ構造
- 拡張性の高いプロジェクト構成

## Folder Structure

```
DeliveryKit.SDK/
├─ DeliveryKit.SDK.csproj        … Core/Logging/AI.Pipelineを束ねるファサード
├─ src/
│   ├─ DeliveryKit.Core/
│   │   ├─ Interfaces/IDeliveryService.cs
│   │   ├─ Services/DeliveryService.cs
│   │   ├─ DeliveryValidationException.cs
│   │   └─ Models/
│   │       ├─ CreateDeliveryRequest.cs
│   │       ├─ DeliveryInfo.cs
│   │       └─ DeliveryResult.cs
│   │
│   ├─ DeliveryKit.Logging/
│   │   ├─ Interfaces/IDeliveryLogger.cs
│   │   └─ DeliveryLogger.cs
│   │
│   ├─ DeliveryKit.LogArchiver/
│   │   └─ Program.cs
│   │
│   └─ DeliveryKit.AI.Pipeline/
│       └─ LogPreprocessor.cs
│
└─ samples/
    └─ BasicDeliveryApp/
        └─ Program.cs
```
