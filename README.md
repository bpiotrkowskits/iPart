# iPart

Aplikacja WPF dla .NET 10. Na obecnym etapie sprawdza dostępność istniejącej bazy SQL Server.

## Uruchomienie

```powershell
dotnet build iPart.slnx
dotnet run --project src/iPart.App
```

Wymagane są Windows, SDK .NET 10 oraz istniejąca baza `iPart_Dev` na `(localdb)\MSSQLLocalDB`, dostępna dla bieżącego użytkownika Windows.

Konfiguracja znajduje się w `src/iPart.App/appsettings.json`. Plik jest kopiowany do katalogu wynikowego i katalogu publikacji. Aplikacja odczytuje go względem `AppContext.BaseDirectory`, przy uruchomieniu. Po zmianie konfiguracji uruchom aplikację ponownie.

`TrustServerCertificate=True` służy wyłącznie lokalnemu środowisku deweloperskiemu. Przy konfiguracji innych środowisk należy wyłączyć tę opcję i użyć prawidłowo zaufanego certyfikatu serwera.

Sprawdzanie korzysta z `Database.CanConnectAsync`, limitu 10 sekund i osobnego, zwalnianego po próbie kontekstu. Nie tworzy bazy, tabel ani migracji i nie zmienia danych.

## Ręczne sprawdzenie

1. Uruchom aplikację. Tytuł okna to „iPart”, a status początkowy to „Nie sprawdzono połączenia”.
2. Kliknij „Sprawdź połączenie z bazą”. Podczas próby przycisk jest nieaktywny, status to „Sprawdzanie połączenia…”, a okno pozostaje responsywne.
3. Dla dostępnej bazy pojawi się „Połączono z bazą iPart_Dev”. Przycisk umożliwia kolejne sprawdzenie.
4. Aby sprawdzić błąd połączenia, zamknij aplikację, tymczasowo zmień nazwę bazy w wynikowym `appsettings.json` na nieistniejącą i uruchom plik wykonywalny z tego katalogu. Próba powinna pokazać polski komunikat o nieudanym połączeniu.
5. Aby sprawdzić konfigurację, zamknij aplikację i tymczasowo zmień nazwę wynikowego `appsettings.json`, usuń wpis `IPartDatabase` lub uszkodź składnię JSON. Po uruchomieniu i kliknięciu przycisku powinien pojawić się komunikat o błędzie konfiguracji.
6. Limit czasu można sprawdzić, podając w wynikowej konfiguracji niedostępny adres SQL Server, który nie odrzuca połączenia natychmiast. Po upływie limitu pojawi się komunikat o przekroczeniu czasu. Natychmiastowe odrzucenie połączenia może dać zwykły komunikat o niedostępności.
7. Przywróć poprawną konfigurację i uruchom aplikację ponownie.
