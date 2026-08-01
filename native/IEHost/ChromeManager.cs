using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Win32;

namespace IEHost;

public static class ChromeManager
{
    static readonly string[] KnownPaths =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
    };

    public static string? FindBrowser()
    {
        foreach (var key in new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe",
        })
        {
            try
            {
                using var k = Registry.LocalMachine.OpenSubKey(key);
                var p = k?.GetValue(null)?.ToString();
                if (!string.IsNullOrEmpty(p) && File.Exists(p)) return p;
            }
            catch { }
        }

        foreach (var p in KnownPaths)
        {
            if (File.Exists(p)) return p;
        }
        return null;
    }

    public static string ProfileDir => Path.Combine(AppSettings.SettingsDir, "ChromeProfile");

    public static Process? LaunchBrowser(string browserPath, int port, string? openUrl = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = browserPath,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add($"--remote-debugging-port={port}");
        psi.ArgumentList.Add($"--user-data-dir={ProfileDir}");
        psi.ArgumentList.Add("--no-first-run");
        psi.ArgumentList.Add("--no-default-browser-check");
        psi.ArgumentList.Add("--noerrdialogs");
        psi.ArgumentList.Add("--disable-session-crashed-bubble");
        if (!string.IsNullOrEmpty(openUrl)) psi.ArgumentList.Add(openUrl);
        try { return Process.Start(psi); }
        catch { return null; }
    }

    public static async Task WaitForCdpAsync(int port, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                var r = await client.GetStringAsync($"http://127.0.0.1:{port}/json/version");
                if (!string.IsNullOrEmpty(r)) return;
            }
            catch { }
            await Task.Delay(300);
        }
    }

    public static async Task<string?> GetActiveTabUrl(int port)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var json = await client.GetStringAsync($"http://127.0.0.1:{port}/json");
            using var doc = JsonDocument.Parse(json);

            string? fallback = null;
            double maxLastActive = -1;
            string? lastActiveUrl = null;

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.TryGetProperty("type", out var t) && t.GetString() != "page") continue;
                if (!el.TryGetProperty("url", out var u) || u.GetString() is not string url) continue;
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;

                if (el.TryGetProperty("active", out var a) && a.GetBoolean())
                    return url;

                if (el.TryGetProperty("lastActiveTime", out var lat) && lat.TryGetDouble(out var tMs) && tMs > maxLastActive)
                {
                    maxLastActive = tMs;
                    lastActiveUrl = url;
                }

                fallback = url;
            }
            return lastActiveUrl ?? fallback;
        }
        catch { return null; }
    }
}
