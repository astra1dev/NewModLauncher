using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using AssetsTools.NET.Extra;
using Microsoft.Win32;

namespace NewModLauncher.Services
{
    public class GamePathService
    {
        private readonly SettingsService _settings;

        public GamePathService(SettingsService settings)
        {
            _settings = settings;
        }

        public string LocateAmongUsPath()
        {
            if (IsValidPath(_settings.AmongUsPath))
                return _settings.AmongUsPath;

            string runningPath = GetPathFromRunningProcess();
            if (IsValidPath(runningPath))
            {
                SavePath(runningPath);
                return runningPath;
            }

            string? regPath = GetPathFromRegistry();
            if (IsValidPath(regPath))
            {
                SavePath(regPath);
                return regPath;
            }

            return string.Empty;
        }

        private bool IsValidPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            return Directory.Exists(path) && File.Exists(Path.Combine(path, "Among Us.exe"));
        }
        // Credit to Daemon for the original implementation found in Reactor.
        public string GetAmongUsVersion(string amongUsPath)
        {
            try
            {
                string ggmPath = Path.Combine(amongUsPath, "Among Us_Data", "globalgamemanagers");
                var assetsManager = new AssetsManager();


                var resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                string tpkResourceName = resourceNames.FirstOrDefault(n => n.EndsWith("type_tree.tpk")) ?? throw new FileNotFoundException("Embedded type_tree.tpk not found. Resource names: " + string.Join(", ", resourceNames));
                using (var tpkStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(tpkResourceName))
                {
                    if (tpkStream == null)
                        throw new FileNotFoundException("Failed to get stream for: " + tpkResourceName);
                    assetsManager.LoadClassPackage(tpkStream);
                }
                var ggmFile = assetsManager.LoadAssetsFile(ggmPath, false);
                assetsManager.LoadClassDatabaseFromPackage(ggmFile.file.Metadata.UnityVersion);

                var playerSettings = assetsManager.GetBaseField(ggmFile, ggmFile.file.GetAssetsOfType(AssetClassID.PlayerSettings)[0]);
                return playerSettings["bundleVersion"].AsString;
            }
            catch (System.Exception e)
            {
                Logger.Error($"Failed to GetAmongUsVersion due:{e.Message}, StackTrace:{e.StackTrace}");
                return "Unknown";
            }
        }
        private string GetPathFromRunningProcess()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("Among Us");
                if (processes.Length > 0)
                {
                    return Path.GetDirectoryName(processes[0].MainModule?.FileName) ?? string.Empty;
                }
            }
            catch { }
            return string.Empty;
        }
        // Inspired by:https://github.com/All-Of-Us-Mods/AOULauncher/blob/master/AOULauncher/Tools/AmongUsLocator.cs#L56
        private string? GetPathFromRegistry()
        {
            try
            {
                object? value = Registry.GetValue(@"HKEY_CLASSES_ROOT\amongus\DefaultIcon", "", null);
                if (value is string s && !string.IsNullOrWhiteSpace(s))
                {
                    s = s.Trim('\"');
                    int index = s.LastIndexOf("Among Us.exe", StringComparison.OrdinalIgnoreCase);
                    if (index > 0)
                    {
                        string path = s[..index].Trim();
                        return path;
                    }
                }
            }
            catch { }
            return null;
        }
        public void SavePath(string path)
        {
            _settings.AmongUsPath = path;
            _settings.Save();
        }
    }
}