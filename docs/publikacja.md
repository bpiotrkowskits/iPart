# Publikacja iPart i baza danych na serwerze — instrukcja na spotkanie z IT

Wersja aplikacji: **0.1.0** (wstępna, do nauki publikacji — nie do pracy produkcyjnej).

Cel spotkania: przejść razem cały proces, który będzie się powtarzał przy każdej kolejnej wersji:

1. baza danych na serwerze SQL firmy,
2. uprawnienia dla użytkowników,
3. wdrożenie schematu bazy (tabel),
4. publikacja aplikacji i udostępnienie jej w firmie,
5. aktualizacja do nowej wersji.

## 1. Jak to jest zbudowane

```text
 Komputer użytkownika                         Serwer SQL firmy
┌──────────────────────────┐               ┌─────────────────────────┐
│ iPart.App.exe (WPF)      │   połączenie  │ SQL Server              │
│ + appsettings.json       │ ────────────► │  └─ baza (np. iPart)    │
│   (adres serwera i bazy) │  konto Windows│     tabele, dane        │
└──────────────────────────┘               └─────────────────────────┘
```

- **Aplikacja** to program okienkowy Windows (WPF, .NET 10). Każdy użytkownik uruchamia ją u siebie.
- **Baza danych** jest jedna, na serwerze — wszyscy pracują na tych samych danych. To jest właśnie „jedno wspólne źródło danych” z wywiadu.
- Aplikacja łączy się z bazą **kontem Windows zalogowanego użytkownika** — bez haseł w plikach.
- Aplikacja **nigdy sama nie zmienia struktury bazy**. Robi to administrator gotowym skryptem SQL. Dzięki temu zwykły użytkownik nie potrzebuje uprawnień administracyjnych.

## 2. Słowniczek

| Pojęcie | Znaczenie |
|---|---|
| SQL Server / instancja | Program bazodanowy na serwerze. Na jednym serwerze może działać kilka instancji, np. `SERWER\SQL2022`. |
| Baza danych | Zbiór tabel jednej aplikacji, np. `iPart`. Na jednej instancji może być wiele baz. |
| Login | Konto, które może połączyć się z serwerem SQL (np. grupa Windows z Active Directory). |
| Użytkownik bazy | Powiązanie loginu z konkretną bazą i nadane mu role (co może czytać/zapisywać). |
| Uwierzytelnianie Windows | Logowanie do SQL kontem Windows — bez osobnego hasła. W connection string: `Integrated Security=True`. |
| Connection string | Jedna linijka tekstu z adresem serwera, nazwą bazy i sposobem logowania. W pliku `appsettings.json`. |
| Schemat | Struktura bazy: tabele, kolumny, ograniczenia, indeksy. |
| Migracja | Opis jednej zmiany schematu, zapisany w kodzie (folder `src/iPart.Infrastructure/Migrations`). Baza pamięta, które migracje ma zastosowane (tabela `__EFMigrationsHistory`). |
| Skrypt idempotentny | Skrypt SQL, który można bezpiecznie uruchomić kilka razy — sam sprawdza, co już zostało zrobione. |
| Publikacja | Przygotowanie gotowego folderu z programem do rozdania użytkownikom (`dotnet publish`). |
| Self-contained | Folder zawiera też środowisko .NET — na komputerach nie trzeba go instalować. |

## 3. Krok 1 — baza danych na serwerze (z IT)

### 3.1 Pytania do IT na początek

- Na którym serwerze i instancji SQL Server ma być baza? Jaka wersja SQL Server (wymagana 2016 lub nowsza)?
- Czy serwer ma zaufany certyfikat TLS (szyfrowane połączenie)? Od tego zależy `TrustServerCertificate`.
- Czy komputery użytkowników mają dostęp sieciowy do serwera SQL (zapora, port — domyślnie 1433)?
- Jaka grupa Active Directory ma dostać dostęp do aplikacji? Czy tworzymy nową, np. `iPart-Uzytkownicy`?
- Jak wygląda kopia zapasowa baz w firmie i czy obejmie nową bazę?
- Gdzie trzymamy aplikację: udział sieciowy, ClickOnce, system dystrybucji oprogramowania (np. Intune/SCCM)?

### 3.2 Utworzenie pustej bazy

Wykonuje administrator, np. w SQL Server Management Studio (SSMS). Nazwę bazy ustalcie razem (poniżej `iPart`).

```sql
CREATE DATABASE [iPart];
```

Lokalna baza `dataPart` z Twojego komputera **nie jest przenoszona** — zawiera tylko słowniki początkowe, które skrypt schematu i tak tworzy. Pakiety testowe zrobione lokalnie zostają lokalnie.

### 3.3 Uprawnienia

Zasada: **użytkownicy aplikacji mogą czytać i zapisywać dane, ale nie mogą zmieniać struktury bazy**. Strukturę zmienia tylko administrator.

```sql
-- Grupa Windows z Active Directory (nazwa domeny i grupy — od IT)
CREATE LOGIN [DOMENA\iPart-Uzytkownicy] FROM WINDOWS;

USE [iPart];
CREATE USER [DOMENA\iPart-Uzytkownicy] FOR LOGIN [DOMENA\iPart-Uzytkownicy];
ALTER ROLE db_datareader ADD MEMBER [DOMENA\iPart-Uzytkownicy];
ALTER ROLE db_datawriter ADD MEMBER [DOMENA\iPart-Uzytkownicy];
```

Dlaczego grupa, a nie pojedyncze osoby: nowego pracownika IT dodaje do grupy w AD i niczego nie trzeba zmieniać w SQL.

### 3.4 Wdrożenie schematu (tabel)

Plik: [`deploy/sql/iPart-schemat.sql`](../deploy/sql/iPart-schemat.sql) — tworzy wszystkie tabele obu migracji (25 tabel łącznie z historią migracji) i słowniki początkowe (sposoby montażu, statusy, kwalifikacje AEC).

1. Otwórz plik w SSMS, połączony kontem administratora.
2. Upewnij się, że na górze wybrana jest baza `iPart` (nie `master`!).
3. Uruchom (F5). Skrypt jest idempotentny — drugie uruchomienie nic nie zepsuje.
4. Sprawdź:

```sql
SELECT MigrationId FROM [iPart].dbo.__EFMigrationsHistory;
-- oczekiwane: 20261113125347_InitialComponentModel oraz ..._AddPackagesAndBom
```

Alternatywa dla osób z zainstalowanym SDK .NET (zamiast SSMS) — to samo polecenie, którym zastosowano migrację lokalnie, z jawnym adresem serwera:

```powershell
dotnet tool restore
dotnet ef database update --project src/iPart.Infrastructure --startup-project src/iPart.Infrastructure --context IPartDbContext --connection "Server=SERWER\INSTANCJA;Database=iPart;Integrated Security=True;Encrypt=True;TrustServerCertificate=False" -- --settings src/iPart.App/appsettings.json
```

(`--connection` zastępuje adres z `appsettings.json`; sam plik jest potrzebny, bo bez niego narzędzie nie wystartuje.)

Dla bazy, która ma już pierwszą migrację (np. lokalna `dataPart`), jest osobny, mniejszy skrypt aktualizacji: [`deploy/sql/aktualizacja-0.1.0-pakiety.sql`](../deploy/sql/aktualizacja-0.1.0-pakiety.sql) (dodaje 5 tabel pakietów i BOM, niczego nie usuwa).

## 4. Krok 2 — connection string

W opublikowanym folderze jest plik `appsettings.json`. Trzeba w nim zmienić jedną linijkę:

```json
{
  "ConnectionStrings": {
    "IPartDatabase": "Server=SERWER\\INSTANCJA;Database=iPart;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;Connect Timeout=10"
  }
}
```

| Fragment | Znaczenie |
|---|---|
| `Server=SERWER\\INSTANCJA` | Adres serwera SQL. W JSON ukośnik wsteczny pisze się podwójnie. Domyślna instancja: samo `SERWER`. |
| `Database=iPart` | Nazwa bazy. |
| `Integrated Security=True` | Logowanie kontem Windows — bez hasła w pliku. |
| `Encrypt=True` | Szyfrowane połączenie. |
| `TrustServerCertificate=False` | Sprawdzaj certyfikat serwera. `True` wyłącznie w testach albo gdy IT świadomie na to pozwoli — inaczej połączenie można podsłuchać. |
| `Connect Timeout=10` | Po 10 sekundach bez odpowiedzi aplikacja pokaże błąd zamiast „wisieć”. |

Plik nie zawiera haseł, więc może leżeć na udziale sieciowym razem z programem.

## 5. Krok 3 — publikacja aplikacji

Na komputerze z kodem (Twoim), w katalogu repozytorium:

```powershell
dotnet publish src/iPart.App -p:PublishProfile=Firma
```

Wynik: folder `publish\iPart\` (ok. 170 MB, z wbudowanym .NET). Ustawienia profilu: [`src/iPart.App/Properties/PublishProfiles/Firma.pubxml`](../src/iPart.App/Properties/PublishProfiles/Firma.pubxml).

Następnie:

1. W `publish\iPart\appsettings.json` wpisz connection string z kroku 2.
2. Skopiuj folder na udział sieciowy do **folderu z numerem wersji**, np. `\\serwer\Aplikacje\iPart\0.1.0\`.
3. Utwórz dla użytkowników skrót do `iPart.App.exe` z tego folderu (np. przez zasady grupy albo ręcznie na pulpit).

Uwagi do omówienia z IT:

- **Uruchamianie z udziału** jest najprostsze, ale pliku `.exe`, który ktoś ma otwarty, nie da się nadpisać. Dlatego każda wersja w osobnym folderze, a skrót przestawia się na nową wersję.
- **ClickOnce** (wbudowany w .NET) daje automatyczne aktualizacje przy starcie — warto rozważyć przy kolejnych wersjach.
- **System dystrybucji IT** (Intune, SCCM itp.) — jeśli firma go używa, IT może instalować aplikację na komputerach.
- **Windows SmartScreen / blokady aplikacji** mogą ostrzegać przed niepodpisanym programem. Podpis kodu (certyfikat firmowy) — pytanie do IT.

## 6. Krok 4 — sprawdzenie po wdrożeniu

Na komputerze użytkownika (najlepiej konto zwykłego użytkownika, nie administratora):

1. Uruchom iPart, zakładka **„Połączenie z bazą”** → „Sprawdź połączenie z bazą”.
2. Oczekiwane: serwer i baza z firmy, logowanie „Windows”, 2 zastosowane migracje, „Schemat bazy jest zgodny z tą wersją aplikacji”.
3. Zakładka **„Pakiety”** → utwórz pakiet testowy (np. `TEST1` rev `A`).
4. Zakładka **„Import BOM”** → wybierz pakiet, plik w układzie BOM_P&P, arkusz `BOM`, „Podpowiedz po nazwach nagłówków”, „Podgląd”, „Zapisz jako nową rewizję importu”.
5. Na drugim komputerze sprawdź, że widać ten sam pakiet — to dowód, że baza jest wspólna.

## 7. Kolejne wersje — stały cykl

1. Zmiana w kodzie, testy (`dotnet test iPart.slnx`).
2. Podnieś numer wersji w `src/iPart.App/iPart.App.csproj` (`<Version>`).
3. Jeśli zmienił się model danych — nowa migracja i skrypt aktualizacji:

   ```powershell
   dotnet ef migrations add NazwaZmiany --project src/iPart.Infrastructure --startup-project src/iPart.Infrastructure --context IPartDbContext -- --settings src/iPart.App/appsettings.json
   dotnet ef migrations script <poprzednia_migracja> NazwaZmiany --idempotent -o deploy/sql/aktualizacja-<wersja>.sql --project src/iPart.Infrastructure --startup-project src/iPart.Infrastructure --context IPartDbContext -- --settings src/iPart.App/appsettings.json
   ```

4. **Najpierw baza, potem aplikacja:** administrator uruchamia skrypt aktualizacji, dopiero potem rozdajemy nową wersję. Zmiany schematu planujemy tak, żeby tylko dodawały (nowe tabele i kolumny) — wtedy stara wersja aplikacji dalej działa na nowej bazie.
5. `dotnet publish …`, nowy folder z numerem wersji, przestawienie skrótu.

Przed wdrożeniem schematu na serwer warto zrobić próbę generalną: test `PackageDatabaseTests` tworzy na lokalnym serwerze pustą tymczasową bazę, stosuje wszystkie migracje, zapisuje pakiet z BOM i usuwa bazę:

```powershell
$env:IPART_RUN_SQL_TESTS = '1'
dotnet test iPart.slnx --filter FullyQualifiedName~PackageDatabaseTests
Remove-Item Env:IPART_RUN_SQL_TESTS
```

## 8. Najczęstsze problemy

| Komunikat | Przyczyna | Co zrobić |
|---|---|---|
| „Brak dostępu do bazy dla Twojego konta Windows” (SQL 18456 / 4060) | Konto nie jest w grupie z dostępem albo brak użytkownika w bazie | IT: dodać do grupy, sprawdzić `CREATE USER` |
| Przekroczono limit czasu połączenia | Zła nazwa serwera, zapora, serwer niedostępny | Sprawdzić `Server=`, port 1433, VPN |
| Błąd certyfikatu („certificate chain… not trusted”) | Serwer nie ma zaufanego certyfikatu | IT: certyfikat; tymczasowo w testach `TrustServerCertificate=True` |
| „W bazie brakuje tabel tej wersji aplikacji” | Nie uruchomiono skryptu schematu / aktualizacji | Administrator: skrypt z `deploy/sql` |
| „Błąd konfiguracji: brak ConnectionStrings…” | Zły lub brakujący `appsettings.json` obok programu | Sprawdzić plik i składnię JSON (przecinki, podwójne `\\`) |

## 9. Ograniczenia wersji 0.1.0 — powiedz o nich IT

- Brak ról w aplikacji: każdy z dostępem do bazy może tworzyć pakiety i importować BOM. Role (np. ceny tylko dla zakupów) — w kolejnych wersjach.
- Rola `db_datawriter` pozwala zmieniać dane także poza aplikacją (np. z SSMS). Docelowo można ograniczyć dostęp do procedur albo przejść na aplikację webową.
- Brak cen i danych wrażliwych — można bezpiecznie testować.
- Import obsługuje tylko `.xlsx` (bez `.xls`, `.csv`, P&P `.txt`).

**Do rozmowy z IT o kierunku na przyszłość:** obecnie każdy komputer łączy się bezpośrednio z bazą. Alternatywą jest aplikacja webowa (np. ASP.NET Core) na serwerze — wtedy z bazą łączy się tylko serwer aplikacji, aktualizacja to jedna instalacja, a uprawnienia (np. ceny tylko dla zakupów) pilnuje serwer. Warto zapytać IT, jaki model woli utrzymywać.

## 10. Lista kontrolna na jutro

- [ ] Ustalona nazwa serwera\instancji i bazy
- [ ] Baza utworzona
- [ ] Grupa AD i uprawnienia `db_datareader` + `db_datawriter`
- [ ] Skrypt `iPart-schemat.sql` uruchomiony, 2 migracje w `__EFMigrationsHistory`
- [ ] Decyzja o certyfikacie (`TrustServerCertificate`)
- [ ] `dotnet publish` z profilem Firma
- [ ] `appsettings.json` z adresem serwera
- [ ] Folder na udziale z numerem wersji + skrót
- [ ] Test na komputerze zwykłego użytkownika (zakładka Połączenie, pakiet testowy)
- [ ] Test z drugiego komputera (wspólne dane)
- [ ] Ustalony plan kopii zapasowych bazy
- [ ] Ustalony sposób dystrybucji kolejnych wersji
