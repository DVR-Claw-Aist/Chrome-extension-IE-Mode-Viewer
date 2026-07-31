# IE Mode Viewer

Chrome/Edge extension that opens pages in Internet Explorer (Trident) engine via Native Messaging for ActiveX/DVR compatibility.

## Why

Some legacy web apps (DVR systems, corporate tools) require ActiveX controls that only work in Internet Explorer. This extension lets you open such pages in a dedicated IE window from Chrome/Edge with a single click.

## How it works

Popup → Native Messaging → IEHost.exe → WinForms + WebBrowser (Trident)

The extension sends the page URL to a native host (`IEHost.exe`) which spawns a Windows Forms window with the IE WebBrowser control pre-configured for ActiveX support.

## Requirements

- Windows 10 or 11
- Chrome or Edge 88+
- .NET 11 SDK (`dotnet` available in `PATH`) — for building the native host

> The .NET SDK is **not bundled** in this repository. Install it from
> [dotnet.microsoft.com](https://dotnet.microsoft.com/download) (or keep a local copy
> and adjust the path in `install.ps1`).

## Quick start

1. **Publish native host**
   ```
   dotnet publish -c Release -r win-x86 --self-contained native\IEHost\IEHost.csproj
   ```

2. **Install**
   ```
   .\install.ps1 -ExtensionId <ID>
   ```
   (Extension ID is shown in the popup or at `chrome://extensions`)

3. **Load extension**
   - Go to `chrome://extensions`
   - Enable **Developer mode**
   - Drag `extension.crx` onto the page (recommended) or click **Load unpacked** → `extension/`

4. **Use**
   - Click the extension icon in the toolbar
   - Click **Open in IE**

> The extension ID is **dynamic** during development. Native messaging must be
> re-registered whenever the ID changes: `.\install.ps1 -ExtensionId <ID>`.
> To fix the ID permanently, pack the extension with a private key (see below).

## Important: 32-bit only

Must build as `win-x86`. Legacy DVR ActiveX controls are 32-bit COM components and won't load in a 64-bit process.

## ActiveX features

The viewer enables at startup (per-user, HKCU):
- IE11 emulation (switchable IE7–IE11 via toolbar)
- ActiveX installs allowed
- ActiveX filtering disabled
- Safe ActiveX scripting enabled
- DEP disabled for ATL controls
- SSL error pages bypassed
- Domain added to Trusted Sites

## Project structure

```
extension/           Chrome Extension (Manifest V3)
  manifest.json
  background.js      Native Messaging bridge
  content_script.js  Whitelist auto-open
  popup.html/js/css  Manual open + extension ID
  options.html/js    Whitelist management
native/
  host_manifest.json Native Messaging host manifest template
  IEHost/            .NET 11 WinForms app
    Program.cs       Entry point + Native Messaging loop
    ViewerForm.cs    IE WebBrowser control
    NativeMessaging.cs   stdin/stdout JSON protocol
install.ps1          Build + install script
extension.crx        Pre-packed extension (fixed extension ID)
Screenshots/         UI screenshots
```

## Screenshots

See the `Screenshots/` folder in the repository root.

## Dev workflow

- JS/CSS changes → reload at `chrome://extensions`
- C# changes → `dotnet publish` + `install.ps1` + reload extension
- To fix the extension ID (so native messaging survives reloads): pack via `chrome://extensions` → **Pack extension**, then drag the resulting `.crx` onto the page. Keep `extension.pem` **locally and private** — it is the key that fixes your extension ID. It is excluded from this repository (`.gitignore`).

## Use Cases

- DVR systems
- Corporate portals
- ActiveX applications
- Legacy ERP
- Industrial software

## Motivation

IE Mode no longer supports our environment, so I built my own replacement.

