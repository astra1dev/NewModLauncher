using System.Diagnostics;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit.Document;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using NewModLauncher.Services;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Linq;
using NewModLauncher.Views;
using NewModLauncher.ViewModels;
using Avalonia.Platform.Storage;
using Avalonia.Controls.Notifications;

namespace NewModLauncher
{
    public partial class MainWindow : Window
    {
        public SettingsService _settingsService;
        public GamePathService _gamePathService;
        public ModUpdateService _modUpdateService;
        public LauncherUpdateService _launcherUpdateService;
        public ModInstallerService _modInstallerService;
        public VersionCheckService _versionCheckService;
        private bool _isUpdating = false;
        private bool _updateDialogShown = false;
        public string _amongUsPath = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
            InitializeLauncher();
        }
        public async Task InitializeLauncher()
        {
            _settingsService = new SettingsService();
            _gamePathService = new GamePathService(_settingsService);
            _modUpdateService = new ModUpdateService(_settingsService);
            _launcherUpdateService = new LauncherUpdateService(_settingsService);
            _versionCheckService = new VersionCheckService();

            string gradientType = _settingsService.GradientType;
            var startColor = Color.Parse(_settingsService.GradientStartColor);
            var endColor = Color.Parse(_settingsService.GradientEndColor);

            ApplyGradient(gradientType, startColor, endColor);


            _amongUsPath = _gamePathService.LocateAmongUsPath();
            _modInstallerService = new ModInstallerService(_settingsService, _amongUsPath);
            GamePathDisplay.Text = _amongUsPath;

            if (!_modInstallerService.IsModInstalled())
            {
                await _modInstallerService.InstallModAsync(async (item, progress) =>
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusIcon.Text = "⟳";
                        StatusIcon.Foreground = new SolidColorBrush(Color.Parse("#FFCC00"));
                        StatusIconBorder.Background = new SolidColorBrush(Color.Parse("#3D3522"));
                        StatusText.Text = "Installing required files...";
                        StatusText.Foreground = Brushes.Yellow;
                        StatusSubText.Text = "Please wait while we install the mod";
                    });

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        DownloadProgressBar.IsVisible = true;
                        DownloadProgressBar.DownloadItemName = item;
                        DownloadProgressBar.Value = progress;
                        DownloadProgressBar.ShowDownloadProgressText = true;
                        DownloadProgressBar.InvalidateVisual();
                    });
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    DownloadProgressBar.IsVisible = false;
                    DownloadProgressBar.ShowDownloadProgressText = false;
                    DownloadProgressBar.DownloadItemName = "";
                    DownloadProgressBar.Value = 0;

                    StatusIcon.Text = "✓";
                    StatusIcon.Foreground = Brushes.LimeGreen;
                    StatusIconBorder.Background = new SolidColorBrush(Color.Parse("#3B4255"));
                    StatusText.Text = "Mod Installed Successfully, Ready!";
                    StatusText.Foreground = Brushes.Green;
                });
            }

            await UpdateVersionInformationAsync();

            CheckInstalledMods();
            UpdateBetaTabVisibility();
        }
        public void UpdateBetaTabVisibility()
        {
            BetaTab.IsVisible = _settingsService.NightlyBuildsEnabled;
        }
        public void ApplyGradient(string gradientType, Color startColor, Color endColor)
        {
            if (gradientType.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                Background = Brushes.Black;
                return;
            }
            if (gradientType.Equals("Linear", StringComparison.OrdinalIgnoreCase))
            {
                var linearBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    [
                        new GradientStop(startColor, 0),
                        new GradientStop(endColor, 1)
                    ]
                };
                Background = linearBrush;
            }
            else if (gradientType.Equals("Radial", StringComparison.OrdinalIgnoreCase))
            {
                var radialBrush = new RadialGradientBrush
                {
                    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    GradientStops =
                    [
                        new GradientStop(startColor, 0),
                        new GradientStop(endColor, 1)
                    ]
                };
                Background = radialBrush;
            }
        }
        public async Task UpdateVersionInformationAsync()
        {
            string launcherVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            _settingsService.LauncherVersion = launcherVersion;


            bool IsNewModPresent = _versionCheckService.IsNewModPresent(_amongUsPath, out var newModVersion);
            _settingsService.ModVersion = newModVersion;

            string gameVersion = _gamePathService.GetAmongUsVersion(_amongUsPath);
            _settingsService.GameVersion = gameVersion;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LauncherVersionText.Text = $"Launcher Version: {launcherVersion}";
                ModVersionText.Text = $"Mod Version: {newModVersion}";
                GameVersionText.Text = $"Among Us Version: {gameVersion}";
            });

            Logger.Info($"GamePath: {_amongUsPath}");
            Logger.Info($"LauncherVersion: {launcherVersion}, ModVersion: {newModVersion}, GameVersion: {gameVersion}");


            await CheckUpdatesAsync();
        }
        public static readonly StyledProperty<string> DownloadIconProperty =
            AvaloniaProperty.Register<ProgressBar, string>(nameof(DownloadIcon), "↻");

        public string DownloadIcon
        {
            get => GetValue(DownloadIconProperty);
            set => SetValue(DownloadIconProperty, value);
        }
        public async Task<bool> CheckExtraDllsAsync()
        {
            string pluginsPath = Path.Combine(_amongUsPath, "BepInEx", "plugins");
            if (!Directory.Exists(pluginsPath)) return true;

            var dllFiles = Directory.GetFiles(pluginsPath, "*.dll");
            var allowed = new string[] { "NewMod.dll", "MiraAPI.dll", "Reactor.dll", "yanplaRoles.dll", "LaunchpadReloaded.dll" };
            var extraDlls = dllFiles
                .ToArray()
                .Where(f => !allowed.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (extraDlls.Count > 0)
            {
                string result = await ShowExtraDllsPopupAsync(extraDlls);
                if (result == "rename")
                {
                    foreach (var file in extraDlls)
                    {
                        string newPath = file + ".old";
                        File.Move(file, newPath);
                    }
                }
                return result != "cancel";
            }
            return true;
        }
        public async Task CheckUpdatesAsync()
        {

            StatusText.Text = "Checking for updates...";
            StatusSubText.Text = "Verifying mod version";
            StatusIcon.Text = "↻";
            StatusIcon.Foreground = new SolidColorBrush(Color.Parse("#FFCC00"));
            StatusIconBorder.Background = new SolidColorBrush(Color.Parse("#3D3522"));

            bool modUpdate = await _modUpdateService.IsModUpdateAvailableAsync();

            if (modUpdate)
            {
                string latestVersion = await _modUpdateService.FetchLatestModVersionAsync();

                var notification = new Notification()
                {
                    Title = "NewMod Update 🚀",
                    Message = $"Heads up! NewMod v{latestVersion} is ready to install!\n🔥 Experience the latest features & fixes.\n\nClick this notification to download now! ⬇️",
                    OnClick = async () =>
                    {
                        await UpdateNewModAsync();
                    }
                };

                if (!_updateDialogShown && !_isUpdating)
                {
                    StatusText.Text = "Updates available!";
                    StatusSubText.Text = "New versions detected";
                    StatusIcon.Text = "↻"; // Update icon
                    StatusIcon.Foreground = new SolidColorBrush(Color.Parse("#FFCC00"));
                    StatusIconBorder.Background = new SolidColorBrush(Color.Parse("#3D3522"));
                    _updateDialogShown = true;
                    await ShowMessageAsync("Updates Available", "A new version of NewMod is available! 🚀\nPress Download Now in the notification or here.", true, showUpdateButton: true);
                }
            }
            else
            {
                StatusText.Text = "NewMod is up-to-date.";
                StatusSubText.Text = "All components are up-to-date";
                StatusIcon.Text = "✓"; // Check mark
                StatusText.Foreground = Brushes.LimeGreen;
                StatusIcon.Foreground = Brushes.LimeGreen;
                StatusIconBorder.Background = new SolidColorBrush(Color.Parse("#3B4255"));
            }
            LaunchGameButton.IsEnabled = true;
        }
        public async Task UpdateNewModAsync()
        {
            if (_isUpdating) return;

            _isUpdating = true;
            _updateDialogShown = false;

            string pluginDir = Path.Combine(_settingsService.AmongUsPath, "BepInEx", "plugins");
            string file = Path.Combine(pluginDir, "NewMod.dll");
            string oldFile = Path.Combine(pluginDir, "NewMod.dll.old");


            var (hasZip, zipUrl, dllUrl) = await _modUpdateService.GetDownloadUrlsAsync();
            string downloadUrl = hasZip ? zipUrl : dllUrl;
            bool isZip = hasZip;
            string tempFile = Path.Combine(Path.GetTempPath(), isZip ? "NewMod_temp.zip" : "NewMod_temp.dll");

            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText.Text = "Downloading update...";
                    StatusText.Foreground = Brushes.Yellow;
                    DownloadProgressBar.IsVisible = true;
                    DownloadProgressBar.DownloadItemName = "NewMod.zip";
                    DownloadProgressBar.Value = 0;
                    DownloadProgressBar.Maximum = 100;
                    DownloadProgressBar.ShowDownloadProgressText = true;
                    DownloadProgressBar.InvalidateVisual();
                });

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "NewModLauncher");
                var response = await client.GetAsync(downloadUrl);

                if (response.IsSuccessStatusCode)
                {
                    long totalBytes = response.Content.Headers.ContentLength ?? -1;
                    long totalRead = 0;
                    byte[] buffer = new byte[8192];

                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

                    int read;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;
                        if (totalBytes > 0)
                        {
                            double progress = ((double)totalRead / totalBytes) * 100;
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                DownloadProgressBar.Value = progress;
                                DownloadProgressBar.InvalidateVisual();
                                StatusText.Text = $"Downloading... {progress:0.0}%";
                            });
                        }
                    }

                    if (isZip)
                    {
                        using var archive = ZipFile.OpenRead(tempFile);
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                            {
                                string destPath = Path.Combine(pluginDir, entry.Name);

                                if (File.Exists(destPath))
                                {
                                    string backupPath = destPath + ".old";
                                    if (File.Exists(backupPath))
                                        File.Delete(backupPath);
                                    File.Move(destPath, backupPath);
                                }
                                entry.ExtractToFile(destPath, true);
                            }
                        }
                    }
                    else
                    {
                        if (File.Exists(file))
                        {
                            if (File.Exists(oldFile))
                            {
                                File.Delete(oldFile);
                            }
                            File.Move(file, oldFile);
                        }
                        File.Move(tempFile, file);
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusText.Text = "Update downloaded successfully!";
                        StatusText.Foreground = Brushes.Green;
                        LaunchGameButton.IsEnabled = true;
                    });
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusText.Text = "Failed to download update.";
                        StatusText.Foreground = Brushes.Red;
                    });
                }
            }
            catch (IOException ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText.Text = "Error: File in use.";
                    StatusText.Foreground = Brushes.Red;
                });
            }
            catch (Exception e)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText.Text = "An unexpected error occurred.";
                    StatusText.Foreground = Brushes.Red;
                });
                Logger.Error(e.ToString());
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    DownloadProgressBar.IsVisible = false;
                    DownloadProgressBar.Value = 0;
                    DownloadProgressBar.ShowDownloadProgressText = false;
                    DownloadProgressBar.DownloadItemName = "";
                    _isUpdating = false;
                    _updateDialogShown = false;
                });
            }
            await UpdateVersionInformationAsync();
        }
        public async Task CheckAndUpdateLauncherAsync()
        {
            bool updateAvailable = await _launcherUpdateService.IsLauncherUpdateAvailableAsync();
            if (!updateAvailable)
            {
                await ShowMessageAsync("Launcher Update", "You're already on the latest version.", true);
                return;
            }

            var dialogResult = await ShowDialogAsync("Launcher Update", "A new version is available! Download and restart now?", true, showConfirmButton: true);
            if (dialogResult != "confirm") return;

            StatusText.Text = "Downloading Launcher Update...";
            var tempExe = await _launcherUpdateService.DownloadLatestLauncherAsync();
            if (tempExe == null)
            {
                await ShowMessageAsync("Update Error", "Failed to download launcher update.", false);
                return;
            }

            StatusText.Text = "Updating and restarting...";
            await Task.Delay(1000);
            _launcherUpdateService.SelfUpdate(tempExe);
        }

        public async void OnChangePathClick(object? sender, RoutedEventArgs e)
        {
            var storageProvider = StorageProvider;
            var folder = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Among Us Directory",
                AllowMultiple = false
            });
            if (folder.Count > 0 && folder[0] is not null)
            {
                string result = folder[0].Path.LocalPath;
                try
                {
                    if (!string.IsNullOrWhiteSpace(result) && File.Exists(Path.Combine(result, "Among Us.exe")))
                    {
                        _settingsService.AmongUsPath = result;
                        _amongUsPath = result;
                        GamePathDisplay.Text = result;

                        _modInstallerService = new ModInstallerService(_settingsService, _amongUsPath);

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            StatusText.Text = "Checking dependencies for new location...";
                            StatusText.Foreground = new SolidColorBrush(Color.Parse("#FFCC00"));
                        });

                        if (!_modInstallerService.IsModInstalled())
                        {
                            await _modInstallerService.InstallModAsync((item, progress) =>
                            {
                                Dispatcher.UIThread.Post(() =>
                                 {
                                     DownloadProgressBar.IsVisible = true;
                                     DownloadProgressBar.DownloadItemName = item;
                                     DownloadProgressBar.Value = progress;
                                     DownloadProgressBar.ShowDownloadProgressText = true;
                                     DownloadProgressBar.InvalidateVisual();
                                     StatusText.Text = $"{item}... {progress:0.0}%";
                                     StatusIcon.Text = "↻";
                                     StatusSubText.Text = "Installing required files...";
                                     StatusText.Foreground = Brushes.Yellow;
                                 });
                            });
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                DownloadProgressBar.IsVisible = false;
                                DownloadProgressBar.ShowDownloadProgressText = false;
                                DownloadProgressBar.DownloadItemName = "";
                                DownloadProgressBar.Value = 0;
                                StatusText.Text = "Mod Installed Successfully, Ready!";
                                StatusText.Foreground = Brushes.Green;
                            });
                            await Task.Delay(1200);
                        }
                        else
                        {
                            StatusText.Text = "All required mod files found in new location. No install needed.";
                            StatusText.Foreground = Brushes.Green;
                        }

                        await Task.Delay(1200);
                        await UpdateVersionInformationAsync();
                    }
                }
                catch { }
            }
        }
        public async void OnCheckUpdatesClick(object? sender, RoutedEventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText.Text = "Checking for updates...";
                StatusText.Foreground = new SolidColorBrush(Color.Parse("#FFCC00"));
            });

            await CheckUpdatesAsync();
        }
        public async void OnLaunchGameClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_amongUsPath) || !File.Exists(Path.Combine(_amongUsPath, "Among Us.exe")))
            {
                await ShowMessageAsync("Error", "Among Us executable not found.", false);
                return;
            }
            bool proceed = await CheckExtraDllsAsync();
            if (!proceed) return;

            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(_amongUsPath, "Among Us.exe"),
                UseShellExecute = true
            };
            StatusText.Text = "Launching game...";
            StatusSubText.Text = "Starting Among Us";
            StatusIcon.Text = "⟳";
            StatusIcon.Foreground = new SolidColorBrush(Color.Parse("#FFCC00"));
            StatusIconBorder.Background = new SolidColorBrush(Color.Parse("#3D3522"));

            Process.Start(psi);

            StatusText.Text = "Game launched successfully!";
            StatusText.Foreground = new SolidColorBrush(Color.Parse("#4CAF50"));
            StatusSubText.Text = "Among Us is now running";
            StatusIcon.Text = "✓";
            StatusIcon.Foreground = new SolidColorBrush(Color.Parse("#4CAF50"));
            StatusIconBorder.Background = new SolidColorBrush(Color.Parse("#3B4255"));
            WindowState = WindowState.Minimized;

        }
        public async Task ShowMessageAsync(string title, string message, bool isSuccess, bool showUpdateButton = false, IBrush? customBrush = null)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.Parse("#1A1A1A"))
            };

            var mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#252525")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20)
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            IBrush foregroundBrush;
            if (customBrush != null)
            {
                foregroundBrush = customBrush;
            }
            else
            {
                foregroundBrush = isSuccess ? Brushes.LimeGreen : Brushes.Red;
            }

            var textBlock = new TextBlock
            {
                Text = message,
                Foreground = foregroundBrush,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            stackPanel.Children.Add(textBlock);

            if (showUpdateButton)
            {
                string buttonText = "Update Now";
                Action buttonAction = async () =>
                {
                    dialog.Close();
                    await UpdateNewModAsync();
                };


                if (customBrush != null && customBrush.Equals(Brushes.Orange))
                {
                    buttonText = "Install Anyway";
                    buttonAction = () => dialog.Close();
                }

                var actionButton = new Button
                {
                    Content = buttonText,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 140,
                    Height = 40,
                    Background = new SolidColorBrush(Color.Parse("#2D8A40")),
                    Foreground = Brushes.White,
                    CornerRadius = new CornerRadius(4),
                    FontWeight = FontWeight.SemiBold
                };

                actionButton.Click += (_, __) => buttonAction();
                stackPanel.Children.Add(actionButton);
            }

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 100,
                Height = 40,
                Background = new SolidColorBrush(Color.Parse("#444444")),
                Foreground = Brushes.White,
                CornerRadius = new CornerRadius(4),
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 10, 0, 0)
            };
            okButton.Click += (_, __) => dialog.Close();

            stackPanel.Children.Add(okButton);

            mainBorder.Child = stackPanel;
            dialog.Content = mainBorder;

            try
            {
                await dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {

                Logger.Log("ERROR", $"Error showing message dialog: {ex.Message}");
            }
        }
        public async Task<string> ShowDialogAsync(string title, string message, bool isSuccess, bool showConfirmButton = false, IBrush? customBrush = null)
        {
            string result = "cancel";

            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.Parse("#1A1A1A"))
            };

            var mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#252525")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20)
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };


            IBrush foregroundBrush;
            if (customBrush != null)
            {
                foregroundBrush = customBrush;
            }
            else
            {
                foregroundBrush = isSuccess ? Brushes.LimeGreen : Brushes.White;
            }

            var textBlock = new TextBlock
            {
                Text = message,
                Foreground = foregroundBrush,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            stackPanel.Children.Add(textBlock);


            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (showConfirmButton)
            {
                var confirmButton = new Button
                {
                    Content = "Yes",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 100,
                    Height = 40,
                    Background = new SolidColorBrush(Color.Parse("#2D8A40")),
                    Foreground = Brushes.White,
                    CornerRadius = new CornerRadius(4),
                    FontWeight = FontWeight.SemiBold
                };

                confirmButton.Click += (_, __) =>
                {
                    result = "confirm";
                    dialog.Close();
                };

                buttonPanel.Children.Add(confirmButton);
            }

            var cancelButton = new Button
            {
                Content = showConfirmButton ? "No" : "OK",
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 100,
                Height = 40,
                Background = new SolidColorBrush(Color.Parse("#444444")),
                Foreground = Brushes.White,
                CornerRadius = new CornerRadius(4),
                FontWeight = FontWeight.SemiBold
            };

            cancelButton.Click += (_, __) =>
            {
                result = "cancel";
                dialog.Close();
            };

            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);

            mainBorder.Child = stackPanel;
            dialog.Content = mainBorder;

            try
            {
                await dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {

                Logger.Log("ERROR", $"Error showing dialog: {ex.Message}");
            }

            return result;
        }
        public async Task<string> ShowExtraDllsPopupAsync(List<string> extraDlls)
        {
            string result = "cancel";

            var dialog = new Window
            {
                Title = "Incompatible Mods Detected",
                Width = 620,
                Height = 500,
                Padding = new Thickness(20),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.Parse("#1A1A1A"))
            };

            var mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#252525")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20)
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 15,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(0, 0, 0, 10)
            };


            var warningIcon = new TextBlock
            {
                Text = "⚠️",
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center
            };

            var headerText = new TextBlock
            {
                Text = "Incompatible Mods Detected",
                Foreground = new SolidColorBrush(Color.Parse("#FFCC00")),
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };

            headerPanel.Children.Add(warningIcon);
            headerPanel.Children.Add(headerText);
            stackPanel.Children.Add(headerPanel);

            var warningMessage = new TextBlock
            {
                Text = "The following mods may cause compatibility issues with NewMod:",
                Foreground = Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(warningMessage);

            var dllListBorder = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#303030")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 5, 0, 10)
            };

            var dllListPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8
            };

            foreach (var dll in extraDlls.Select(f => Path.GetFileName(f)))
            {
                var dllRow = new Grid();
                dllRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                dllRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                var fileIcon = new TextBlock
                {
                    Text = "📄",
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(fileIcon, 0);

                var dllText = new TextBlock
                {
                    Text = dll,
                    Foreground = Brushes.LightGray,
                    FontWeight = FontWeight.SemiBold,
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(dllText, 1);

                dllRow.Children.Add(fileIcon);
                dllRow.Children.Add(dllText);
                dllListPanel.Children.Add(dllRow);
            }

            dllListBorder.Child = dllListPanel;
            stackPanel.Children.Add(dllListBorder);

            var instructionText = new TextBlock
            {
                Text = "These mods may not be compatible with NewMod and could unexpected behavior.",
                Foreground = Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            };
            stackPanel.Children.Add(instructionText);


            var renameExplanation = new TextBlock
            {
                Text = "• 'Rename to .old' will backup these files and allow the game to run without them. The files will be renamed with a .old extension and can be restored later if needed.",
                Foreground = Brushes.LightGray,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 0)
            };
            stackPanel.Children.Add(renameExplanation);

            var launchExplanation = new TextBlock
            {
                Text = "• 'Launch Anyway' will attempt to run with these mods installed (not recommended, may cause issues).",
                Foreground = Brushes.LightGray,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            };
            stackPanel.Children.Add(launchExplanation);

            var cancelExplanation = new TextBlock
            {
                Text = "• 'Cancel' will stop the launch process and return to the launcher.",
                Foreground = Brushes.LightGray,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 10)
            };
            stackPanel.Children.Add(cancelExplanation);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var renameButton = new Button
            {
                Content = "Rename to .old",
                Width = 150,
                Height = 40,
                Background = new SolidColorBrush(Color.Parse("#D68C00")),
                Foreground = Brushes.White,
                CornerRadius = new CornerRadius(4),
                FontWeight = FontWeight.SemiBold
            };
            renameButton.Click += (_, __) =>
            {
                result = "rename";
                dialog.Close();
            };

            var launchButton = new Button
            {
                Content = "Launch Anyway",
                Width = 150,
                Height = 40,
                Background = new SolidColorBrush(Color.Parse("#2D8A40")),
                Foreground = Brushes.White,
                CornerRadius = new CornerRadius(4),
                FontWeight = FontWeight.SemiBold
            };
            launchButton.Click += (_, __) =>
            {
                result = "open";
                dialog.Close();
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 40,
                Background = new SolidColorBrush(Color.Parse("#444444")),
                Foreground = Brushes.White,
                CornerRadius = new CornerRadius(4),
                FontWeight = FontWeight.SemiBold
            };
            cancelButton.Click += (_, __) =>
            {
                result = "cancel";
                dialog.Close();
            };

            buttonPanel.Children.Add(renameButton);
            buttonPanel.Children.Add(launchButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(buttonPanel);
            mainBorder.Child = stackPanel;
            dialog.Content = mainBorder;

            try
            {
                await dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                return "cancel";
            }

            return result;
        }
        public void CheckInstalledMods()
        {
            string pluginsPath = Path.Combine(_amongUsPath, "BepInEx", "plugins");
            if (Directory.Exists(pluginsPath))
            {
                var files = Directory.GetFiles(pluginsPath, "*.dll")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .ToArray();


                _settingsService.InstalledMods = files;

                var modsView = MainTabControl.Items
                    .OfType<TabItem>()
                    .Select(t => t.Content)
                    .OfType<ModsView>()
                    .FirstOrDefault();

                if (modsView != null)
                {
                    if (modsView.DataContext is ModsViewModel viewModel)
                    {
                        _gamePathService.GetAmongUsVersion(_amongUsPath);

                        viewModel.CheckModInstallStates(files);
                        viewModel.CheckForModUpdatesAsync();
                    }
                }
            }
        }
    }
}