namespace IEHost;

public class UrlPrompt : Form
{
    readonly TextBox _input = new();

    public string Url => _input.Text.Trim();

    public UrlPrompt(string defaultUrl)
    {
        Text = "IE Mode Viewer — First run";
        Size = new(480, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var label = new Label
        {
            Text = "Enter the default address to open in the IE viewer:",
            Location = new(16, 16),
            Size = new(440, 24),
            Font = new("Segoe UI", 10),
        };

        _input = new TextBox
        {
            Text = defaultUrl,
            Location = new(16, 48),
            Size = new(440, 26),
            Font = new("Segoe UI", 11),
        };
        _input.KeyPress += (_, e) =>
        {
            if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; DialogResult = DialogResult.OK; }
        };

        var warn = new Label
        {
            Text = "The viewer enables ActiveX and adds this site to the IE Trusted Sites zone.\nUse it only for trusted legacy pages (DVR, intranet) — not for random web sites.",
            Location = new(16, 84),
            Size = new(440, 38),
            Font = new("Segoe UI", 9),
            ForeColor = Color.FromArgb(176, 80, 0),
        };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new(288, 132), Size = new(80, 30) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new(376, 132), Size = new(80, 30) };

        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange([label, _input, warn, ok, cancel]);
    }
}
