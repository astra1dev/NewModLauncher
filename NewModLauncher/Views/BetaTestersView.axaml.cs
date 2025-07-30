using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NewModLauncher.Views
{
    public partial class BetaTestersView : UserControl
    {
        private readonly string _nightlyApiUrl = "https://newmod.up.railway.app/api/nightly";
        private bool _isCheckingForNightlyBuilds = false;

        public BetaTestersView()
        {
            InitializeComponent();
        }

        private HttpClient CreateSecureConnection(TimeSpan? timeout = null)
        {
            var httpClient = new HttpClient
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(15)
            };

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(timestamp));

            httpClient.DefaultRequestHeaders.Add("X-Request-Token", token);
            httpClient.DefaultRequestHeaders.Add("X-Client-Timestamp", timestamp);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NewModLauncher");

            return httpClient;
        }

        private async void OnCheckNightlyBuildsClick(object? sender, RoutedEventArgs e)
        {
            if (_isCheckingForNightlyBuilds) return;
            _isCheckingForNightlyBuilds = true;

            try
            {
                ButtonCheckNightly.IsEnabled = false;
                StatusIndicator.IsVisible = true;
                StatusText.Text = "Checking for nightly builds...";
                StatusText.Foreground = new SolidColorBrush(Color.Parse("#FFCC00"));
                CheckProgressBar.Value = 0;

                using (var httpClient = CreateSecureConnection())
                {
                    CheckProgressBar.Value = 30;

                    var response = await httpClient.GetAsync(_nightlyApiUrl);
                    response.EnsureSuccessStatusCode();

                    CheckProgressBar.Value = 70;
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonResponse);
                    var hasNewBuild = jsonDoc.RootElement.TryGetProperty("hasUpdate", out var hasUpdateProp) && hasUpdateProp.GetBoolean();

                    CheckProgressBar.Value = 100;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (hasNewBuild)
                        {
                            StatusText.Text = "New nightly build available!";
                            StatusText.Foreground = new SolidColorBrush(Color.Parse("#00CC66"));
                            DownloadBuildButton.IsVisible = true;
                            DownloadBuildButton.IsEnabled = true;
                        }
                        else
                        {
                            StatusText.Text = "You're up to date with the latest nightly build";
                            StatusText.Foreground = new SolidColorBrush(Color.Parse("#00CC66"));
                            DownloadBuildButton.IsVisible = false;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText.Text = $"Error checking for nightly builds: {ex.Message}";
                    StatusText.Foreground = new SolidColorBrush(Color.Parse("#FF4444"));
                    CheckProgressBar.Value = 0;
                    DownloadBuildButton.IsVisible = false;
                });
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ButtonCheckNightly.IsEnabled = true;
                    _isCheckingForNightlyBuilds = false;
                });
            }
        }

        private async void OnDownloadBuildClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                DownloadBuildButton.IsEnabled = false;

                StatusText.Text = "Downloading nightly build...";
                StatusText.Foreground = new SolidColorBrush(Color.Parse("#FFCC00"));

                using (var httpClient = CreateSecureConnection(TimeSpan.FromMinutes(5)))
                {
                    var response = await httpClient.GetAsync(_nightlyApiUrl);
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonResponse);

                    string downloadUrl = jsonDoc.RootElement.TryGetProperty("dllUrl", out var dllUrlProp)
                        ? dllUrlProp.GetString() ?? ""
                        : "";

                    var mainWindow = this.GetVisualRoot() as MainWindow;
                    if (mainWindow == null)
                        throw new Exception("Could not get MainWindow");

                    string downloadPath = System.IO.Path.Combine(
                        mainWindow._amongUsPath ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "NewMod.dll");

                    if (string.IsNullOrEmpty(downloadUrl))
                        throw new Exception("Download URL not found in API response");

                    await mainWindow._modInstallerService.DownloadFileAsyncWrapper(
                        downloadUrl,
                        downloadPath,
                        progress => { /* Optionally update progress here if you add a bar */ });

                    StatusText.Text = "Nightly build downloaded successfully! Please extract and install manually.";
                    StatusText.Foreground = new SolidColorBrush(Color.Parse("#00CC66"));
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error downloading nightly build: {ex.Message}";
                StatusText.Foreground = new SolidColorBrush(Color.Parse("#FF4444"));
            }
            finally
            {
                DownloadBuildButton.IsEnabled = true;
            }
        }
    }
}
