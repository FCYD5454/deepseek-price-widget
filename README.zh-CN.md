# DeepSeek 价格小工具

[繁體中文](README.md) · [English](README.en.md) · **简体中文** · [日本語](README.ja.md) · [Русский](README.ru.md)

Windows 桌面悬浮小工具，显示 DeepSeek API 的高峰/空闲时段与实时价格，支持多货币与多语言。

> 非官方工具；价格一律以 DeepSeek 官方定价页为准。

## 功能

- 按北京时间与中国法定节假日判断**高峰/空闲**时段，显示倒计时
- 自动抓取官方定价（`pricing.md`），含失败提示、过期警示与价格变动历史
- **16 种货币**：CNY、HKD、USD、EUR、JPY、GBP、AUD、CAD、CHF、SGD、KRW、THB、BRL、TWD、VND、RUB
  - 汇率来源依次尝试：open.er-api.com → fawazahmed0 currency-api → frankfurter.app
- **11 种语言**（另可跟随系统），语言可自动对应货币
- 成本估算器（缓存命中率、输入/输出 tokens）、服务状态（官方 RSS）
- 系统托盘、全局热键 `Ctrl+Alt+D`、浅色/深色主题、位置记忆与吸附

## 系统需求

- Windows 10/11（使用内置 .NET Framework 4.8）
- PowerShell 5.1（构建使用内置 `csc.exe`，无需安装 SDK）

## 构建

```powershell
powershell -ExecutionPolicy Bypass -File src\build-widget.ps1
```

输出 `DeepSeekPriceWidget.exe` 于项目根目录；`assets\app.ico` 由脚本自动生成。

## 使用

双击 `DeepSeekPriceWidget.exe` 或 `啟動DeepSeek價格小工具.bat`。

右键菜单可切换窗口角落、外观、语言、货币，打开成本估算/价格历史/快捷键说明。
默认快捷键：`Ctrl+Alt+D`（显示/隐藏）、`Ctrl+E`（估算）、`Ctrl+R`（刷新价格）、`Ctrl+H`（收到系统托盘）、`Ctrl+Shift+Q`（退出）。

## 项目结构

```
assets/   界面 XAML 与图标（app.ico 由构建生成）
data/     设置、节假日、货币、汇率来源、价格历史
lang/     语言包（en, zh-CN, zh-TW, ja, ko, ru, fr, de, es, it, pt-BR）
logs/     日志与测试输出（自动建立；不纳入版本控制）
src/      源码、manifest、构建脚本
```

## 维护（多数情况无需重新编译）

| 项目 | 文件 | 方式 |
|---|---|---|
| 节假日 | `data/holidays.json` | 编辑后右键“重新载入假日表” |
| 货币 | `data/currencies.json` | 新增 `code` / `symbol` / `name`；`languageDefaults` 控制语言→货币 |
| 汇率来源 | `data/fx-providers.json` | 数组顺序即优先序；`url` / `container` / `lowerKeys` |
| 语言 | `lang/*.json` | 复制 `en.json` 翻译并修改 `_name`，重启后出现在菜单 |
| 界面 | `assets/*.xaml` | 直接编辑，重启生效 |
| 程序逻辑 | `src/*.cs` | 关闭程序后执行构建脚本 |

缺少的 `currencies.json` / `fx-providers.json` 会在首次运行时自动生成默认内容。

## 测试

```powershell
DeepSeekPriceWidget.exe --selftest   # 时段、节假日、倒计时、换算、语言、汇率解析
DeepSeekPriceWidget.exe --smoke      # 窗口构建、托盘、热键、版面量测
DeepSeekPriceWidget.exe --fetchtest  # 官方定价抓取与解析
DeepSeekPriceWidget.exe --statustest # 官方状态 RSS
DeepSeekPriceWidget.exe --fxtest     # 汇率端点
```

结果输出于 `logs/`。
