using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NewModLauncher.Services
{
    public class SettingsService
    {
        private readonly string _configPath;
        private JsonNode _cachedConfig;

        public SettingsService()
        {
            var dataFolder = Path.Combine(AppContext.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            _configPath = Path.Combine(dataFolder, "config.json");
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (!File.Exists(_configPath))
            {
                _cachedConfig = new JsonObject();
                Save();
            }
            else
            {
                var json = File.ReadAllText(_configPath);
                _cachedConfig = JsonNode.Parse(json) ?? new JsonObject();
            }
        }

        public void Save()
        {
            try
            {
                string json = _cachedConfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {

                if (File.Exists(_configPath))
                {
                    try
                    {

                        File.SetAttributes(_configPath, FileAttributes.Normal);
                        string json = _cachedConfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(_configPath, json);
                    }
                    catch
                    {

                        string backupPath = _configPath + ".bak";
                        if (File.Exists(backupPath)) File.Delete(backupPath);


                        string json = _cachedConfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(backupPath, json);


                        if (File.Exists(_configPath)) File.Delete(_configPath);
                        File.Move(backupPath, _configPath);
                    }
                }
                else
                {

                    string json = _cachedConfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_configPath, json);
                }
            }
        }

        public string? AmongUsPath
        {
            get => _cachedConfig["AmongUsPath"]?.GetValue<string>();
            set
            {
                _cachedConfig["AmongUsPath"] = value;
                Save();
            }
        }

        public string? GameVersion
        {
            get => _cachedConfig["GameVersion"]?.GetValue<string>();
            set
            {
                _cachedConfig["GameVersion"] = value;
                Save();
            }
        }

        public string LauncherVersion
        {
            get => _cachedConfig["LauncherVersion"]?.GetValue<string>() ?? "1.0.0";
            set
            {
                _cachedConfig["LauncherVersion"] = value;
                Save();
            }
        }

        public string ModVersion
        {
            get => _cachedConfig["ModVersion"]?.GetValue<string>() ?? "";
            set
            {
                _cachedConfig["ModVersion"] = value;
                Save();
            }
        }
        public string GradientType
        {
            get => _cachedConfig["GradientType"]?.GetValue<string>() ?? "None";
            set
            {
                _cachedConfig["GradientType"] = value;
                Save();
            }
        }
        public string GradientStartColor
        {
            get => _cachedConfig["GradientStartColor"]?.GetValue<string>() ?? "#FF000000";
            set
            {
                _cachedConfig["GradientStartColor"] = value;
                Save();
            }
        }
        public string GradientEndColor
        {
            get => _cachedConfig["GradientEndColor"]?.GetValue<string>() ?? "#FF000000";
            set
            {
                _cachedConfig["GradientEndColor"] = value;
                Save();
            }
        }
        public bool NightlyBuildsEnabled
        {
            get => _cachedConfig["NightlyBuildsEnabled"]?.GetValue<bool>() ?? false;
            set
            {
                _cachedConfig["NightlyBuildsEnabled"] = value;
                Save();
            }
        }
        public string[] InstalledMods
        {
            get
            {
                try
                {
                    var modsArray = _cachedConfig["InstalledMods"]?.AsArray();
                    if (modsArray != null)
                    {
                        string[] mods = new string[modsArray.Count];
                        for (int i = 0; i < modsArray.Count; i++)
                        {
                            mods[i] = modsArray[i]?.GetValue<string>() ?? "";
                        }
                        return mods;
                    }
                }
                catch { }

                return [];
            }
            set
            {
                var array = new JsonArray();
                foreach (var mod in value)
                {
                    array.Add(mod);
                }
                _cachedConfig["InstalledMods"] = array;
                Save();
            }
        }

        public bool AnimationsEnabled
        {
            get => _cachedConfig["AnimationsEnabled"]?.GetValue<bool>() ?? true;
            set
            {
                _cachedConfig["AnimationsEnabled"] = value;
                Save();
            }
        }
    }
}
