using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using iPart.Core;

namespace iPart.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IDatabaseConnectionChecker? _checker;
    private readonly string? _configurationError;
    private string _status = "Nie sprawdzono połączenia";

    public MainWindowViewModel(IDatabaseConnectionChecker? checker, string? configurationError = null, ImportExcelViewModel? import = null)
    {
        _checker = checker;
        _configurationError = configurationError;
        Import = import;
        CheckConnectionCommand = new AsyncCommand(CheckConnectionAsync);
    }

    public ICommand CheckConnectionCommand { get; }
    public ImportExcelViewModel? Import { get; }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value)
                return;
            _status = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task CheckConnectionAsync()
    {
        if (_checker is null)
        {
            Status = _configurationError ?? "Błąd konfiguracji połączenia z bazą danych.";
            return;
        }

        Status = "Sprawdzanie połączenia…";
        try
        {
            Status = await _checker.CheckAsync() switch
            {
                DatabaseConnectionStatus.Connected => "Połączono z bazą dataPart",
                DatabaseConnectionStatus.TimedOut => "Przekroczono limit czasu połączenia (10 sekund). Sprawdź dostępność SQL Server i spróbuj ponownie.",
                _ => "Nie udało się połączyć z bazą. Sprawdź, czy SQL Server LocalDB działa, baza istnieje i masz do niej dostęp."
            };
        }
        catch (OperationCanceledException)
        {
            Status = "Sprawdzanie połączenia zostało anulowane.";
        }
        catch (Exception)
        {
            Status = "Wystąpił błąd podczas sprawdzania połączenia. Sprawdź konfigurację i dostępność SQL Server.";
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
