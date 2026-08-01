using Microsoft.Win32;

namespace IEHost;

public partial class ViewerForm : Form
{
    readonly string _initialUrl;
    Panel _toolbar = null!;
    Button _backBtn = null!, _forwardBtn = null!;
    TextBox _urlBar = null!;
    WebBrowser _browser = null!;
    StatusStrip _statusBar = null!;
    ToolStripStatusLabel _statusLabel = null!;

    public ViewerForm(string url)
    {
        _initialUrl = url;
        InitializeComponent();
        _browser.ScriptErrorsSuppressed = true;
        _browser.Navigate(url);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        var current = _browser.Url?.ToString() ?? _initialUrl;
        if (!string.IsNullOrWhiteSpace(current))
        {
            var s = AppSettings.Load();
            s.LastUrl = current;
            s.Save();
        }
        base.OnFormClosing(e);
    }

    void InitializeComponent()
    {
        Text = "IE Mode Viewer";
        Size = new(1200, 800);
        MinimumSize = new(640, 480);
        StartPosition = FormStartPosition.CenterScreen;

        _toolbar = new() { Height = 36, Dock = DockStyle.Top, BackColor = Color.FromArgb(245, 245, 245), Padding = new(4) };

        _backBtn = new() { Text = "◀", Width = 30, Height = 28, Location = new(4, 4), Enabled = false };
        _backBtn.Click += (_, _) => _browser.GoBack();

        _forwardBtn = new() { Text = "▶", Width = 30, Height = 28, Location = new(36, 4), Enabled = false };
        _forwardBtn.Click += (_, _) => _browser.GoForward();

        var refreshBtn = new Button { Text = "⟳", Width = 30, Height = 28, Location = new(68, 4) };
        refreshBtn.Click += (_, _) => _browser.Refresh();

        var ieModeBtn = new Button { Text = "IE11", Width = 40, Height = 28, Location = new(100, 4) };
        var ieVersions = new[] { 11001, 10001, 9999, 8888, 7000 };
        var ieNames = new[] { "IE11", "IE10", "IE9", "IE8", "IE7" };
        int ieIdx = 0;
        ieModeBtn.Click += (_, _) =>
        {
            ieIdx = (ieIdx + 1) % ieVersions.Length;
            ieModeBtn.Text = ieNames[ieIdx];
            SetIEVersion(ieVersions[ieIdx]);
            _browser.Refresh();
        };

        _urlBar = new()
        {
            Location = new(182, 6), Height = 24,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
            Font = new("Segoe UI", 10)
        };
        _urlBar.KeyPress += (_, e) =>
        {
            if (e.KeyChar == (char)Keys.Enter) { NavigateTo(_urlBar.Text); e.Handled = true; }
        };

        _browser = new() { Dock = DockStyle.Fill };
        _browser.Navigated += OnNavigated;
        _browser.DocumentCompleted += OnDocumentCompleted;

        _statusBar = new() { Dock = DockStyle.Bottom, SizingGrip = false };
        _statusLabel = new("Loading...");
        _statusBar.Items.Add(_statusLabel);

        _toolbar.Controls.AddRange([_backBtn, _forwardBtn, refreshBtn, ieModeBtn, _urlBar]);
        Controls.AddRange([_browser, _toolbar, _statusBar]);

        Resize += (_, _) => _urlBar.Width = Math.Max(100, ClientSize.Width - 194);
        _urlBar.Width = ClientSize.Width - 194;
    }

    void OnNavigated(object? _, WebBrowserNavigatedEventArgs e)
    {
        _urlBar.Text = e.Url?.ToString() ?? "";
        _backBtn.Enabled = _browser.CanGoBack;
        _forwardBtn.Enabled = _browser.CanGoForward;
        var title = _browser.Document?.Title;
        Text = string.IsNullOrEmpty(title) ? "IE Mode Viewer" : $"{title} - IE Mode Viewer";
        _statusLabel.Text = $"Loaded: {e.Url?.Host}";
    }

    void OnDocumentCompleted(object? _, WebBrowserDocumentCompletedEventArgs e)
    {
        try { _statusLabel.Text = $"{GetDocumentMode()} | {e.Url?.Host}"; }
        catch { _statusLabel.Text = $"Completed: {e.Url?.Host}"; }
    }

    string GetDocumentMode()
    {
        try
        {
            var doc = _browser.Document?.DomDocument;
            if (doc == null) return "mode:?";
            var docMode = doc.GetType().InvokeMember("documentMode",
                System.Reflection.BindingFlags.GetProperty, null, doc, null);
            return docMode != null ? $"IE{docMode}" : "mode:?";
        }
        catch { return "mode:?"; }
    }

    static void SetIEVersion(int version)
    {
        try
        {
            var exeName = Path.GetFileName(Environment.ProcessPath);
            using var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");
            key.SetValue(exeName, version, RegistryValueKind.DWord);
        }
        catch { }
    }

    void NavigateTo(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!url.Contains("://") && !url.StartsWith("about:")) url = "https://" + url;
        _statusLabel.Text = "Navigating...";
        _browser.Navigate(url);
    }
}
