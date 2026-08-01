namespace IEHost;

public class UrlPrompt : Form
{
    readonly TextBox _input = new();

    public string Url => _input.Text.Trim();

    public UrlPrompt(string defaultUrl)
    {
        Text = "IE Mode Viewer — First run";
        Size = new(480, 160);
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

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new(288, 88), Size = new(80, 30) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new(376, 88), Size = new(80, 30) };

        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange([label, _input, ok, cancel]);
    }
}
