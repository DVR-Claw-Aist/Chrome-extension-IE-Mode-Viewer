<div align="center">

[English](README.md) · [Русский](README_RU.md)

# IE Mode Viewer

**Open legacy pages in a real Internet Explorer (Trident) window right from Chrome/Edge — ActiveX, DVR and corporate controls just work.**

[Screenshots](#screenshots) · [Features](#features) · [Architecture](#architecture) · [Installation](#installation) · [Roadmap](#roadmap)

[![License](https://img.shields.io/badge/license-MIT-yellow.svg)](LICENSE)
[![Chrome](https://img.shields.io/badge/Chrome-Extension%20MV3-4285F4.svg?logo=googlechrome&logoColor=white)]()
[![Edge](https://img.shields.io/badge/Edge-Extension%20MV3-0078D7.svg?logo=microsoftedge&logoColor=white)]()
[![.NET](https://img.shields.io/badge/.NET-11-512BD4.svg?logo=dotnet&logoColor=white)]()

</div>

![IE Mode Viewer](Screenshots/1.png)

## Why This Project?

> IE Mode no longer supports our environment, so I built my own replacement.

Some legacy web apps — DVR systems, corporate portals, industrial software — still require ActiveX controls that only run in Internet Explorer. This extension opens such pages in a dedicated IE (Trident) window from Chrome/Edge with a single click.

## Screenshots

| | |
|---|---|
| ![Extension popup](Screenshots/1.png) | ![Options — whitelist](Screenshots/2.png) |
| ![IE viewer window](Screenshots/3.png) | ![IE viewer — rendered page](Screenshots/4.png) |

## Features

- **One-click open** — send the current tab to the IE viewer from the toolbar popup.
- **Auto-open whitelist** — glob patterns in the options page; matching URLs open in IE automatically.
- **Native Messaging** — Chrome talks to a .NET host over a JSON stdin/stdout protocol.
- **IE7–IE11 emulation switch** — toolbar button cycles the document mode, no registry fiddling.
- **ActiveX enabled** — installs allowed, filtering off, DEP workaround, safe scripting for controls.
- **Trusted Sites auto-add** — the page's host lands in the IE Trusted Sites zone.
- **SSL error pages bypassed** — legacy certificates no longer block the page.
- **Per-user install** — HKCU registry only, no admin rights required, registered for Chrome and Edge.

## Architecture

```
┌──────────────┐   ┌─────────────────────┐   ┌────────────────────────────────────┐
│  Popup /     │   │  Service worker     │   │  IEHost.exe  (.NET 11, win-x86)    │
│  options /   │──▶│  background.js      │──▶│  NativeMessaging loop (stdio JSON) │
│  content     │   │  sendNativeMessage  │   │    └─ spawn --viewer <url>         │
│  script      │   └─────────────────────┘   │  ViewerForm (WinForms)             │
└──────────────┘                              │    └─ WebBrowser (Trident)         │
                                              │  HKCU reg tweaks (IE / ActiveX)    │
                                              └────────────────────────────────────┘
```

- Chrome ↔ host IPC uses the **Native Messaging** protocol: 4-byte little-endian length + UTF-8 JSON on stdin/stdout.
- The viewer runs as a **separate 32-bit (`win-x86`) process** — legacy DVR ActiveX controls are 32-bit COM components and won't load in a 64-bit process.
- IE and ActiveX settings are applied **per-user (HKCU)** at viewer startup — no admin rights needed.
- The extension ID is **dynamic during development**; `install.ps1` rewrites the host manifest and registry keys every time it runs.

## Tech Stack

| Layer | Technology |
|---|---|
| Extension | Chrome/Edge Manifest V3, vanilla JS (no build tooling) |
| Native host | C# / .NET 11 (`net11.0-windows`) |
| UI | Windows Forms + `WebBrowser` control (Trident engine) |
| IPC | Native Messaging (JSON over stdio) |

## How It Works

1. Click the extension icon — the popup checks whether the native host is installed and enabled.
2. Click **Open in IE** — the popup sends the active tab's URL to the service worker.
3. `background.js` calls `chrome.runtime.sendNativeMessage` → `IEHost.exe`.
4. `IEHost.exe` reads the JSON request, then spawns itself with `--viewer <url>`.
5. The viewer process applies HKCU registry tweaks: IE11 emulation, ActiveX support, Trusted Sites, SSL bypass.
6. A WinForms window opens with the `WebBrowser` (Trident) control and navigates to the URL.
7. For whitelisted URLs the content script triggers the same flow automatically.

## Installation

Requires **Windows 10/11** and a **.NET 11 SDK** (see [prerequisites](#prerequisites)).

```bash
git clone https://github.com/DVR-Claw-Aist/Chrome-extension-IE-Mode-Viewer.git
cd Chrome-extension-IE-Mode-Viewer
dotnet publish -c Release -r win-x86 --self-contained native\IEHost\IEHost.csproj
.\install.ps1 -ExtensionId <ID>
```

Load the extension at `chrome://extensions` → enable **Developer mode** → drag `extension.crx` onto the page, or click **Load unpacked** and pick `extension/`. The extension ID is shown in the popup or at `chrome://extensions`.

> **The extension ID is dynamic during development.** Native messaging must be re-registered whenever it changes: `.\install.ps1 -ExtensionId <ID>`. To fix the ID permanently, pack the extension with a private key (Chrome → **Pack extension**) and keep `extension.pem` **local and private** — it is excluded from this repository.

### Prerequisites

- Windows 10 or 11
- .NET 11 SDK — a system install in `PATH`, or pass `.\install.ps1 -DotNetPath <path\to\dotnet.exe>`
- Chrome or Edge 88+

The .NET SDK is **not bundled** in this repository. Install it from [dotnet.microsoft.com](https://dotnet.microsoft.com/download).

### Commands

| Command | Description |
|---|---|
| `dotnet publish -c Release -r win-x86 --self-contained native\IEHost\IEHost.csproj` | Build the self-contained 32-bit native host |
| `.\install.ps1 -ExtensionId <ID>` | Install host + register for Chrome and Edge (HKCU, no admin) |
| `.\install.ps1 -DotNetPath <path>` | Install with a specific .NET SDK path |

## Roadmap

- [ ] Automated tests (JS + native host)
- [ ] Installer (MSI) with SDK path detection
- [ ] Right-click context menu item "Open in IE"
- [ ] Remember emulation mode per site
- [ ] 64-bit host fallback for pages that don't need ActiveX

## License

Released under the [MIT](LICENSE) license.
