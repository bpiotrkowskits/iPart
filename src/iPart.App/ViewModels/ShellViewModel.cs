using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace iPart.App.ViewModels;

/// <summary>Pozycja menu: nazwa i strona (ViewModel), którą pokazuje.</summary>
public sealed record NavItem(string Title, object Page);

/// <summary>
/// Powłoka aplikacji: nagłówek, menu i bieżąca strona (nawigacja ViewModel-first — widok wynika z typu strony).
/// Bez logiki biznesowej.
/// </summary>
public sealed partial class ShellViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    [ObservableProperty] public partial object? CurrentPage { get; set; }
    [ObservableProperty] public partial NavItem? SelectedNav { get; set; }
    [ObservableProperty] public partial string AccountName { get; set; } = "";
    [ObservableProperty] public partial bool IsSignedIn { get; set; }

    public ObservableCollection<NavItem> NavItems { get; } = [];

    public string AppVersion { get; } = "iPart " +
        (typeof(ShellViewModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "?")
        + " — pilotaż";

    partial void OnSelectedNavChanged(NavItem? value)
    {
        if (value is not null) CurrentPage = value.Page;
    }

    /// <summary>Po wyborze konta: menu i pierwsza strona.</summary>
    public void SignedIn(string accountName, IEnumerable<NavItem> items)
    {
        AccountName = accountName;
        IsSignedIn = true;
        NavItems.Clear();
        foreach (var item in items) NavItems.Add(item);
        SelectedNav = NavItems.FirstOrDefault();
    }

    /// <summary>Strona spoza menu (np. BOM wybranego pakietu); menu zostaje na swojej pozycji.</summary>
    public void Show(object page) => CurrentPage = page;
}
