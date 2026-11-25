# DeepSeek 가격 위젯

[繁體中文](README.zh-Hant.md) · [简体中文](README.zh-CN.md) · [English](../README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Português (Brasil)](README.pt-BR.md)

DeepSeek API의 혼잡/비혼잡 시간대와 실시간 가격을 보여 주는 Windows 데스크톱 플로팅 위젯입니다. 다중 통화와 다국어를 지원합니다.

> 비공식 도구입니다. 최종 가격은 DeepSeek 공식 가격 페이지를 기준으로 하세요.

## 기능

- 베이징 시간과 중국 공휴일 기준으로 혼잡/비혼잡 판단, 남은 시간 표시
- 공식 `pricing.md`에서 가격 자동 수집(실패·오래된 데이터 표시, 가격 변동 기록 포함)
- **16개 통화**: CNY, HKD, USD, EUR, JPY, GBP, AUD, CAD, CHF, SGD, KRW, THB, BRL, TWD, VND, RUB
  - 환율 소스 순서: open.er-api.com → fawazahmed0 currency-api → frankfurter.app
- **11개 언어**(시스템 따르기 가능), 언어에 따른 통화 자동 전환 옵션
- 비용 계산기(캐시 적중률, 입력/출력 토큰), 서비스 상태(공식 RSS)
- 트레이, 전역 단축키 `Ctrl+Alt+D`, 라이트/다크 테마, 위치 기억과 화면 가장자리 스냅

## 요구 사항

- Windows 10/11(내장 .NET Framework 4.8)
- PowerShell 5.1(빌드는 내장 `csc.exe` 사용, SDK 불필요)

## 빌드

```powershell
powershell -ExecutionPolicy Bypass -File src\build-widget.ps1
```

프로젝트 루트에 `DeepSeekPriceWidget.exe`가 생성됩니다. `assets\app.ico`는 스크립트가 생성합니다.

## 사용법

`DeepSeekPriceWidget.exe` 또는 `Start-DeepSeek-Price-Widget.bat`을 더블 클릭하세요.

오른쪽 클릭 메뉴에서 모서리, 모양, 언어, 통화, 비용 계산, 가격 기록, 단축키를 설정할 수 있습니다.
단축키: `Ctrl+Alt+D`(표시/숨기기), `Ctrl+E`(계산), `Ctrl+R`(가격 새로고침), `Ctrl+H`(트레이로), `Ctrl+Shift+Q`(종료).

## 구조

```
assets/   XAML UI와 아이콘(app.ico는 빌드 시 생성)
data/     설정, 휴일, 통화, 환율 소스, 가격 기록
lang/     언어 팩(en, zh-CN, zh-TW, ja, ko, ru, fr, de, es, it, pt-BR)
logs/     로그와 테스트 출력(실행 시 생성, 버전 관리 제외)
src/      C# 소스, manifest, 빌드 스크립트
```

## 유지보수(대부분 재빌드 불필요)

| 항목 | 파일 | 방법 |
|---|---|---|
| 휴일 | `data/holidays.json` | 수정 후 오른쪽 클릭 “휴일 데이터 다시 불러오기” |
| 통화 | `data/currencies.json` | `code` / `symbol` / `name` 추가; `languageDefaults`가 언어→통화 |
| 환율 소스 | `data/fx-providers.json` | 배열 순서가 우선순위; `url` / `container` / `lowerKeys` |
| 언어 | `lang/*.json` | `en.json` 복사 후 번역 및 `_name` 수정, 재시작 시 메뉴 표시 |
| UI | `assets/*.xaml` | 수정 후 재시작 |
| 로직 | `src/*.cs` | 위젯 종료 후 빌드 스크립트 실행 |

`currencies.json` / `fx-providers.json`이 없으면 첫 실행 시 기본값으로 생성됩니다.

## 테스트

```powershell
DeepSeekPriceWidget.exe --selftest   # 시간대, 휴일, 카운트다운, 환산, i18n, 환율 파싱
DeepSeekPriceWidget.exe --smoke      # 창 구성, 트레이, 단축키, 레이아웃 측정
DeepSeekPriceWidget.exe --fetchtest  # 공식 가격 수집 및 파싱
DeepSeekPriceWidget.exe --statustest # 공식 상태 RSS
DeepSeekPriceWidget.exe --fxtest     # 환율 엔드포인트
```

결과는 `logs/`에 기록됩니다.
