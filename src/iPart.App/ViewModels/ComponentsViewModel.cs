using System.Collections.ObjectModel;
using iPart.Core.Packages;

namespace iPart.App.ViewModels;

public sealed class ComponentsViewModel : ObservableObject
{
    private const int Take = 500;
    private readonly IComponentBrowser _browser;
    private string _search = "", _status = "Wpisz fragment MPN i kliknij „Szukaj” (puste pole = wszystkie).";

    public ComponentsViewModel(IComponentBrowser browser)
    {
        _browser = browser;
        SearchCommand = new AsyncCommand(SearchAsync);
    }

    public ObservableCollection<ComponentListItem> Items { get; } = [];
    public AsyncCommand SearchCommand { get; }
    public string Search { get => _search; set => Set(ref _search, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }

    private async Task SearchAsync()
    {
        Status = "Wyszukiwanie…";
        try
        {
            var (items, total) = await _browser.SearchAsync(Search, Take);
            Items.Clear();
            foreach (var item in items) Items.Add(item);
            Status = total == 0 ? "Brak komponentów spełniających warunek (baza może być jeszcze pusta)."
                : $"Pokazano {items.Count} z {total} komponentów.";
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
    }
}
