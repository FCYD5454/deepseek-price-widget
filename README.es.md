# Widget de precios de DeepSeek

[繁體中文（香港）](README.zh-HK.md) · [繁體中文（台灣）](README.zh-TW.md) · [简体中文](README.zh-CN.md) · [English](README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Português (Brasil)](README.pt-BR.md)

Widget flotante para Windows que muestra los periodos de punta/valle de la API de DeepSeek y los precios en vivo, con varias divisas e idiomas.

> Herramienta no oficial. Los precios definitivos son los de la página oficial de DeepSeek.

## Funciones

- Detección de punta/valle según la hora de Pekín y los festivos chinos, con cuenta atrás
- Obtención automática de precios desde el `pricing.md` oficial, con avisos de error/obsolescencia e historial de cambios
- **16 divisas**: CNY, HKD, USD, EUR, JPY, GBP, AUD, CAD, CHF, SGD, KRW, THB, BRL, TWD, VND, RUB
  - Fuentes de tipo de cambio en orden: open.er-api.com → fawazahmed0 currency-api → frankfurter.app
- **11 idiomas** (o seguir el sistema), moneda automática por idioma opcional
- Estimador de coste (tasa de caché, tokens de entrada/salida), estado del servicio (RSS oficial)
- Bandeja del sistema, atajo global `Ctrl+Alt+D`, temas claro/oscuro, memoria de posición y ajuste a bordes

## Requisitos

- Windows 10/11 con .NET Framework 4.8 integrado
- PowerShell 5.1 (la compilación usa el `csc.exe` integrado, sin SDK)

## Compilación

```powershell
powershell -ExecutionPolicy Bypass -File src\build-widget.ps1
```

Genera `DeepSeekPriceWidget.exe` en la raíz del proyecto. `assets\app.ico` lo crea el script.

## Uso

Haz doble clic en `DeepSeekPriceWidget.exe` o `啟動DeepSeek價格小工具.bat`.

Clic derecho: esquina, apariencia, idioma, moneda, estimador, historial de precios, atajos.
Atajos: `Ctrl+Alt+D` (mostrar/ocultar), `Ctrl+E` (estimador), `Ctrl+R` (actualizar precios), `Ctrl+H` (bandeja), `Ctrl+Shift+Q` (salir).

## Estructura

```
assets/   interfaz XAML e icono (app.ico generado al compilar)
data/     ajustes, festivos, divisas, fuentes de cambio, historial de precios
lang/     paquetes de idioma (en, zh-CN, zh-TW, ja, ko, ru, fr, de, es, it, pt-BR)
logs/     registros y resultados de pruebas (creados al ejecutar, no versionados)
src/      código C#, manifest, script de compilación
```

## Mantenimiento (normalmente sin recompilar)

| Elemento | Archivo | Cómo |
|---|---|---|
| Festivos | `data/holidays.json` | Editar y clic derecho «Recargar festivos» |
| Divisas | `data/currencies.json` | Añadir `code` / `symbol` / `name`; `languageDefaults` asigna idioma → divisa |
| Fuentes de cambio | `data/fx-providers.json` | El orden del array es prioridad; `url` / `container` / `lowerKeys` |
| Idiomas | `lang/*.json` | Copiar `en.json`, traducir y cambiar `_name`; reiniciar |
| Interfaz | `assets/*.xaml` | Editar y reiniciar |
| Lógica | `src/*.cs` | Cerrar el widget y ejecutar el script de compilación |

Si faltan `currencies.json` / `fx-providers.json`, se generan con valores por defecto al primer inicio.

## Pruebas

```powershell
DeepSeekPriceWidget.exe --selftest   # periodos, festivos, cuenta atrás, conversión, i18n, análisis de tipos
DeepSeekPriceWidget.exe --smoke      # ventana, bandeja, atajo, medición de diseño
DeepSeekPriceWidget.exe --fetchtest  # obtención y análisis de precios oficiales
DeepSeekPriceWidget.exe --statustest # RSS de estado oficial
DeepSeekPriceWidget.exe --fxtest     # punto de acceso de tipos de cambio
```

Resultados en `logs/`.
