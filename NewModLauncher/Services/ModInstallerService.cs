using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace NewModLauncher.Services
{
    public class ModInstallerService
    {
        public readonly SettingsService _settingsService;
        private readonly string _amongUsPath;
        public ModInstallerService(SettingsService settingsService, string amongUsPath)
        {
            _settingsService = settingsService;
            _amongUsPath = amongUsPath;
        }
        public async Task InstallModAsync(Action<string, double> onProgress)
        {
            string tempPath = Path.GetTempPath();
            string pluginsPath = Path.Combine(_amongUsPath, "BepInEx", "plugins");
            Directory.CreateDirectory(pluginsPath);


            string repoBase = "https://github.com/CallOfCreator/NewMod/releases/latest/download/";
            string bepinexUrl = "https://builds.bepinex.dev/projects/bepinex_be/738/BepInEx-Unity.IL2CPP-win-x86-6.0.0-be.738%2Baf0cba7.zip";
            string miraApiUrl = "https://github.com/All-Of-Us-Mods/MiraAPI/releases/latest/download/MiraAPI.dll";
            string reactorUrl = "https://github.com/NuclearPowered/Reactor/releases/latest/download/Reactor.dll";
            string newModDllUrl = repoBase + "NewMod.dll";


            bool isMicrosoftStore = _amongUsPath.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase)
                                 || _amongUsPath.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
            string[] modZips = isMicrosoftStore ? new[] { "NewMod-MS.zip" } : new[] { "NewMod.zip" };
            bool installedZip = false;
            string tempZip = Path.Combine(tempPath, "NewMod_temp.zip");
            string tempExtractDir = Path.Combine(tempPath, "NewModExtract");

            foreach (var zipName in modZips)
            {
                string url = repoBase + zipName;
                try
                {
                    onProgress?.Invoke("Downloading " + zipName, 0);
                    await DownloadFileAsync(url, tempZip, progress => onProgress?.Invoke("Downloading " + zipName, progress));


                    if (Directory.Exists(tempExtractDir))
                        Directory.Delete(tempExtractDir, true);

                    ZipFile.ExtractToDirectory(tempZip, tempExtractDir, true);

                    string sourceDir = Path.Combine(tempExtractDir, "NewMod");
                    if (Directory.Exists(sourceDir))
                    {
                        foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                        {
                            var destDir = dirPath.Replace(sourceDir, _amongUsPath);
                            Directory.CreateDirectory(destDir);
                        }
                        foreach (var filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                        {
                            var destFile = filePath.Replace(sourceDir, _amongUsPath);
                            File.Copy(filePath, destFile, true);
                        }
                    }
                    onProgress?.Invoke("Extracted " + zipName, 100);
                    installedZip = true;
                    break;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Logger.Error($"Failed to install: {ex.Message}");
                }
                finally
                {
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                    if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
                }
            }

            if (!installedZip)
            {
                string bepinExPath = Path.Combine(_amongUsPath, "BepInEx");
                if (!Directory.Exists(bepinExPath))
                {
                    string tempBepinEx = Path.Combine(tempPath, "BepInEx_temp.zip");
                    onProgress?.Invoke("Downloading BepInEx", 0);
                    await DownloadFileAsync(bepinexUrl, tempBepinEx, progress => onProgress?.Invoke("Downloading BepInEx", progress));
                    ZipFile.ExtractToDirectory(tempBepinEx, _amongUsPath, true);
                    if (File.Exists(tempBepinEx)) File.Delete(tempBepinEx);
                    onProgress?.Invoke("Extracted BepInEx", 100);
                }

                string tempDll = Path.Combine(tempPath, "NewMod_temp.dll");

                string newModDll = Path.Combine(pluginsPath, "NewMod.dll");
                if (!File.Exists(newModDll))
                {
                    onProgress?.Invoke("Downloading NewMod.dll", 0);
                    await DownloadFileAsync(newModDllUrl, tempDll, progress => onProgress?.Invoke("Downloading NewMod.dll", progress));
                    File.Copy(tempDll, newModDll, true);
                    onProgress?.Invoke("Installed NewMod.dll", 100);
                    if (File.Exists(tempDll)) File.Delete(tempDll);
                }

                string miraApiDll = Path.Combine(pluginsPath, "MiraAPI.dll");
                if (!File.Exists(miraApiDll))
                {
                    onProgress?.Invoke("Downloading MiraAPI.dll", 0);
                    await DownloadFileAsync(miraApiUrl, miraApiDll, progress => onProgress?.Invoke("Downloading MiraAPI.dll", progress));
                    onProgress?.Invoke("Installed MiraAPI.dll", 100);
                }

                string reactorDll = Path.Combine(pluginsPath, "Reactor.dll");
                if (!File.Exists(reactorDll))
                {
                    onProgress?.Invoke("Downloading Reactor.dll", 0);
                    await DownloadFileAsync(reactorUrl, reactorDll, progress => onProgress?.Invoke("Downloading Reactor.dll", progress));
                    onProgress?.Invoke("Installed Reactor.dll", 100);
                }
            }
            onProgress?.Invoke("Install complete", 100);
        }
        public bool IsModInstalled()
        {
            string bepinExPath = Path.Combine(_amongUsPath, "BepInEx");
            string pluginsPath = Path.Combine(bepinExPath, "plugins");
            if (!Directory.Exists(bepinExPath) || !Directory.Exists(pluginsPath))
                return false;

            string[] requiredDlls = { "NewMod.dll", "MiraAPI.dll", "Reactor.dll" };
            foreach (string dll in requiredDlls)
            {
                if (!File.Exists(Path.Combine(pluginsPath, dll)))
                    return false;
            }
            return true;
        }

        public async Task DownloadFileAsyncWrapper(string url, string destination, Action<double> onProgress = null)
        {
            await DownloadFileAsync(url, destination, onProgress);
        }
        private async Task DownloadFileAsync(string url, string destination, Action<double> onProgress)
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}", null, response.StatusCode);

            long totalBytes = response.Content.Headers.ContentLength ?? 0;
            long downloaded = 0;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloaded += bytesRead;
                if (onProgress != null && totalBytes > 0)
                {
                    double progress = (double)downloaded / totalBytes * 100.0;
                    onProgress(progress);
                }
            }
        }
    }
}
