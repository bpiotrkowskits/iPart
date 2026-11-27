using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace iPart.App.Controls;

/// <summary>
/// Postęp procesu u góry ekranu: 1 Import BOM → 2 Import P&amp;P → 3 Comparator → 4 Integracja Danych → 5 Zatwierdzenie i wersje.
/// Kroki zakończone — zielone kółko z ✓, bieżący — zielony, następne — szare.
/// </summary>
public sealed class ProcessStepper : Border
{
    public static readonly string[] Steps = ["Import BOM", "Import P&P", "Comparator", "Integracja Danych", "Zatwierdzenie i wersje"];

    public static readonly DependencyProperty CurrentProperty = DependencyProperty.Register(
        nameof(Current), typeof(int), typeof(ProcessStepper), new FrameworkPropertyMetadata(1, (d, _) => ((ProcessStepper)d).Build()));

    private readonly Grid _root = new() { Margin = new Thickness(0, 4, 0, 8) };

    public ProcessStepper()
    {
        Child = _root;
        Loaded += (_, _) => Build();
        AutomationProperties_SetName();
    }

    public int Current { get => (int)GetValue(CurrentProperty); set => SetValue(CurrentProperty, value); }

    private void AutomationProperties_SetName() =>
        System.Windows.Automation.AutomationProperties.SetName(this, "Postęp procesu");

    private Brush Find(string key, Color fallback) => TryFindResource(key) as Brush ?? new SolidColorBrush(fallback);

    private void Build()
    {
        var accent = Find("Ui.Accent", Color.FromRgb(0x2E, 0x7D, 0x32));
        var light = Find("Ui.Accent.Light", Color.FromRgb(0xE3, 0xF2, 0xE1));
        var muted = Find("Ui.Muted", Color.FromRgb(0xB0, 0xB7, 0xC0));
        var text = Find("Ui.Text", Colors.Black);
        var secondary = Find("Ui.TextSecondary", Colors.Gray);
        _root.Children.Clear();
        _root.ColumnDefinitions.Clear();
        for (var i = 0; i < Steps.Length; i++)
        {
            if (i > 0) _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }
        for (var i = 0; i < Steps.Length; i++)
        {
            var number = i + 1;
            var done = number < Current;
            var current = number == Current;
            var circle = new Border
            {
                Width = 30, Height = 30, CornerRadius = new CornerRadius(15), BorderThickness = new Thickness(2),
                BorderBrush = done || current ? accent : muted,
                Background = done ? accent : current ? light : Brushes.Transparent,
                Child = new TextBlock
                {
                    Text = done ? "✓" : number.ToString(), FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center, Foreground = done ? Brushes.White : current ? accent : muted
                }
            };
            var label = new TextBlock
            {
                Text = Steps[i], Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center,
                FontWeight = current ? FontWeights.SemiBold : FontWeights.Normal, Foreground = done || current ? text : secondary
            };
            var step = new StackPanel { Orientation = Orientation.Horizontal, Children = { circle, label } };
            System.Windows.Automation.AutomationProperties.SetName(step, $"Krok {number}: {Steps[i]}{(current ? " (bieżący)" : done ? " (zakończony)" : "")}");
            Grid.SetColumn(step, i * 2);
            _root.Children.Add(step);
            if (i < Steps.Length - 1)
            {
                var line = new Rectangle { Height = 2, Fill = number < Current ? accent : muted, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
                Grid.SetColumn(line, i * 2 + 1);
                _root.Children.Add(line);
            }
        }
    }
}
