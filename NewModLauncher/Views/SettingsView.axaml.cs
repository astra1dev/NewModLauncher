using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NewModLauncher.Views
{
    public class BoolToNightlyTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? "Disable nightly builds" : "Enable nightly builds";
            }
            return "Enable nightly builds";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();

            if (this.GetVisualRoot() is MainWindow mainWindow)
            {
                UpdateNightlyBuildButton(mainWindow._settingsService.NightlyBuildsEnabled);
            }
        }
        private void UpdateNightlyBuildButton(bool isEnabled)
        {
            if (NightlyBuildButton == null) return;

            if (isEnabled)
            {
                NightlyBuildButton.Content = "Disable Nightly Testing";
                NightlyBuildButton.Background = new SolidColorBrush(Color.Parse("#D32F2F"));
                NightlyBuildButton.Foreground = Brushes.White;
                NightlyBuildButton.Classes.Clear();
            }
            else
            {
                NightlyBuildButton.Content = "Register for Nightly Testing";
                NightlyBuildButton.Background = null;
                NightlyBuildButton.Foreground = Brushes.White;
                NightlyBuildButton.Classes.Clear();
                NightlyBuildButton.Classes.Add("secondary");
            }
        }

        private void OnThemeToggleChanged(object? sender, RoutedEventArgs e)
        {
            bool isDark = DarkThemeToggle.IsChecked ?? false;
            Application.Current!.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        }

        private void OnAnimationsToggleChanged(object? sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
            {
                bool enableAnimations = AnimationsToggle.IsChecked == true;
                mainWindow._settingsService.AnimationsEnabled = enableAnimations;
                mainWindow._settingsService.Save();

                Application.Current!.Resources["GlobalAnimationDuration"] =
                enableAnimations ? TimeSpan.FromMilliseconds(180) : TimeSpan.Zero;
            }
        }

        public void OnApplyGradientClick(object? sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
            {
                string gradientType = (GradientTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";
                Color startColor = StartColorPicker.Color;
                Color endColor = EndColorPicker.Color;

                mainWindow.ApplyGradient(gradientType, startColor, endColor);
                mainWindow._settingsService.GradientType = gradientType;
                mainWindow._settingsService.GradientStartColor = startColor.ToString();
                mainWindow._settingsService.GradientEndColor = endColor.ToString();
            }
        }

        public async void OnSettingsCheckUpdatesClick(object? sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    mainWindow.StatusText.Text = "Checking for updates...";
                    mainWindow.StatusText.Foreground = new SolidColorBrush(Color.Parse("#FFCC00"));
                });

                await mainWindow.CheckUpdatesAsync();

                string message = mainWindow.StatusText.Text;
                bool isSuccess = message.Contains("up-to-date");
                await mainWindow.ShowMessageAsync("Update Status", message, isSuccess);
            }
        }
        private async void OnNightlyBuildButtonClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = this.GetVisualRoot() as MainWindow;

            if (mainWindow._settingsService.NightlyBuildsEnabled)
            {
                await DisableNightlyBuilds(mainWindow);
            }
            else
            {
                await EnableNightlyBuilds(mainWindow);
            }
        }
        public async void OnCheckLauncherUpdatesClick(object sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
                await mainWindow.CheckAndUpdateLauncherAsync();
        }

        private async Task DisableNightlyBuilds(MainWindow mainWindow)
        {
            try
            {

                var dialog = new Window
                {
                    Title = "Disable Nightly Builds",
                    Width = 500,
                    Height = 300,
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

                var titleText = new TextBlock
                {
                    Text = "Disable Nightly Builds",
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var infoText = new TextBlock
                {
                    Text = "Are you sure you want to disable nightly builds? You will no longer receive experimental features and updates.",
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var disableButton = new Button
                {
                    Content = "Disable Nightly Builds",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 200,
                    Height = 40,
                    Background = new SolidColorBrush(Color.Parse("#D32F2F")),
                    Foreground = Brushes.White,
                    CornerRadius = new CornerRadius(4),
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(0, 10, 0, 10)
                };

                disableButton.Click += (_, __) =>
                {
                    try
                    {

                        mainWindow._settingsService.NightlyBuildsEnabled = false;
                        mainWindow._settingsService.Save(); // Ensure changes are saved


                        Dispatcher.UIThread.Post(() =>
                        {
                            UpdateNightlyBuildButton(false);
                        });

                        mainWindow.UpdateBetaTabVisibility();

                        var messagePanel = new StackPanel
                        {
                            Margin = new Thickness(0, 5, 0, 0)
                        };

                        var successText = new TextBlock
                        {
                            Text = "✓ Nightly builds disabled!",
                            Foreground = Brushes.LimeGreen,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };

                        var restartText = new TextBlock
                        {
                            Text = "Changes applied successfully.",
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 5, 0, 0)
                        };

                        messagePanel.Children.Add(successText);
                        messagePanel.Children.Add(restartText);


                        stackPanel.Children.Remove(disableButton);
                        stackPanel.Children.Add(messagePanel);


                        Task.Delay(2000).ContinueWith(_ =>
                        {
                            Dispatcher.UIThread.Post(() => dialog.Close());
                        });
                    }
                    catch (Exception ex)
                    {
                        var errorText = new TextBlock
                        {
                            Text = $"Error disabling nightly builds: {ex.Message}",
                            Foreground = Brushes.Red,
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        stackPanel.Children.Add(errorText);
                    }
                };

                var cancelButton = new Button
                {
                    Content = "Cancel",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 100,
                    Height = 40,
                    Background = new SolidColorBrush(Color.Parse("#444444")),
                    Foreground = Brushes.White,
                    CornerRadius = new CornerRadius(4),
                    FontWeight = FontWeight.SemiBold
                };

                cancelButton.Click += (_, __) => dialog.Close();

                stackPanel.Children.Add(titleText);
                stackPanel.Children.Add(infoText);
                stackPanel.Children.Add(disableButton);
                stackPanel.Children.Add(cancelButton);

                mainBorder.Child = stackPanel;
                dialog.Content = mainBorder;

                await dialog.ShowDialog(mainWindow);
            }
            catch (Exception ex)
            {
                await mainWindow.ShowMessageAsync("Error", $"Failed to disable nightly builds: {ex.Message}", false);
            }
        }

        private async Task EnableNightlyBuilds(MainWindow mainWindow)
        {
            try
            {

                var dialog = new Window
                {
                    Title = "Nightly Build Access",
                    Width = 500,
                    Height = 300,
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

                var titleText = new TextBlock
                {
                    Text = "Enable Nightly Builds",
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var infoText = new TextBlock
                {
                    Text = "Clicking the button below will enable nightly builds for your launcher.",
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var warningText = new TextBlock
                {
                    Text = "Warning: Nightly builds are experimental and may contain bugs.",
                    Foreground = Brushes.Orange,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var enableButton = new Button
                {
                    Content = "Enable Nightly Builds",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 200,
                    Height = 40,
                    Background = new SolidColorBrush(Color.Parse("#2D8A40")),
                    Foreground = Brushes.White,
                    CornerRadius = new CornerRadius(4),
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(0, 10, 0, 10)
                };

                enableButton.Click += (_, __) =>
                {
                    try
                    {

                        mainWindow._settingsService.NightlyBuildsEnabled = true;
                        mainWindow._settingsService.Save();

                        Dispatcher.UIThread.Post(() =>
                        {
                            UpdateNightlyBuildButton(true);
                        });

                        mainWindow.UpdateBetaTabVisibility();

                        var messagePanel = new StackPanel
                        {
                            Margin = new Thickness(0, 5, 0, 0)
                        };

                        var successText = new TextBlock
                        {
                            Text = "✓ Nightly builds enabled!",
                            Foreground = Brushes.LimeGreen,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };

                        messagePanel.Children.Add(successText);

                        if (mainWindow.MainTabControl.Items.OfType<TabItem>().Any(t => t.Header?.ToString() == "Beta Testers"))
                        {
                            var availableText = new TextBlock
                            {
                                Text = "Beta Testers tab is now available.",
                                Foreground = Brushes.White,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Margin = new Thickness(0, 5, 0, 0)
                            };
                            messagePanel.Children.Add(availableText);
                        }
                        stackPanel.Children.Remove(enableButton);
                        stackPanel.Children.Add(messagePanel);
                    }
                    catch (Exception ex)
                    {
                        var errorText = new TextBlock
                        {
                            Text = $"Error enabling nightly builds: {ex.Message}",
                            Foreground = Brushes.Red,
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        stackPanel.Children.Add(errorText);
                    }
                };

                var closeButton = new Button
                {
                    Content = "Close",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 100,
                    Height = 40,
                    Background = new SolidColorBrush(Color.Parse("#444444")),
                    Foreground = Brushes.White,
                    CornerRadius = new CornerRadius(4),
                    FontWeight = FontWeight.SemiBold
                };
                closeButton.Click += (_, __) => dialog.Close();

                stackPanel.Children.Add(titleText);
                stackPanel.Children.Add(infoText);
                stackPanel.Children.Add(warningText);
                stackPanel.Children.Add(enableButton);
                stackPanel.Children.Add(closeButton);

                mainBorder.Child = stackPanel;
                dialog.Content = mainBorder;

                await dialog.ShowDialog(mainWindow);
            }
            catch (Exception ex)
            {
                await mainWindow.ShowMessageAsync("Error", $"Failed to enable nightly builds: {ex.Message}", false);
            }
        }
    }
}