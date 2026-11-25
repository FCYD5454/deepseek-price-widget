# DeepSeek 價格小工具（繁體中文・香港）

[繁體中文（香港）](README.zh-HK.md) · [繁體中文（台灣）](README.zh-TW.md) · [简体中文](README.zh-CN.md) · [English](README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Português (Brasil)](README.pt-BR.md)

Windows 桌面懸浮小工具，顯示 DeepSeek API 的高峰／空閒時段與即時價格，支援多貨幣與多語言。

> 非官方工具；價格一律以 DeepSeek 官方定價頁為準。

## 功能

- 依北京時間與中國法定假日判斷**高峰／空閒**時段，並顯示倒數
- 自動抓取官方定價（`pricing.md`），含失敗提示、過期警示與價格變動歷史
- **16 種貨幣**：CNY、HKD、USD、EUR、JPY、GBP、AUD、CAD、CHF、SGD、KRW、THB、BRL、TWD、VND、RUB
  - 匯率來源依序嘗試：open.er-api.com → fawazahmed0 currency-api → frankfurter.app
- **11 種語言**（另可跟隨系統），可選語言自動對應貨幣
- 成本估算器（快取命中率、輸入／輸出 tokens）、服務狀態（官方 RSS）
- 系統匣、全域熱鍵 `Ctrl+Alt+D`、淺色／深色主題、位置記憶與邊緣吸附

## 系統需求

- Windows 10／11（使用內建 .NET Framework 4.8）
- PowerShell 5.1（建置使用內建 `csc.exe`，無需安裝 SDK）

## 建置

```powershell
powershell -ExecutionPolicy Bypass -File src\build-widget.ps1
```

輸出 `DeepSeekPriceWidget.exe` 於專案根目錄；`assets\app.ico` 會由腳本自動生成。

## 使用

雙擊 `DeepSeekPriceWidget.exe` 或 `啟動DeepSeek價格小工具.bat`。

右鍵選單可切換視窗角落、外觀、語言、貨幣，開啟成本估算／價格歷史／快捷鍵說明。
快捷鍵：`Ctrl+Alt+D`（顯示／隱藏）、`Ctrl+E`（估算）、`Ctrl+R`（重新整理價格）、`Ctrl+H`（收到系統匣）、`Ctrl+Shift+Q`（退出）。

## 專案結構

```
assets/   介面 XAML 與圖示（app.ico 由建置生成）
data/     設定、假日、貨幣、匯率來源、價格歷史
lang/     語言包（en, zh-CN, zh-TW, ja, ko, ru, fr, de, es, it, pt-BR）
logs/     日誌與測試輸出（自動建立；不納入版本控制）
src/      原始碼、manifest、建置腳本
```

## 維護（多數情況無需重新編譯）

| 項目 | 檔案 | 方式 |
|---|---|---|
| 假日 | `data/holidays.json` | 編輯後右鍵「重新載入假日表」 |
| 貨幣 | `data/currencies.json` | 新增 `code` / `symbol` / `name`；`languageDefaults` 控制語言→貨幣 |
| 匯率來源 | `data/fx-providers.json` | 陣列順序即優先序；`url` / `container` / `lowerKeys` |
| 語言 | `lang/*.json` | 複製 `en.json` 翻譯並改 `_name`，重啟後出現在選單 |
| 介面 | `assets/*.xaml` | 直接編輯，重啟生效 |
| 程式邏輯 | `src/*.cs` | 關閉程式後執行建置腳本 |

缺少的 `currencies.json`／`fx-providers.json` 會在首次執行時自動生成預設內容。

## 測試

```powershell
DeepSeekPriceWidget.exe --selftest   # 時段、假日、倒數、換算、語言、匯率解析
DeepSeekPriceWidget.exe --smoke      # 視窗建構、托盤、熱鍵、版面量測
DeepSeekPriceWidget.exe --fetchtest  # 官方定價抓取與解析
DeepSeekPriceWidget.exe --statustest # 官方狀態 RSS
DeepSeekPriceWidget.exe --fxtest     # 匯率端點
```

結果輸出於 `logs/`。
