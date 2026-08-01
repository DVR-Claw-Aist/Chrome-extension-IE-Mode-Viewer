namespace IEHost;

public class SettingsForm : Form
{
    readonly TextBox _url = new();
    readonly TextBox _hotkey = new();
    readonly AppSettings _settings;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;

        Text = "IE Mode Viewer — Settings";
        Size = new(440, 220);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var urlLabel = new Label { Text = "Default URL:", Location = new(16, 20), Size = new(120, 24) };
        _url = new TextBox { Text = settings.DefaultUrl, Location = new(140, 18), Size = new(276, 24) };

        var hotkeyLabel = new Label { Text = "Hotkey:", Location = new(16, 60), Size = new(120, 24) };
        _hotkey = new TextBox { Text = settings.Hotkey, Location = new(140, 58), Size = new(276, 24) };

        var hint = new Label
        {
            Text = "Hotkey format: Ctrl+Alt+X (modifiers + single key)",
            Location = new(140, 84),
            Size = new(276, 20),
            ForeColor = SystemColors.GrayText,
        };

        var browserLabel = new Label
        {
            Text = settings.ChromePath,
            Location = new(16, 116),
            Size = new(400, 24),
            ForeColor = SystemColors.GrayText,
        };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new(236, 148), Size = new(80, 30) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new(324, 148), Size = new(80, 30) };

        ok.Click += (_, _) =>
        {
            _settings.DefaultUrl = _url.Text.Trim();
            _settings.Hotkey = _hotkey.Text.Trim();
            _settings.Save();
        };

        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange([urlLabel, _url, hotkeyLabel, _hotkey, hint, browserLabel, ok, cancel]);
    }
}
