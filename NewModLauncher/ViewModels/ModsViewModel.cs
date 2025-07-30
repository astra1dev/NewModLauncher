using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace NewModLauncher.ViewModels
{
    public class ModsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ModViewModel> Mods { get; } = new();
        private string _currentGameVersion = "";
        public string CurrentGameVersion
        {
            get => _currentGameVersion;
            set
            {
                if (_currentGameVersion != value)
                {
                    _currentGameVersion = value;
                    OnPropertyChanged();
                    UpdateModCompatibility();
                }
            }
        }
        public ModsViewModel()
        {
            Mods.Add(new ModViewModel
            {
                Name = "yanplaRoles",
                IconText = "YR",
                Version = "v0.1.8",
                Description = "yanplaRoles is a mod for the game Among Us that introduces new roles and modifiers.",
                DownloadUrl = "https://github.com/yanpla/yanplaRoles/releases/latest/download/yanplaRoles.dll",
                RepositoryUrl = "https://github.com/yanpla/yanplaRoles",
                SupportVersion = "2024.10.29"
            });
            Mods.Add(new ModViewModel
            {
                Name = "Submerged",
                IconUrl = "https://raw.githubusercontent.com/SubmergedAmongUs/Submerged/main/Submerged/Resources/Images/OptionsIcon.png",
                Version = "v2025.1.30",
                Description = "Submerged is a mod for Among Us which adds a new map into the game.",
                DownloadUrl = "https://github.com/SubmergedAmongUs/Submerged/releases/latest/download/Submerged.dll",
                RepositoryUrl = "https://github.com/SubmergedAmongUs/Submerged",
                SupportVersion = "2024.11.26"
            });
            Mods.Add(new ModViewModel
            {
                Name = "Launchpad Reloaded",
                IconUrl = "https://allofus.dev/static/images/launchpad_no_shadow.png",
                Version = "v0.3.4",
                Description = "A vanilla oriented fun and unique Among Us client mod.",
                DownloadUrl = "https://github.com/All-Of-Us-Mods/LaunchpadReloaded/releases/download/0.3.4/LaunchpadReloaded.dll",
                RepositoryUrl = "https://github.com/All-Of-Us-Mods/LaunchpadReloaded",
                SupportVersion = "2025.4.15"
            });

            foreach (var mod in Mods)
            {
                _ = mod.RefreshIconAsync();
            }
        }

        public void CheckModInstallStates(string[] installedMods)
        {
            foreach (var mod in Mods)
                mod.IsInstalled = installedMods.Contains(mod.Name, System.StringComparer.OrdinalIgnoreCase);
        }

        public void UpdateModCompatibility()
        {
            foreach (var mod in Mods)
                mod.IsCompatible = VersionUtils.Compare(CurrentGameVersion, mod.SupportVersion) >= 0;
        }
        public async Task CheckForModUpdatesAsync()
        {
            foreach (var mod in Mods)
            {
                mod.LatestVersion = await GetLatestVersionFromGithub(mod.RepositoryUrl);
                mod.NotifyUpdateChange();
            };
        }

        private async Task<string> GetLatestVersionFromGithub(string repoUrl)
        {
            try
            {
                var userRepo = repoUrl.Replace("https://github.com/", "").TrimEnd('/');
                var apiUrl = $"https://api.github.com/repos/{userRepo}/releases/latest";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "NewModLauncher");
                var json = await client.GetStringAsync(apiUrl);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
                return tag;
            } catch {}
            return "";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
