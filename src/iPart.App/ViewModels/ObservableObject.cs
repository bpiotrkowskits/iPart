using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using iPart.Core.Packages;
using Microsoft.Data.SqlClient;

namespace iPart.App.ViewModels;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// Zamiana wyjątków na zrozumiałe komunikaty dla użytkownika.
public static class ErrorMessages
{
    public static string Describe(Exception exception) => exception switch
    {
        PackageOperationException e => e.Message,
        SqlException { Number: 208 } => "W bazie brakuje tabel tej wersji aplikacji. Schemat bazy nie jest aktualny — zobacz zakładkę „Połączenie z bazą”.",
        SqlException { Number: 4060 or 18456 } => "Brak dostępu do bazy dla Twojego konta Windows. Zgłoś się do administratora bazy.",
        SqlException => "Błąd SQL Server: " + exception.Message,
        IOException => "Nie można odczytać pliku. Zamknij go w Excelu albo sprawdź uprawnienia. (" + exception.Message + ")",
        InvalidDataException => "Plik nie jest poprawnym skoroszytem .xlsx. (" + exception.Message + ")",
        OperationCanceledException => "Operacja anulowana.",
        _ => "Nieoczekiwany błąd: " + exception.Message
    };
}
