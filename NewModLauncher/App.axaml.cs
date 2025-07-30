using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace NewModLauncher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new SplashWindow(() =>
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                desktop.MainWindow = mainWindow;
            });
        }
        Current!.Resources["GlobalAnimationDuration"] = TimeSpan.FromMilliseconds(200);
        
        base.OnFrameworkInitializationCompleted();
    }
}