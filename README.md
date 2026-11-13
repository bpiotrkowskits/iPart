# iPart

Aplikacja WPF dla .NET 10. Okno sprawdza dostępność istniejącej bazy SQL Server. Przygotowano model komponentów, reguły jednostek, serwis zapisu z historią i pierwszą migrację EF Core. Zakładka „Import Excel” odczytuje, mapuje i waliduje XLSX oraz pokazuje podgląd i eksport raportu; nie zapisuje importu do bazy. Formularz edycji komponentów nie jest jeszcze wdrożony.

## Uruchomienie

```powershell
dotnet build iPart.slnx
dotnet run --project src/iPart.App
```

Wymagane są Windows, SDK .NET 10 oraz istniejąca baza `dataPart` na `(localdb)\MSSQLLocalDB`, dostępna dla bieżącego użytkownika Windows.

Konfiguracja znajduje się w `src/iPart.App/appsettings.json`. Plik jest kopiowany do katalogu wynikowego i katalogu publikacji. Aplikacja odczytuje go względem `AppContext.BaseDirectory`, przy uruchomieniu. Po zmianie konfiguracji uruchom aplikację ponownie.

`TrustServerCertificate=True` służy wyłącznie lokalnemu środowisku deweloperskiemu. Przy konfiguracji innych środowisk należy wyłączyć tę opcję i użyć prawidłowo zaufanego certyfikatu serwera.

Sprawdzanie korzysta z `Database.CanConnectAsync`, limitu 10 sekund i osobnego, zwalnianego po próbie kontekstu. Nie tworzy bazy, tabel ani migracji i nie zmienia danych.

## Model, testy i migracja

Opis relacji, jednostek, precyzji, zapisu, historii i przyszłych rozszerzeń: [docs/data-model.md](docs/data-model.md).

Instrukcja zakładki importu, reguły mapowania, liczniki i eksport: [docs/excel-import.md](docs/excel-import.md). Excel nie musi być zainstalowany; odczyt wykorzystuje Open XML SDK 3.5.1 (MIT). Zastosowana migracja pozostaje bez zmian. Testy importera (bez wcześniejszych testów zapisujących SQL):

```powershell
$env:IPART_RUN_IMPORT_SQL_TESTS = '1'
dotnet test iPart.slnx --filter 'FullyQualifiedName!~SqlServer'
Remove-Item Env:IPART_RUN_IMPORT_SQL_TESTS
```

```powershell
dotnet tool restore
dotnet test iPart.slnx
```

Testy SQL Server są domyślnie pomijane. Aby uruchomić cały zestaw na lokalnej instancji:

```powershell
$env:IPART_RUN_SQL_TESTS = '1'
$env:IPART_RUN_DATABASE_TESTS = '1'
dotnet test iPart.slnx
Remove-Item Env:IPART_RUN_SQL_TESTS
Remove-Item Env:IPART_RUN_DATABASE_TESTS
```

`IPART_RUN_SQL_TESTS` włącza testy tabel `#temp` oraz odczytu stanu bazy. `IPART_RUN_DATABASE_TESTS` włącza testy rzeczywistych tabel i serwisu zapisu w `dataPart`, wymagające zastosowanej migracji. Dane tych testów są wycofywane transakcyjnie; po każdym teście porównywane są liczniki wierszy wszystkich tabel. Testy nie stosują migracji i nie pozostawiają komponentów ani historii testowej.

Migracja `20261113125347_InitialComponentModel` została zastosowana do lokalnej `dataPart`. WPF nigdy nie wykonuje migracji przy uruchomieniu. Polecenie ręcznej kontroli/zastosowania tej wersji schematu z katalogu repozytorium (dla aktualnej bazy nie wykona ponownie zastosowanej migracji):

```powershell
dotnet ef database update 20261113125347_InitialComponentModel --project src/iPart.Infrastructure --startup-project src/iPart.Infrastructure --context IPartDbContext -- --settings src/iPart.App/appsettings.json
```

To polecenie odczytuje połączenie do `dataPart` z konfiguracji aplikacji, z niezmienionego klucza `ConnectionStrings:IPartDatabase`. Lokalny manifest `dotnet-tools.json` przypina `dotnet-ef` do 10.0.15, zgodnie z pakietami EF Core.

## Lokalna baza dataPart

Podczas zmiany konfiguracji sprawdzono LocalDB: `iPart_Dev` nie zawierała tabel użytkownika ani tabeli historii migracji, a `dataPart` nie istniała. Utworzono pustą bazę `dataPart`. Starą `iPart_Dev` pozostawiono bez zmian; nie była to operacja zmiany nazwy istniejącej bazy ani przenoszenia danych. W kolejnym kroku, po przeglądzie SQL, zastosowano pierwszą migrację wyłącznie do `dataPart`.

Aplikacja nadal nazywa się iPart, nazwy projektów i klucz `IPartDatabase` pozostają bez zmian. Serwer to nadal `(localdb)\MSSQLLocalDB`. Przyszłe migracje korzystają z połączenia do `dataPart` wskazanego w `appsettings.json`.

Przejrzany skrypt: [artifacts/migrations/dataPart.sql](artifacts/migrations/dataPart.sql). Wyniki wdrożenia schematu: [artifacts/migrations/verification.md](artifacts/migrations/verification.md) (ówczesny zestaw 127/127 testów). Weryfikacja etapu importera: 137/137 odpowiednich testów, kompilacja bez ostrzeżeń i błędów, brak zmian modelu poza snapshotem.

## Ręczne sprawdzenie

1. Uruchom aplikację. Tytuł okna to „iPart”, a status początkowy to „Nie sprawdzono połączenia”.
2. Kliknij „Sprawdź połączenie z bazą”. Podczas próby przycisk jest nieaktywny, status to „Sprawdzanie połączenia…”, a okno pozostaje responsywne.
3. Dla dostępnej bazy pojawi się „Połączono z bazą dataPart”. Przycisk umożliwia kolejne sprawdzenie.
4. Aby sprawdzić błąd połączenia, zamknij aplikację, tymczasowo zmień nazwę bazy w wynikowym `appsettings.json` na nieistniejącą i uruchom plik wykonywalny z tego katalogu. Próba powinna pokazać polski komunikat o nieudanym połączeniu.
5. Aby sprawdzić konfigurację, zamknij aplikację i tymczasowo zmień nazwę wynikowego `appsettings.json`, usuń wpis `IPartDatabase` lub uszkodź składnię JSON. Po uruchomieniu i kliknięciu przycisku powinien pojawić się komunikat o błędzie konfiguracji.
6. Limit czasu można sprawdzić, podając w wynikowej konfiguracji niedostępny adres SQL Server, który nie odrzuca połączenia natychmiast. Po upływie limitu pojawi się komunikat o przekroczeniu czasu. Natychmiastowe odrzucenie połączenia może dać zwykły komunikat o niedostępności.
7. Przywróć poprawną konfigurację i uruchom aplikację ponownie.
