using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace NewModLauncher
{
    public partial class SplashWindow : Window
    {
        private readonly Action? _onComplete;
        public SplashWindow() 
        { 

        }
        public SplashWindow(Action onComplete)
        {
            _onComplete = onComplete;
            InitializeComponent();

            Opened += OnOpened;
        }

        private async void OnOpened(object? sender, EventArgs e)
        {
            await Task.Delay(3000);
            
            Dispatcher.UIThread.Post(() =>
            {
                _onComplete!.Invoke(); 
                Close();
            });
        }
    }
}
