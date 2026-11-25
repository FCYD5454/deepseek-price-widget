# DeepSeek-Preis-Widget

[繁體中文](README.zh-Hant.md) · [简体中文](README.zh-CN.md) · [English](../README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Português (Brasil)](README.pt-BR.md)

Schwebendes Windows-Desktop-Widget, das Spitzen-/Nebenzeiten der DeepSeek-API und aktuelle Preise anzeigt – mit mehreren Währungen und Sprachen.

> Inoffizielles Tool. Maßgeblich sind die Preise auf der offiziellen DeepSeek-Seite.

## Funktionen

- Spitzen-/Nebenzeiten nach Pekinger Zeit und chinesischen Feiertagen inkl. Countdown
- Automatischer Preisabruf aus der offiziellen `pricing.md`, mit Fehler-/Veraltet-Hinweisen und Preisverlauf
- **16 Währungen**: CNY, HKD, USD, EUR, JPY, GBP, AUD, CAD, CHF, SGD, KRW, THB, BRL, TWD, VND, RUB
  - Kursquellen in Reihenfolge: open.er-api.com → fawazahmed0 currency-api → frankfurter.app
- **11 Sprachen** (oder System folgen), optionale Währung je Sprache
- Kostenschätzung (Cache-Trefferquote, Ein-/Ausgabe-Tokens), Dienststatus (offizieller RSS)
- Infobereich, globaler Hotkey `Ctrl+Alt+D`, helles/dunkles Design, Positionsspeicher und Kanten-Andocken

## Voraussetzungen

- Windows 10/11 mit integriertem .NET Framework 4.8
- PowerShell 5.1 (Build nutzt das integrierte `csc.exe`, kein SDK nötig)

## Build

```powershell
powershell -ExecutionPolicy Bypass -File src\build-widget.ps1
```

Erzeugt `DeepSeekPriceWidget.exe` im Projektstamm. `assets\app.ico` wird vom Skript generiert.

## Nutzung

Doppelklick auf `DeepSeekPriceWidget.exe` oder `Start-DeepSeek-Price-Widget.bat`.

Rechtsklick: Ecke, Darstellung, Sprache, Währung, Kostenschätzung, Preisverlauf, Tastenkürzel.
Kürzel: `Ctrl+Alt+D` (anzeigen/ausblenden), `Ctrl+E` (Schätzung), `Ctrl+R` (Preise aktualisieren), `Ctrl+H` (Infobereich), `Ctrl+Shift+Q` (beenden).

## Struktur

```
assets/   XAML-UI und Icon (app.ico beim Build erzeugt)
data/     Einstellungen, Feiertage, Währungen, Kursquellen, Preisverlauf
lang/     Sprachpakete (en, zh-CN, zh-TW, ja, ko, ru, fr, de, es, it, pt-BR)
logs/     Protokolle und Testergebnisse (zur Laufzeit erzeugt, nicht versioniert)
src/      C#-Quellcode, Manifest, Build-Skript
```

## Wartung (meist ohne Neubau)

| Element | Datei | Vorgehen |
|---|---|---|
| Feiertage | `data/holidays.json` | Bearbeiten, dann Rechtsklick „Feiertage neu laden“ |
| Währungen | `data/currencies.json` | `code` / `symbol` / `name` hinzufügen; `languageDefaults` bildet Sprache → Währung ab |
| Kursquellen | `data/fx-providers.json` | Array-Reihenfolge = Priorität; `url` / `container` / `lowerKeys` |
| Sprachen | `lang/*.json` | `en.json` kopieren, übersetzen, `_name` ändern; Neustart |
| UI | `assets/*.xaml` | Bearbeiten und neu starten |
| Logik | `src/*.cs` | Widget schließen, dann Build-Skript ausführen |

Fehlende `currencies.json` / `fx-providers.json` werden beim ersten Start mit Standardwerten erzeugt.

## Tests

```powershell
DeepSeekPriceWidget.exe --selftest   # Zeiten, Feiertage, Countdown, Umrechnung, i18n, Kurs-Parsing
DeepSeekPriceWidget.exe --smoke      # Fenster, Infobereich, Hotkey, Layoutmessung
DeepSeekPriceWidget.exe --fetchtest  # offizieller Preisabruf und Parsing
DeepSeekPriceWidget.exe --statustest # offizieller Status-RSS
DeepSeekPriceWidget.exe --fxtest     # Kurs-Endpunkt
```

Ergebnisse in `logs/`.
