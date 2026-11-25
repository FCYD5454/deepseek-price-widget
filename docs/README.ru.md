# Виджет цен DeepSeek

[繁體中文](README.zh-Hant.md) · [简体中文](README.zh-CN.md) · [English](../README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Português (Brasil)](README.pt-BR.md)

Плавающий виджет для рабочего стола Windows: показывает пиковые/внепиковые периоды API DeepSeek и актуальные цены. Поддерживает несколько валют и языков.

> Неофициальный инструмент. Окончательные цены — на официальной странице DeepSeek.

## Возможности

- Определение пика/вне пика по пекинскому времени и праздникам Китая, с обратным отсчётом
- Автоматическая загрузка цен с официального `pricing.md`; индикаторы ошибок/устаревания и история изменений
- **16 валют**: CNY, HKD, USD, EUR, JPY, GBP, AUD, CAD, CHF, SGD, KRW, THB, BRL, TWD, VND, RUB
  - Источники курсов по порядку: open.er-api.com → fawazahmed0 currency-api → frankfurter.app
- **11 языков** (или как в системе), опция автоматической валюты по языку
- Калькулятор стоимости (доля кэша, входные/выходные токены), статус сервиса (официальный RSS)
- Трей, глобальная горячая клавиша `Ctrl+Alt+D`, светлая/тёмная тема, запоминание позиции и привязка к краям

## Требования

- Windows 10/11 со встроенным .NET Framework 4.8
- PowerShell 5.1 (сборка использует встроенный `csc.exe`, SDK не нужен)

## Сборка

```powershell
powershell -ExecutionPolicy Bypass -File src\build-widget.ps1
```

Создаёт `DeepSeekPriceWidget.exe` в корне проекта. `assets\app.ico` генерируется скриптом.

## Использование

Запустите `DeepSeekPriceWidget.exe` или `Start-DeepSeek-Price-Widget.bat`.

Правый клик: угол экрана, оформление, язык, валюта, калькулятор, история цен, горячие клавиши.
Клавиши: `Ctrl+Alt+D` (показать/скрыть), `Ctrl+E` (калькулятор), `Ctrl+R` (обновить цены), `Ctrl+H` (в трей), `Ctrl+Shift+Q` (выход).

## Структура

```
assets/   XAML-интерфейс и иконка (app.ico создаётся при сборке)
data/     настройки, праздники, валюты, источники курсов, история цен
lang/     языковые пакеты (en, zh-CN, zh-TW, ja, ko, ru, fr, de, es, it, pt-BR)
logs/     журналы и результаты тестов (создаются при работе; не в git)
src/      исходники C#, manifest, скрипт сборки
```

## Обслуживание (обычно без пересборки)

| Что | Файл | Как |
|---|---|---|
| Праздники | `data/holidays.json` | Изменить и выбрать «Перезагрузить праздники» |
| Валюты | `data/currencies.json` | Добавить `code` / `symbol` / `name`; `languageDefaults` задаёт язык → валюту |
| Источники курсов | `data/fx-providers.json` | Порядок массива = приоритет; `url` / `container` / `lowerKeys` |
| Языки | `lang/*.json` | Скопировать `en.json`, перевести и изменить `_name`; перезапуск |
| Интерфейс | `assets/*.xaml` | Изменить и перезапустить |
| Логика | `src/*.cs` | Закрыть виджет и выполнить скрипт сборки |

Отсутствующие `currencies.json` / `fx-providers.json` создаются с настройками по умолчанию при первом запуске.

## Тесты

```powershell
DeepSeekPriceWidget.exe --selftest   # периоды, праздники, отсчёт, конвертация, i18n, разбор курсов
DeepSeekPriceWidget.exe --smoke      # построение окна, трей, горячая клавиша, замеры макета
DeepSeekPriceWidget.exe --fetchtest  # загрузка и разбор официальных цен
DeepSeekPriceWidget.exe --statustest # официальный статус RSS
DeepSeekPriceWidget.exe --fxtest     # эндпоинт курсов
```

Результаты сохраняются в `logs/`.
