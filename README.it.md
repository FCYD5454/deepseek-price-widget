# Widget prezzi DeepSeek

[繁體中文（香港）](README.zh-HK.md) · [繁體中文（台灣）](README.zh-TW.md) · [简体中文](README.zh-CN.md) · [English](README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Português (Brasil)](README.pt-BR.md)

Widget flottante per Windows che mostra i periodi di punta/non di punta dell’API DeepSeek e i prezzi in tempo reale, con supporto multi-valuta e multi-lingua.

> Strumento non ufficiale. I prezzi definitivi sono quelli della pagina ufficiale DeepSeek.

## Funzioni

- Rilevamento punta/non di punta in base all’ora di Pechino e alle festività cinesi, con conto alla rovescia
- Recupero automatico dei prezzi dal `pricing.md` ufficiale, con avvisi di errore/obsolescenza e cronologia modifiche
- **16 valute**: CNY, HKD, USD, EUR, JPY, GBP, AUD, CAD, CHF, SGD, KRW, THB, BRL, TWD, VND, RUB
  - Fonti dei cambi in ordine: open.er-api.com → fawazahmed0 currency-api → frankfurter.app
- **11 lingue** (o segui il sistema), valuta automatica per lingua opzionale
- Stima costi (tasso di cache, token di input/output), stato del servizio (RSS ufficiale)
- Area di notifica, scorciatoia globale `Ctrl+Alt+D`, temi chiaro/scuro, memoria posizione e aggancio ai bordi

## Requisiti

- Windows 10/11 con .NET Framework 4.8 integrato
- PowerShell 5.1 (la build usa `csc.exe` integrato, nessun SDK)

## Build

```powershell
powershell -ExecutionPolicy Bypass -File src\build-widget.ps1
```

Genera `DeepSeekPriceWidget.exe` nella radice del progetto. `assets\app.ico` è creato dallo script.

## Uso

Fai doppio clic su `DeepSeekPriceWidget.exe` o `啟動DeepSeek價格小工具.bat`.

Clic destro: angolo, aspetto, lingua, valuta, stima costi, cronologia prezzi, scorciatoie.
Scorciatoie: `Ctrl+Alt+D` (mostra/nascondi), `Ctrl+E` (stima), `Ctrl+R` (aggiorna prezzi), `Ctrl+H` (area di notifica), `Ctrl+Shift+Q` (esci).

## Struttura

```
assets/   interfaccia XAML e icona (app.ico generata alla build)
data/     impostazioni, festività, valute, fonti cambi, cronologia prezzi
lang/     pacchetti lingua (en, zh-CN, zh-TW, ja, ko, ru, fr, de, es, it, pt-BR)
logs/     log e risultati test (creati a runtime, non versionati)
src/      sorgente C#, manifest, script di build
```

## Manutenzione (di solito senza ricompilare)

| Voce | File | Come |
|---|---|---|
| Festività | `data/holidays.json` | Modifica, poi clic destro «Ricarica festività» |
| Valute | `data/currencies.json` | Aggiungi `code` / `symbol` / `name`; `languageDefaults` mappa lingua → valuta |
| Fonti cambi | `data/fx-providers.json` | L’ordine dell’array è la priorità; `url` / `container` / `lowerKeys` |
| Lingue | `lang/*.json` | Copia `en.json`, traduci e cambia `_name`; riavvia |
| Interfaccia | `assets/*.xaml` | Modifica e riavvia |
| Logica | `src/*.cs` | Chiudi il widget, poi esegui lo script di build |

Se mancano `currencies.json` / `fx-providers.json`, vengono generati con valori predefiniti al primo avvio.

## Test

```powershell
DeepSeekPriceWidget.exe --selftest   # periodi, festività, conto alla rovescia, conversione, i18n, parsing cambi
DeepSeekPriceWidget.exe --smoke      # finestra, area di notifica, scorciatoia, misura layout
DeepSeekPriceWidget.exe --fetchtest  # recupero e parsing prezzi ufficiali
DeepSeekPriceWidget.exe --statustest # RSS stato ufficiale
DeepSeekPriceWidget.exe --fxtest     # endpoint cambi
```

Risultati in `logs/`.
