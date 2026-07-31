<div align="center">

[English](README.md) · [Русский](README_RU.md)

# IE Mode Viewer

**Открывайте старые страницы в настоящем окне Internet Explorer (Trident) прямо из Chrome/Edge — ActiveX, DVR и корпоративные контролы просто работают.**

[Скриншоты](#скриншоты) · [Возможности](#возможности) · [Архитектура](#архитектура) · [Установка](#установка) · [План развития](#план-развития)

[![Лицензия](https://img.shields.io/badge/license-MIT-yellow.svg)](LICENSE)
[![Chrome](https://img.shields.io/badge/Chrome-Extension%20MV3-4285F4.svg?logo=googlechrome&logoColor=white)]()
[![Edge](https://img.shields.io/badge/Edge-Extension%20MV3-0078D7.svg?logo=microsoftedge&logoColor=white)]()
[![.NET](https://img.shields.io/badge/.NET-11-512BD4.svg?logo=dotnet&logoColor=white)]()

</div>

![IE Mode Viewer](Screenshots/1.png)

## Почему этот проект?

> Режим IE больше не поддерживает наше окружение, поэтому я написал собственную замену.

Некоторые старые веб-приложения — системы видеонаблюдения (DVR), корпоративные порталы, промышленное ПО — всё ещё требуют контролов ActiveX, которые работают только в Internet Explorer. Это расширение открывает такие страницы в отдельном окне IE (Trident) из Chrome/Edge в один клик.

## Скриншоты

| | |
|---|---|
| ![Попап расширения](Screenshots/1.png) | ![Настройки — белый список](Screenshots/2.png) |
| ![Окно IE-просмотрщика](Screenshots/3.png) | ![IE-просмотрщик — страница](Screenshots/4.png) |

## Возможности

- **Открытие в один клик** — текущая вкладка отправляется в IE-просмотрщик из попапа на панели инструментов.
- **Автооткрытие по белому списку** — glob-шаблоны на странице настроек; подходящие URL открываются в IE автоматически.
- **Native Messaging** — Chrome общается с .NET-хостом по JSON-протоколу stdin/stdout.
- **Переключение эмуляции IE7–IE11** — кнопка на панели циклически меняет режим документа, без правки реестра.
- **ActiveX включён** — установка разрешена, фильтрация выключена, обход DEP, безопасный скриптинг контролов.
- **Автодобавление в надежные сайты** — хост страницы попадает в зону «Надежные сайты» IE.
- **Обход страниц ошибок SSL** — устаревшие сертификаты больше не блокируют страницу.
- **Установка для пользователя** — только HKCU, права администратора не нужны, регистрация для Chrome и Edge.

## Архитектура

```
┌──────────────┐   ┌─────────────────────┐   ┌────────────────────────────────────┐
│  Попап /     │   │  Service worker     │   │  IEHost.exe  (.NET 11, win-x86)    │
│  настройки / │──▶│  background.js      │──▶│  Native Messaging loop (stdio JSON) │
│  content     │   │  sendNativeMessage  │   │    └─ spawn --viewer <url>         │
│  script      │   └─────────────────────┘   │  ViewerForm (WinForms)             │
└──────────────┘                              │    └─ WebBrowser (Trident)         │
                                              │  HKCU reg tweaks (IE / ActiveX)    │
                                              └────────────────────────────────────┘
```

- Обмен Chrome ↔ хост идёт по протоколу **Native Messaging**: 4-байтная длина (little-endian) + UTF-8 JSON в stdin/stdout.
- Просмотрщик запускается как **отдельный 32-битный процесс (`win-x86`)** — старые DVR-контролы ActiveX это 32-битные COM-компоненты и не загружаются в 64-битный процесс.
- Настройки IE и ActiveX применяются **для пользователя (HKCU)** при старте просмотрщика — права администратора не нужны.
- ID расширения **динамический в процессе разработки**; `install.ps1` перезаписывает манифест хоста и ключи реестра при каждом запуске.

## Технологии

| Слой | Технология |
|---|---|
| Расширение | Chrome/Edge Manifest V3, чистый JS (без сборки) |
| Нативный хост | C# / .NET 11 (`net11.0-windows`) |
| Интерфейс | Windows Forms + контрол `WebBrowser` (движок Trident) |
| IPC | Native Messaging (JSON через stdio) |

## Как это работает

1. Клик по иконке расширения — попап проверяет, установлен ли нативный хост.
2. Клик по кнопке **Открыть в IE** — попап отправляет URL активной вкладки в service worker.
3. `background.js` вызывает `chrome.runtime.sendNativeMessage` → `IEHost.exe`.
4. `IEHost.exe` читает JSON-запрос и запускает себя с `--viewer <url>`.
5. Процесс просмотрщика применяет правки HKCU: эмуляция IE11, поддержка ActiveX, надежные сайты, обход SSL.
6. Открывается окно WinForms с контролом `WebBrowser` (Trident), которое переходит на URL.
7. Для URL из белого списка тот же сценарий запускается автоматически через content script.

## Установка

Требуются **Windows 10/11** и **.NET 11 SDK** (см. [требования](#требования)).

```bash
git clone https://github.com/DVR-Claw-Aist/Chrome-extension-IE-Mode-Viewer.git
cd Chrome-extension-IE-Mode-Viewer
dotnet publish -c Release -r win-x86 --self-contained native\IEHost\IEHost.csproj
.\install.ps1 -ExtensionId <ID>
```

Загрузите расширение на `chrome://extensions` → включите **Режим разработчика** → перетащите `extension.crx` на страницу или нажмите **Загрузить распакованное** и выберите папку `extension/`. ID расширения показан в попапе или на `chrome://extensions`.

> **ID расширения динамический в процессе разработки.** Native messaging нужно перерегистрировать при каждом изменении ID: `.\install.ps1 -ExtensionId <ID>`. Чтобы закрепить ID навсегда, упакуйте расширение с приватным ключом (Chrome → **Pack extension**) и храните `extension.pem` **только локально** — он исключён из этого репозитория.

### Требования

- Windows 10 или 11
- .NET 11 SDK — системная установка в `PATH` или `.\install.ps1 -DotNetPath <путь\к\dotnet.exe>`
- Chrome или Edge 88+

.NET SDK **не включён** в репозиторий. Установите его с [dotnet.microsoft.com](https://dotnet.microsoft.com/download).

### Команды

| Команда | Описание |
|---|---|
| `dotnet publish -c Release -r win-x86 --self-contained native\IEHost\IEHost.csproj` | Сборка автономного 32-битного нативного хоста |
| `.\install.ps1 -ExtensionId <ID>` | Установка хоста + регистрация для Chrome и Edge (HKCU, без админа) |
| `.\install.ps1 -DotNetPath <путь>` | Установка с указанием пути к .NET SDK |

## План развития

- [ ] Автоматические тесты (JS + нативный хост)
- [ ] Установщик (MSI) с определением пути SDK
- [ ] Пункт контекстного меню «Открыть в IE»
- [ ] Запоминание режима эмуляции для каждого сайта
- [ ] 64-битный запасной хост для страниц без ActiveX

## Лицензия

Распространяется под лицензией [MIT](LICENSE).
