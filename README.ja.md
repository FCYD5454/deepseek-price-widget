# DeepSeek 価格ウィジェット

[繁體中文](README.md) · [English](README.en.md) · [简体中文](README.zh-CN.md) · **日本語** · [Русский](README.ru.md)

DeepSeek API のピーク／オフピーク時間帯と最新価格を表示する Windows デスクトップ用のフローティングウィジェットです。多通貨・多言語に対応します。

> 非公式ツールです。価格は必ず DeepSeek 公式の料金ページをご確認ください。

## 機能

- 北京時間と中国の祝日を基準にピーク／オフピークを判定し、カウントダウンを表示
- 公式 `pricing.md` から価格を自動取得（失敗・古いデータの表示、価格変更履歴つき）
- **16 通貨**：CNY、HKD、USD、EUR、JPY、GBP、AUD、CAD、CHF、SGD、KRW、THB、BRL、TWD、VND、RUB
  - 為替取得の順序：open.er-api.com → fawazahmed0 currency-api → frankfurter.app
- **11 言語**（システムに従うことも可能）、言語に応じた通貨の自動切替オプション
- コスト計算（キャッシュヒット率、入力／出力トークン）、サービス状態（公式 RSS）
- トレイ、グローバルホットキー `Ctrl+Alt+D`、ライト／ダークテーマ、位置記憶と画面端スナップ

## 動作環境

- Windows 10/11（内蔵の .NET Framework 4.8 を使用）
- PowerShell 5.1（ビルドは内蔵の `csc.exe` を使用。SDK 不要）

## ビルド

```powershell
powershell -ExecutionPolicy Bypass -File src\build-widget.ps1
```

プロジェクト直下に `DeepSeekPriceWidget.exe` を出力します。`assets\app.ico` はスクリプトが生成します。

## 使い方

`DeepSeekPriceWidget.exe` または `啟動DeepSeek價格小工具.bat` をダブルクリックします。

右クリックでコーナー、外観、言語、通貨、コスト計算、価格履歴、ショートカットを操作できます。
ショートカット：`Ctrl+Alt+D`（表示／非表示）、`Ctrl+E`（計算）、`Ctrl+R`（価格更新）、`Ctrl+H`（トレイへ）、`Ctrl+Shift+Q`（終了）。

## 構成

```
assets/   XAML UI とアイコン（app.ico はビルド時に生成）
data/     設定、祝日、通貨、為替プロバイダー、価格履歴
lang/     言語パック（en, zh-CN, zh-TW, ja, ko, ru, fr, de, es, it, pt-BR）
logs/     ログとテスト出力（実行時に生成。バージョン管理対象外）
src/      C# ソース、manifest、ビルドスクリプト
```

## メンテナンス（多くは再ビルド不要）

| 項目 | ファイル | 方法 |
|---|---|---|
| 祝日 | `data/holidays.json` | 編集後、右クリック「休日データを再読込」 |
| 通貨 | `data/currencies.json` | `code` / `symbol` / `name` を追加。`languageDefaults` が言語→通貨 |
| 為替プロバイダー | `data/fx-providers.json` | 配列順が優先順位。`url` / `container` / `lowerKeys` |
| 言語 | `lang/*.json` | `en.json` を複製して翻訳し `_name` を変更。再起動でメニューに表示 |
| UI | `assets/*.xaml` | 編集して再起動 |
| ロジック | `src/*.cs` | ウィジェットを閉じてからビルドスクリプトを実行 |

`currencies.json` / `fx-providers.json` が無い場合は初回起動時に既定値で生成されます。

## テスト

```powershell
DeepSeekPriceWidget.exe --selftest   # 時間帯、祝日、カウントダウン、換算、i18n、為替解析
DeepSeekPriceWidget.exe --smoke      # ウィンドウ構築、トレイ、ホットキー、レイアウト計測
DeepSeekPriceWidget.exe --fetchtest  # 公式価格の取得と解析
DeepSeekPriceWidget.exe --statustest # 公式ステータス RSS
DeepSeekPriceWidget.exe --fxtest     # 為替エンドポイント
```

結果は `logs/` に出力されます。
