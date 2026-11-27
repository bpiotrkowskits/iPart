using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iPart.Core.Shared;

namespace iPart.App.ViewModels;

public sealed record AccountItem(ProduManAccount Account, string Name, string Login, string DepartmentText);

/// <summary>
/// Wybór konta przy starcie (pilotaż, ADR-0014): lista aktywnych kont z kopii listy kont ProduMana, bez hasła.
/// To nie jest zabezpieczenie — tożsamość i uprawnienia w pilotażu są umowne.
/// </summary>
public sealed partial class AccountSelectionViewModel(IAccountDirectory directory, Action<ProduManAccount> signIn) : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private IReadOnlyList<AccountItem> _all = [];

    [ObservableProperty] public partial string Search { get; set; } = "";
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ContinueCommand))] public partial AccountItem? Selected { get; set; }
    [ObservableProperty] public partial string Status { get; set; } = "Wczytywanie listy kont…";

    public ObservableCollection<AccountItem> Accounts { get; } = [];

    partial void OnSearchChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            var departments = await directory.DepartmentsAsync();
            _all = (await directory.ListSelectableAsync())
                .Select(a => new AccountItem(a, a.FullName ?? a.Login ?? a.UserId.ToString(), a.Login ?? "", Department.Describe(a.Department, departments)))
                .ToList();
            ApplyFilter();
            Status = _all.Count == 0
                ? "Lista kont jest pusta. Kopię listy kont ProduMana wgrywa developer (pilotaż)."
                : "Wybierz swoje konto. W pilotażu nie ma hasła — wybieraj tylko własne konto.";
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
    }

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        if (Selected is { } item) signIn(item.Account);
    }

    private bool CanContinue() => Selected is not null;

    private void ApplyFilter()
    {
        var text = Search.Trim();
        Accounts.Clear();
        foreach (var a in _all.Where(a => text.Length == 0
                     || a.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                     || a.Login.Contains(text, StringComparison.CurrentCultureIgnoreCase)))
            Accounts.Add(a);
    }
}
