# DeliveryKit.SDK Architecture

DeliveryKit.SDK は、配送システムを構築するための .NET SDK です。  
企業向けの配送 API / バッチ処理 / ログ基盤を統一的に扱えるように設計されています。

## Overview

DeliveryKit.SDK は以下の 4 コンポーネントで構成されています。

| Component | Description |
|----------|-------------|
| DeliveryKit.Core | 配送ロジック（Create / Get） |
| DeliveryKit.Logging | ログ基盤（1MB ローテーション） |
| DeliveryKit.LogArchiver | 180日経過ログの隔離バッチ |
| DeliveryKit.AI.Pipeline | AI 学習用ログ前処理 |

## Design Principles

- **API / SDK の責務分離**  
- **安全なログ運用（削除ではなく隔離）**  
- **AI 活用を前提としたログ構造**  
- **拡張性の高いプロジェクト構成**

## Folder Structure

DeliveryKit.SDK/
├─ DeliveryKit.Core/
│   ├─ IDeliveryService.cs
│   ├─ DeliveryService.cs
│   ├─ Models/
│   │   ├─ CreateDeliveryRequest.cs
│   │   ├─ DeliveryInfo.cs
│   │   └─ DeliveryResult.cs
│
├─ DeliveryKit.Logging/
│   ├─ IDeliveryLogger.cs
│   ├─ DeliveryLogger.cs
│   └─ LogWriter.cs
│
├─ DeliveryKit.LogArchiver/
│   ├─ Program.cs
│   └─ LogArchiver.cs
│
└─ DeliveryKit.AI.Pipeline/
├─ LogPreprocessor.cs
└─ Extensions/