# Widget de prix DeepSeek

[繁體中文](README.zh-Hant.md) · [简体中文](README.zh-CN.md) · [English](../README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Português (Brasil)](README.pt-BR.md)

Widget flottant pour Windows affichant les périodes de pointe/hors pointe de l’API DeepSeek et les prix en direct, avec prise en charge de plusieurs devises et langues.

> Outil non officiel. Les prix définitifs sont ceux de la page tarifaire officielle de DeepSeek.

## Fonctionnalités

- Détection pointe/hors pointe selon l’heure de Pékin et les jours fériés chinois, avec compte à rebours
- Récupération automatique des prix depuis le `pricing.md` officiel, avec indicateurs d’échec/d’obsolescence et historique des changements
- **16 devises** : CNY, HKD, USD, EUR, JPY, GBP, AUD, CAD, CHF, SGD, KRW, THB, BRL, TWD, VND, RUB
  - Sources de taux dans l’ordre : open.er-api.com → fawazahmed0 currency-api → frankfurter.app
- **11 langues** (ou suivre le système), devise automatique par langue en option
- Estimateur de coût (taux de cache, tokens d’entrée/sortie), état du service (RSS officiel)
- Zone de notification, raccourci global `Ctrl+Alt+D`, thèmes clair/sombre, mémorisation de position et accrochage aux bords

## Prérequis

- Windows 10/11 avec .NET Framework 4.8 intégré
- PowerShell 5.1 (la compilation utilise le `csc.exe` intégré, sans SDK)

## Compilation

```powershell
powershell -ExecutionPolicy Bypass -File src\build-widget.ps1
```

Génère `DeepSeekPriceWidget.exe` à la racine du projet. `assets\app.ico` est créé par le script.

## Utilisation

Double-cliquez sur `DeepSeekPriceWidget.exe` ou `Start-DeepSeek-Price-Widget.bat`.

Clic droit : coin, apparence, langue, devise, estimateur, historique des prix, raccourcis.
Raccourcis : `Ctrl+Alt+D` (afficher/masquer), `Ctrl+E` (estimateur), `Ctrl+R` (actualiser), `Ctrl+H` (zone de notification), `Ctrl+Shift+Q` (quitter).

## Structure

```
assets/   interface XAML et icône (app.ico générée à la compilation)
data/     réglages, jours fériés, devises, sources de taux, historique des prix
lang/     packs de langue (en, zh-CN, zh-TW, ja, ko, ru, fr, de, es, it, pt-BR)
logs/     journaux et sorties de test (créés à l’exécution, non versionnés)
src/      source C#, manifest, script de compilation
```

## Maintenance (généralement sans recompilation)

| Élément | Fichier | Procédure |
|---|---|---|
| Jours fériés | `data/holidays.json` | Modifier puis clic droit « Recharger les jours fériés » |
| Devises | `data/currencies.json` | Ajouter `code` / `symbol` / `name` ; `languageDefaults` mappe langue → devise |
| Sources de taux | `data/fx-providers.json` | L’ordre du tableau est la priorité ; `url` / `container` / `lowerKeys` |
| Langues | `lang/*.json` | Copier `en.json`, traduire et changer `_name` ; redémarrer |
| Interface | `assets/*.xaml` | Modifier puis redémarrer |
| Logique | `src/*.cs` | Fermer le widget puis lancer le script de compilation |

Les fichiers `currencies.json` / `fx-providers.json` manquants sont générés par défaut au premier lancement.

## Tests

```powershell
DeepSeekPriceWidget.exe --selftest   # périodes, fériés, compte à rebours, conversion, i18n, taux
DeepSeekPriceWidget.exe --smoke      # fenêtre, zone de notification, raccourci, mesure de mise en page
DeepSeekPriceWidget.exe --fetchtest  # récupération et analyse des prix officiels
DeepSeekPriceWidget.exe --statustest # RSS de statut officiel
DeepSeekPriceWidget.exe --fxtest     # point d’accès des taux
```

Résultats dans `logs/`.
