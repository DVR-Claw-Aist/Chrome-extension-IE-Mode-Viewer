using System.Diagnostics;

namespace IEHost;

public class TrayApp : ApplicationContext
{
    readonly AppSettings _settings;
    readonly NotifyIcon _icon;
    readonly HotkeyWindow _hotkeyWindow = new();
    const int HOTKEY_ID = 1;
    bool _busy;

    public TrayApp(AppSettings settings)
    {
        _settings = settings;

        _icon = new NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)!,
            Text = "IE Mode Viewer",
            Visible = true,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open current tab in IE", null, async (_, _) => await OpenActiveTabAsync());
        menu.Items.Add("Open viewer...", null, (_, _) => OpenViewerAtLast());
        menu.Items.Add("Settings", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Uninstall", null, (_, _) => Program.RunUninstall());
        menu.Items.Add("Exit", null, (_, _) => Exit());
        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += async (_, _) => await OpenActiveTabAsync();

        _hotkeyWindow.HotkeyPressed += async () => await OpenActiveTabAsync();
        _hotkeyWindow.CreateHandle(new CreateParams());
        TryRegisterHotkey();

        Application.ApplicationExit += (_, _) => Cleanup();
    }

    void TryRegisterHotkey()
    {
        if (!HotkeyParser.TryParse(_settings.Hotkey, out uint mods, out uint vk))
            return;
        if (!NativeMethods.RegisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID, mods, vk))
        {
            _icon.ShowBalloonTip(3000, "IE Mode Viewer",
                $"Hotkey {_settings.Hotkey} is already in use", ToolTipIcon.Warning);
        }
    }

    async Task OpenActiveTabAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            if (string.IsNullOrEmpty(_settings.ChromePath))
            {
                _icon.ShowBalloonTip(3000, "IE Mode Viewer",
                    "No browser configured. Open viewer once to detect Chrome.", ToolTipIcon.Warning);
                return;
            }

            Process? launched = null;
            if (!ChromeManager.IsBrowserRunning(_settings.DebugPort))
            {
                launched = ChromeManager.LaunchBrowser(_settings.ChromePath, _settings.DebugPort);
                if (launched != null)
                    await ChromeManager.WaitForCdpAsync(_settings.DebugPort, 8000);
            }

            var url = await ChromeManager.GetActiveTabUrl(_settings.DebugPort);
            if (string.IsNullOrEmpty(url))
            {
                _icon.ShowBalloonTip(3000, "IE Mode Viewer",
                    launched != null
                        ? "Managed Chrome was started. Open a page there, then press the hotkey again."
                        : "No page found in managed Chrome. Open a page there first.", ToolTipIcon.Warning);
                return;
            }

            Program.SpawnViewer(url);
            await ChromeManager.CloseBrowserAsync(_settings.DebugPort, launched);
        }
        finally
        {
            _busy = false;
        }
    }

    void OpenViewerAtLast()
    {
        var url = string.IsNullOrWhiteSpace(_settings.LastUrl) ? _settings.DefaultUrl : _settings.LastUrl;
        Program.SpawnViewer(url);
    }

    void ShowSettings()
    {
        using var form = new SettingsForm(_settings);
        form.ShowDialog();
    }

    void Exit()
    {
        _icon.Visible = false;
        if (_hotkeyWindow.Handle != IntPtr.Zero)
            NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID);
        Application.Exit();
    }

    void Cleanup()
    {
        try { _icon?.Dispose(); } catch { }
        try { _hotkeyWindow?.DestroyHandle(); } catch { }
    }
}
