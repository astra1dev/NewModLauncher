using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace NewModLauncher.Controls
{
    public class CustomProgressBar : TemplatedControl
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<CustomProgressBar, double>(nameof(Value), 0);

        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<CustomProgressBar, double>(nameof(Maximum), 100);

        public static readonly StyledProperty<string> DownloadItemNameProperty =
            AvaloniaProperty.Register<CustomProgressBar, string>(nameof(DownloadItemName), "");

        public static readonly StyledProperty<bool> ShowDownloadProgressTextProperty =
            AvaloniaProperty.Register<CustomProgressBar, bool>(nameof(ShowDownloadProgressText), true);

        public static readonly StyledProperty<IBrush> ProgressColorProperty =
            AvaloniaProperty.Register<CustomProgressBar, IBrush>(nameof(ProgressColor), Brushes.DeepSkyBlue);

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }
        public string DownloadItemName
        {
            get => GetValue(DownloadItemNameProperty);
            set => SetValue(DownloadItemNameProperty, value);
        }
        public bool ShowDownloadProgressText
        {
            get => GetValue(ShowDownloadProgressTextProperty);
            set => SetValue(ShowDownloadProgressTextProperty, value);
        }
        public IBrush ProgressColor
        {
            get => GetValue(ProgressColorProperty);
            set => SetValue(ProgressColorProperty, value);
        }

        public string DownloadProgressText
        {
            get
            {
                if (Maximum <= 0 || Value < 0.5f)
                {
                    return string.IsNullOrWhiteSpace(DownloadItemName)
                    ? "Preparing..."
                    : $"{DownloadItemName}: Preparing...";
                }
                var percent = Math.Min(100.0, Value / Maximum * 100.0);
                if (string.IsNullOrWhiteSpace(DownloadItemName))
                    return $"{percent:0.0}%";

                if (percent >= 100)
                    return string.IsNullOrWhiteSpace(DownloadItemName)
                        ? "Done!"
                        : $"{DownloadItemName}: Done!";

                return $"{DownloadItemName}: {percent:0.0}%";
            }
        }

        private double _actualWidth = 350;
        private double _lastIndicatorWidth;
        public double IndicatorWidth => (Maximum > 0) ? Math.Min(1.0, Value / Maximum) * _actualWidth : 0;
        public static readonly DirectProperty<CustomProgressBar, double> IndicatorWidthProperty =
            AvaloniaProperty.RegisterDirect<CustomProgressBar, double>(
                nameof(IndicatorWidth),
                o => o.IndicatorWidth);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == BoundsProperty)
            {
                _actualWidth = Bounds.Width;
                RaiseIndicatorWidthChanged();
            }
            else if (change.Property == ValueProperty || change.Property == MaximumProperty)
            {
                RaiseIndicatorWidthChanged();
            }
        }

        private void RaiseIndicatorWidthChanged()
        {
            double oldValue = _lastIndicatorWidth;
            double newValue = IndicatorWidth;
            _lastIndicatorWidth = newValue;
            RaisePropertyChanged(IndicatorWidthProperty, oldValue, newValue);
        }
    }
}
