using System.Collections.ObjectModel;
using iPart.Core.Components;
using iPart.Core.Packages;

namespace iPart.App.ViewModels;

public sealed record CustomerOption(Guid? Id, string Name)
{
    public override string ToString() => Name;
}

public sealed class PackagesViewModel : ObservableObject
{
    private readonly IPackageService _service;
    private string _search = "", _status = "Kliknij „Odśwież”, aby wczytać pakiety z bazy.";
    private string _newCode = "", _newRevision = "", _newDescription = "", _newCustomerName = "";
    private PackageSummary? _selectedPackage;
    private CustomerOption? _selectedCustomer;
    private bool _isBusy;

    public PackagesViewModel(IPackageService service)
    {
        _service = service;
        RefreshCommand = new AsyncCommand(RefreshAsync);
        CreatePackageCommand = new AsyncCommand(CreatePackageAsync);
        AddCustomerCommand = new AsyncCommand(AddCustomerAsync);
        OpenCommand = new AsyncCommand(() => SelectedPackage is { } p && Open is not null ? Open(p, null) : Task.CompletedTask);
    }

    /// <summary>Nawigacja do BOM pakietu (ustawia powłoka): pakiet i opcjonalnie rewizja importu do pokazania.</summary>
    public Func<PackageSummary, Guid?, Task>? Open { get; set; }
    public AsyncCommand OpenCommand { get; }

    public ObservableCollection<PackageSummary> Packages { get; } = [];
    public ObservableCollection<CustomerOption> Customers { get; } = [];
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand CreatePackageCommand { get; }
    public AsyncCommand AddCustomerCommand { get; }

    public string Search { get => _search; set => Set(ref _search, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public bool IsBusy { get => _isBusy; private set => Set(ref _isBusy, value); }
    public string NewCode { get => _newCode; set => Set(ref _newCode, value); }
    public string NewRevision { get => _newRevision; set => Set(ref _newRevision, value); }
    public string NewDescription { get => _newDescription; set => Set(ref _newDescription, value); }
    public string NewCustomerName { get => _newCustomerName; set => Set(ref _newCustomerName, value); }
    public CustomerOption? SelectedCustomer { get => _selectedCustomer; set => Set(ref _selectedCustomer, value); }

    public PackageSummary? SelectedPackage { get => _selectedPackage; set => Set(ref _selectedPackage, value); }

    public async Task RefreshAsync()
    {
        await Run(async () =>
        {
            await RefreshCoreAsync(SelectedPackage?.Id);
            Status = $"Pakietów w bazie: {Packages.Count}.";
        });
    }

    private async Task CreatePackageAsync()
    {
        await Run(async () =>
        {
            var id = await _service.CreatePackageAsync(new NewPackageRequest(NewCode, NewRevision, SelectedCustomer?.Id, NewDescription));
            var created = $"{NewCode.Trim()} rev {NewRevision.Trim()}";
            NewCode = NewRevision = NewDescription = "";
            await RefreshCoreAsync(id);
            Status = $"Utworzono pakiet {created}. Teraz zaimportuj jego BOM (menu „Import BOM”).";
        });
    }

    private async Task AddCustomerAsync()
    {
        await Run(async () =>
        {
            var id = await _service.AddCustomerAsync(NewCustomerName);
            var name = NewCustomerName.Trim();
            NewCustomerName = "";
            await RefreshCoreAsync(SelectedPackage?.Id);
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == id);
            Status = $"Dodano Klienta „{name}”.";
        });
    }

    // Po imporcie BOM: odśwież listę i otwórz BOM pakietu na nowej rewizji importu.
    public async Task ShowRevisionAsync(Guid packageId, Guid revisionId)
    {
        await RefreshCoreAsync(packageId);
        if (SelectedPackage is { } package && Open is not null) await Open(package, revisionId);
    }

    private async Task RefreshCoreAsync(Guid? select)
    {
        var packages = await _service.ListPackagesAsync(Search);
        Packages.Clear();
        foreach (var p in packages) Packages.Add(p);
        var customers = await _service.ListCustomersAsync();
        var selectedCustomer = SelectedCustomer?.Id; // przed Clear — lista rozwijana wyzeruje zaznaczenie
        Customers.Clear();
        Customers.Add(new CustomerOption(null, "(bez Klienta)"));
        foreach (var c in customers) Customers.Add(new CustomerOption(c.Id, c.Name));
        SelectedCustomer = Customers.FirstOrDefault(c => c.Id == selectedCustomer) ?? Customers[0];
        _selectedPackage = null;
        SelectedPackage = Packages.FirstOrDefault(p => p.Id == select);
    }

    private async Task Run(Func<Task> action)
    {
        IsBusy = true;
        try { await action(); }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
        finally { IsBusy = false; }
    }
}
