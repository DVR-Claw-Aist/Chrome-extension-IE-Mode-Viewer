using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;

namespace IEHost;

record Request(string Type, string? Url);
record Response(string Type, bool Success, string? Error);

class Program
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--viewer")
        {
            string url = args.Length > 1 ? args[1] : "about:blank";
            StartViewer(url);
        }
        else
        {
            SetEmulation();
            RunLoop();
        }
    }

    static void StartViewer(string url)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        SetEmulation();
        SetActiveXSupport();
        AddToTrustedSites(url);
        Application.Run(new ViewerForm(url));
    }

    static void RunLoop()
    {
        while (true)
        {
            string? json;
            try
            {
                json = NativeMessaging.ReadMessage();
            }
            catch
            {
                break;
            }

            if (json == null) break;

            Request? req;
            try
            {
                req = JsonSerializer.Deserialize<Request>(json, JsonOptions);
            }
            catch
            {
                WriteResp(new Response("RESULT", false, "Invalid JSON"));
                continue;
            }

            if (req == null)
            {
                WriteResp(new Response("RESULT", false, "Empty request"));
                break;
            }

            try
            {
                switch (req.Type)
                {
                    case "PING":
                        WriteResp(new Response("PONG", true, null));
                        break;

                    case "OPEN":
                        HandleOpen(req.Url);
                        break;

                    default:
                        WriteResp(new Response("RESULT", false, $"Unknown command: {req.Type}"));
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteResp(new Response("RESULT", false, ex.Message));
            }
        }
    }

    static void HandleOpen(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            WriteResp(new Response("RESULT", false, "No URL provided"));
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath!,
            Arguments = $"--viewer \"{url}\"",
            UseShellExecute = true
        };
        Process.Start(psi);

        WriteResp(new Response("RESULT", true, null));
    }

    static void WriteResp(Response resp)
    {
        NativeMessaging.WriteMessage(JsonSerializer.Serialize(resp, JsonOptions));
    }

    static void SetEmulation()
    {
        try
        {
            var exeName = Path.GetFileName(Environment.ProcessPath);
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");
            key.SetValue(exeName, 11001, Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch { }
    }

    static void SetActiveXSupport()
    {
        try
        {
            var exeName = Path.GetFileName(Environment.ProcessPath);

            using (var k = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_RESTRICT_ACTIVEXINSTALL"))
                k.SetValue(exeName, 0, RegistryValueKind.DWord);

            using (var k = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_DISABLE_ACTIVEX_FILTERING"))
                k.SetValue(exeName, 1, RegistryValueKind.DWord);

            using (var k = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_RESTRICT_FILEDOWNLOAD"))
                k.SetValue(exeName, 0, RegistryValueKind.DWord);

            using (var k = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_SAFE_ACTIVEX"))
                k.SetValue(exeName, 0, RegistryValueKind.DWord);

            using (var k = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ENABLE_DEP"))
                k.SetValue(exeName, 0, RegistryValueKind.DWord);

            // Bypass SSL error pages
            using (var k = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ERROR_PAGE_BYPASS"))
                k.SetValue(exeName, 1, RegistryValueKind.DWord);

            // Disable certificate revocation check
            using (var k = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
                k.SetValue("CertificateRevocation", 0, RegistryValueKind.DWord);
        }
        catch { }
    }

    static void AddToTrustedSites(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host;
            if (string.IsNullOrEmpty(host) || host == "localhost") return;

            var zoneMap = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\Domains\" + host;
            using var key = Registry.CurrentUser.CreateSubKey(zoneMap);
            key.SetValue(uri.Scheme == "https" ? "https" : "http", 2, RegistryValueKind.DWord);
        }
        catch { }
    }
}
