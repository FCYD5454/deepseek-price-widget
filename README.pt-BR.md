# Widget de preços DeepSeek

[繁體中文（香港）](README.zh-HK.md) · [繁體中文（台灣）](README.zh-TW.md) · [简体中文](README.zh-CN.md) · [English](README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Português (Brasil)](README.pt-BR.md)

Widget flutuante para Windows que mostra os períodos de pico/fora de pico da API DeepSeek e preços ao vivo, com suporte a várias moedas e idiomas.

> Ferramenta não oficial. Os preços finais são os da página oficial da DeepSeek.

## Recursos

- Detecção de pico/fora de pico com base no horário de Pequim e feriados chineses, com contagem regressiva
- Busca automática de preços do `pricing.md` oficial, com avisos de falha/desatualização e histórico de mudanças
- **16 moedas**: CNY, HKD, USD, EUR, JPY, GBP, AUD, CAD, CHF, SGD, KRW, THB, BRL, TWD, VND, RUB
  - Fontes de câmbio em ordem: open.er-api.com → fawazahmed0 currency-api → frankfurter.app
- **11 idiomas** (ou seguir o sistema), moeda automática por idioma opcional
- Estimador de custo (taxa de cache, tokens de entrada/saída), status do serviço (RSS oficial)
- Bandeja do sistema, atalho global `Ctrl+Alt+D`, temas claro/escuro, memória de posição e encaixe nas bordas

## Requisitos

- Windows 10/11 com .NET Framework 4.8 integrado
- PowerShell 5.1 (a compilação usa o `csc.exe` integrado, sem SDK)

## Compilação

```powershell
powershell -ExecutionPolicy Bypass -File src\build-widget.ps1
```

Gera `DeepSeekPriceWidget.exe` na raiz do projeto. `assets\app.ico` é criado pelo script.

## Uso

Dê duplo clique em `DeepSeekPriceWidget.exe` ou `啟動DeepSeek價格小工具.bat`.

Clique direito: canto, aparência, idioma, moeda, estimador, histórico de preços, atalhos.
Atalhos: `Ctrl+Alt+D` (mostrar/ocultar), `Ctrl+E` (estimador), `Ctrl+R` (atualizar preços), `Ctrl+H` (bandeja), `Ctrl+Shift+Q` (sair).

## Estrutura

```
assets/   interface XAML e ícone (app.ico gerado na compilação)
data/     configurações, feriados, moedas, fontes de câmbio, histórico de preços
lang/     pacotes de idioma (en, zh-CN, zh-TW, ja, ko, ru, fr, de, es, it, pt-BR)
logs/     logs e saídas de teste (criados em tempo de execução, fora do controle de versão)
src/      código C#, manifest, script de compilação
```

## Manutenção (geralmente sem recompilar)

| Item | Arquivo | Como |
|---|---|---|
| Feriados | `data/holidays.json` | Editar e clicar com o direito em «Recarregar feriados» |
| Moedas | `data/currencies.json` | Adicionar `code` / `symbol` / `name`; `languageDefaults` mapeia idioma → moeda |
| Fontes de câmbio | `data/fx-providers.json` | A ordem do array é a prioridade; `url` / `container` / `lowerKeys` |
| Idiomas | `lang/*.json` | Copiar `en.json`, traduzir e alterar `_name`; reiniciar |
| Interface | `assets/*.xaml` | Editar e reiniciar |
| Lógica | `src/*.cs` | Fechar o widget e executar o script de compilação |

`currencies.json` / `fx-providers.json` ausentes são gerados com padrões na primeira execução.

## Testes

```powershell
DeepSeekPriceWidget.exe --selftest   # períodos, feriados, contagem, conversão, i18n, parsing de câmbio
DeepSeekPriceWidget.exe --smoke      # janela, bandeja, atalho, medição de layout
DeepSeekPriceWidget.exe --fetchtest  # busca e parsing de preços oficiais
DeepSeekPriceWidget.exe --statustest # RSS de status oficial
DeepSeekPriceWidget.exe --fxtest     # endpoint de câmbio
```

Resultados em `logs/`.
