# iPart

Aplikacja WPF dla .NET 10 z bazą SQL Server, następca systemu plików Excel (iPart, sPart, cPart…). Wersja **0.1.0** jest wstępna i służy nauce publikacji oraz wpięcia bazy na serwer firmy, nie pracy produkcyjnej.

**Dokumentacja: [docs/README.md](docs/README.md).** Baza danych, uprawnienia i publikacja: [Środowiska](docs/system/07%20Środowiska.md).

## Zakładki wersji 0.1.0

- **Pakiety** — pakiety (kod + rewizja Klienta + Klient) i ich kolejne rewizje importu BOM z liniami.
- **Import BOM** — BOM Klienta z `.xlsx` z ręcznym mapowaniem kolumn: kilka kolumn do jednego pola z priorytetem, kolumny nieprzypisane zachowane jako dodatkowe, rozbijanie zakresów desygnatorów (`R1-R5`, `R01-R05`, `C2A-C2D`), stały numer linii, kontrola powtórzonych desygnatorów i zgodności z Quantity.
- **Komponenty** — przeglądanie bazy komponentów.
- **Połączenie z bazą** — serwer, baza, sposób logowania i zgodność schematu z wersją aplikacji.
- **Import dataPart (podgląd)** — podgląd importu dataPart, bez zapisu.

## Uruchomienie

Wymagane są Windows, SDK .NET 10 oraz baza `dataPart` na `(localdb)\MSSQLLocalDB` z zastosowanymi migracjami.

```powershell
dotnet build iPart.slnx
dotnet run --project src/iPart.App
```

Konfiguracja: `src/iPart.App/appsettings.json` (klucz `ConnectionStrings:IPartDatabase`). Aplikacja nie stosuje migracji — lokalnie:

```powershell
dotnet tool restore
dotnet ef database update --project src/iPart.Infrastructure --startup-project src/iPart.Infrastructure --context IPartDbContext -- --settings src/iPart.App/appsettings.json
```

## Testy

```powershell
dotnet test iPart.slnx
```

Testy SQL Server są domyślnie pomijane. Włączają je zmienne środowiskowe:

| Zmienna | Co włącza |
|---|---|
| `IPART_RUN_SQL_TESTS` | testy tabel `#temp`, odczytu stanu bazy i próbę migracji na tymczasowej bazie (`PackageDatabaseTests`) |
| `IPART_RUN_DATABASE_TESTS` | testy rzeczywistych tabel i zapisu w `dataPart` (dane wycofywane transakcyjnie) |
| `IPART_RUN_IMPORT_SQL_TESTS` | testy importera korzystające z SQL Server |
