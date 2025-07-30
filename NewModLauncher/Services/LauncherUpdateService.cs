using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NewModLauncher.Services
{
    public class LauncherUpdateService
    {
        private const string LauncherReleasesUrl = "https://api.github.com/repos/CallOfCreator/NewModLauncher/releases/latest";
        private readonly SettingsService _settings;

        public LauncherUpdateService(SettingsService settings)
        {
            _settings = settings;
        }
        public async Task<bool> IsLauncherUpdateAvailableAsync()
        {
            var current = _settings.LauncherVersion;
            var latest = (await FetchLatestLauncherVersionAsync())?.TrimStart('v', 'V').Trim();
            if (string.IsNullOrEmpty(latest))
                return false;
            return VersionUtils.Compare(latest, current) > 0;
        }

        private async Task<string> FetchLatestLauncherVersionAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "NewModLauncher");
            var json = await client.GetStringAsync(LauncherReleasesUrl);
            using var document = JsonDocument.Parse(json);
            return document.RootElement.GetProperty("tag_name").GetString() ?? "";
        }
        public async Task<string?> DownloadLatestLauncherAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "NewModLauncher");
            var json = await client.GetStringAsync(LauncherReleasesUrl);
            using var document = JsonDocument.Parse(json);
            var assets = document.RootElement.GetProperty("assets");
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".exe"))
                {
                    var url = asset.GetProperty("browser_download_url").GetString();
                    if (!string.IsNullOrEmpty(url))
                    {
                        string tempFile = Path.Combine(Path.GetTempPath(), "NewModLauncher_Update.exe");
                        using var response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();
                        await using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                        await response.Content.CopyToAsync(fs);
                        return tempFile;
                    }
                }
            }
            return null;
        }
        public void SelfUpdate(string tempExePath)
        {
            var currentExe = Process.GetCurrentProcess().MainModule!.FileName!;
            var bat = Path.Combine(Path.GetTempPath(), "nml_update.bat");
            var cmd = $@"@echo off
                         timeout /t 2 > nul
                         move /y ""{tempExePath}"" ""{currentExe}""
                         start """" ""{currentExe}""
                         ";
            File.WriteAllText(bat, cmd);
            Process.Start(new ProcessStartInfo
            {
                FileName = bat,
                UseShellExecute = true,
                CreateNoWindow = true
            });
            Environment.Exit(0);
        }
    }
}
