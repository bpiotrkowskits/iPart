using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace iPart.App.Controls;

/// <summary>
/// Okno pytania w wyglądzie aplikacji (karta Material, zielone przyciski) — zamiast systemowego MessageBox.
/// </summary>
public static class AppDialog
{
    /// <summary>Pytanie Tak/Nie; domyślnie (Esc, zamknięcie) — Nie.</summary>
    public static bool Confirm(string message, string yes = "Tak, wróć", string no = "Anuluj") => Show(message, yes, no);

    /// <summary>Komunikat z jednym przyciskiem.</summary>
    public static void Inform(string message) => Show(message, "OK", null);

    private static bool Show(string message, string yes, string? no)
    {
        var window = new Window
        {
            WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = Brushes.Transparent, ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight, ShowInTaskbar = false, Title = "iPart",
            WindowStartupLocation = Application.Current?.MainWindow is { IsVisible: true } ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
        };
        if (Application.Current?.MainWindow is { IsVisible: true } owner) window.Owner = owner;
        window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/iPart.App;component/Resources/Theme.xaml") });

        var result = false;
        var yesButton = new Button { Content = yes, IsDefault = no is null, MinWidth = 110, Margin = new Thickness(0) };
        yesButton.Click += (_, _) => { result = true; window.Close(); };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        if (no is not null)
        {
            var noButton = new Button { Content = no, IsCancel = true, IsDefault = true, MinWidth = 110, Margin = new Thickness(0, 0, 8, 0) };
            noButton.SetResourceReference(FrameworkElement.StyleProperty, "MaterialDesignOutlinedButton");
            noButton.Click += (_, _) => window.Close();
            buttons.Children.Add(noButton);
        }
        else yesButton.IsCancel = true;
        buttons.Children.Add(yesButton);

        var icon = new TextBlock { Text = no is null ? "ℹ" : "!", FontSize = 20, FontWeight = FontWeights.Bold, Width = 36, Height = 36, TextAlignment = TextAlignment.Center, Padding = new Thickness(0, 4, 0, 0) };
        icon.SetResourceReference(TextBlock.ForegroundProperty, "Ui.Accent");
        var iconBox = new Border { CornerRadius = new CornerRadius(18), Child = icon, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Top };
        iconBox.SetResourceReference(Border.BackgroundProperty, "Ui.Accent.Light");
        var text = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 420, FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
        text.SetResourceReference(TextBlock.ForegroundProperty, "Ui.Text");
        var body = new DockPanel();
        DockPanel.SetDock(iconBox, Dock.Left);
        body.Children.Add(iconBox);
        body.Children.Add(text);

        var card = new Border
        {
            CornerRadius = new CornerRadius(12), Padding = new Thickness(24), Margin = new Thickness(16), BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 2, Opacity = 0.25 },
            Child = new StackPanel { Children = { body, buttons } }
        };
        card.SetResourceReference(Border.BackgroundProperty, "Ui.Surface");
        card.SetResourceReference(Border.BorderBrushProperty, "Ui.Border");
        window.Content = card;
        window.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) window.DragMove(); };
        window.ShowDialog();
        return result;
    }
}
