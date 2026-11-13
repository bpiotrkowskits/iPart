# Model komponentów iPart

## Granice odpowiedzialności

- `iPart.Core`: agregat komponentu, słowniki, typowane parametry, walidacja, parser, konwersja i formatowanie oraz interfejsy serwisów. Brak zależności od EF Core.
- `iPart.Infrastructure`: mapowanie SQL Server, krótkotrwałe konteksty, migracje, zapis transakcyjny, historia i lokalny dostawca autora.
- `iPart.App`: istniejące WPF/MVVM i konfiguracja JSON. Dotychczasowy interfejs sprawdzania połączenia pozostaje bez zmian.

Nie zaimplementowano importera, formularza komponentów, ekranu historii ani funkcji magazynowych. Plik Excel nie jest odczytywany. Nie dodano przykładowych komponentów do bazy.

## Identyfikacja i normalizacja

`Component.Id` jest technicznym GUID, niezależnym od oznaczeń klienta i przyszłego symbolu magazynowego. `Version` jest niezależnym GUID używanym jako token współbieżności, a nie identyfikatorem komponentu.

Klucz biznesowy to `(Mpn, ManufacturerId, CustomerId)`. MPN jest wymaganym `nvarchar(200)`; limit liczony jest w jednostkach UTF-16, tak samo jak `string.Length` i długość SQL `nvarchar`. Setter usuwa wyłącznie białe znaki na krawędziach przez `Trim()`. Zera początkowe, znaki wewnętrzne i wielkość liter pozostają niezmienione. Aplikacja porównuje MPN przez `StringComparer.Ordinal`, a kolumna SQL stosuje `Latin1_General_100_BIN2`, bez normalizacji Unicode. `é` i `e` z osobnym znakiem akcentu są różnymi MPN.

SQL CHECK zabrania pustego MPN oraz wszystkich znaków z zestawu `char.IsWhiteSpace` na początku/końcu. Bez tego SQL mógłby traktować końcowe spacje jako nieistotne w porównaniu. Bezpośredni SQL ma odrzucać nieznormalizowane dane, a nie zmieniać je po cichu.

Unikalny indeks `UX_Components_BusinessKey` ma jawne `HasFilter(null)`. Nie zawiera domyślnego dla EF SQL Server filtra `IS NOT NULL`. Dlatego wszystkie kombinacje podlegają unikalności, także:

| MPN | Producent | Klient | Drugi identyczny wpis |
|---|---|---|---|
| X | NULL | NULL | konflikt |
| X | 1 | NULL | konflikt |
| X | NULL | 1 | konflikt |
| X | 1 | 1 | konflikt |

Zmiana producenta lub klienta daje inny klucz. `NULL` jest konkretnym brakiem informacji, a nie symbolem dopasowania do dowolnej wartości. Nie ma rekordów „Nieznany”, automatycznego scalania ani identyfikacji według zaokrąglonego Value. Serwis wykonuje wstępną kontrolę klucza, a naruszenia unikalności SQL (2601/2627), także w wyścigu zapisów, zwraca jako czytelny konflikt.

## Relacje i dane wspólne

`Components` zawiera MPN, opcjonalne nazwy PL/ENG, nazwę dystrybutora, nazwę komponentu według klienta, obudowę (`CaseName`), footprint, MSL, RoHS, temperatury pracy, uwagi, Critical, dodatkowy opis i skrócony opis etykiety. RoHS i Critical mają typ `bool?`.

Słowniki to osobne tabele:

- `Manufacturers`: nazwa;
- `Customers`: nazwa i opcjonalny kod;
- `Categories`, `Subcategories`: podkategoria wskazuje kategorię;
- `MountingMethods`: początkowo SMT, Pin-In-Paste, THT, Press-Fit, Plugs, Mech;
- `ProductStatuses`: początkowo Last Time Buy, Not For New Design, Obsolete, End of Life;
- `AecQualifications`: początkowo AEC-Q200, AEC-Q101, AEC-Q100;
- `CasePatterns`: wzorzec nazewnictwa obudowy, opis i uwagi.

Słowniki są rozszerzalnymi tabelami, a nie zamkniętymi enumami. Migracja zawiera tylko wskazane początkowe pozycje montażu, statusu i AEC. Nie zawiera fikcyjnych producentów, klientów ani komponentów.

Odniesienia do słowników są opcjonalnymi FK. Wyjątek: `Subcategory.CategoryId` jest wymagany. Komponent może nie mieć kategorii, ale wskazana podkategoria wymaga kategorii. Złożony FK `(SubcategoryId, CategoryId)` i dodatkowy CHECK zapewniają zgodność również w SQL Server. Słowniki użyte przez komponent nie są usuwane kaskadowo.

Komponent ma zero lub wiele kwalifikacji przez `ComponentQualifications` oraz zero lub wiele odnośników przez `ComponentLinks`. Odnośnik ma stabilny GUID, rodzaj (zdjęcie katalogowe/dokumentacja), adres HTTP/HTTPS i opcjonalny opis. Adresy są odnośnikami, nie plikami ani danymi binarnymi. Zmiana kolejności list nie jest zmianą biznesową.

Obudowa, `CasePattern` i footprint to trzy odrębne informacje. Na tym etapie obudowa i footprint są tekstem. Dielektryk, rdzeń, typ indukcyjności, typ złącza i kolor LED również są opcjonalnymi tekstami, bez tworzenia dodatkowych nieuzgodnionych klasyfikacji.

## Typowane parametry i jednostki

Każdy zestaw parametrów i PADS znajduje się w osobnej tabeli, z `ComponentId` jako PK i FK relacji 0..1. Kolumny liczbowe są nullable. Nie ma EAV, JSON parametrów ani niezależnie edytowalnej kolumny Value. Nie wprowadzono nieuzgodnionej reguły wzajemnego wykluczania zestawów ani automatycznego przypisywania ich na podstawie kategorii.

| Zestaw | Właściwość | Jednostka bazowa | Typ SQL |
|---|---|---|---|
| Komponent | MinimumOperatingTemperatureC, MaximumOperatingTemperatureC | °C | decimal(9,3) |
| Rezystor | ResistanceOhms | Ω | decimal(28,12) |
| Rezystor | TolerancePercent | % | decimal(18,6) |
| Rezystor | PowerWatts | W | decimal(18,9) |
| Rezystor | TemperatureCoefficientPpmPerC | ppm/°C | decimal(18,6) |
| Kondensator | CapacitancePf, AbsoluteTolerancePf | pF | decimal(28,9) |
| Kondensator | RatedVoltageV | V | decimal(18,6) |
| Kondensator | TolerancePercent | % | decimal(18,6) |
| Kondensator | EsrMilliOhms | mΩ | decimal(28,9) |
| Kondensator | LifetimeHours | h | int |
| Indukcyjność | InductanceMicroHenries | µH | decimal(28,9) |
| Indukcyjność | TolerancePercent | % | decimal(18,6) |
| Indukcyjność | RatedCurrentA | A | decimal(18,9) |
| Indukcyjność | DcrOhms | Ω | decimal(28,12) |
| LED | LuminousIntensityCd | cd | decimal(28,9) |
| LED | LuminousFluxLm | lm | decimal(28,9) |
| LED | ForwardCurrentA | A | decimal(18,9) |
| LED | ForwardVoltageV | V | decimal(18,6) |
| LED | DominantWavelengthNm | nm | decimal(18,6) |
| Złącze | PositionCount, RowCount | liczba | int |
| Złącze | PitchMm | mm | decimal(18,9) |
| XTAL | FrequencyHz | Hz | decimal(28,6) |
| XTAL | LoadCapacitancePf | pF | decimal(28,9) |
| PADS | RC, TT, DL, QL, BGA, TP, THT, PreTHT, PressFIT, MECH | liczba wyprowadzeń/cech | int |

`LowEsr`, `BTC`, `NS`, `Rohs` i `Critical` to nullable `bit`. Nieobecna wartość pozostaje NULL; nie jest zerem ani false.

Wielkości są nieujemne; temperatury i współczynnik temperaturowy mogą być ujemne. Nie dodano arbitralnych granic fizycznych ani górnej granicy procentowej. Obowiązują zakresy typów SQL/.NET, nieujemne liczby godzin/pozycji/rzędów i relacja temperatura minimalna ≤ maksymalna. Nie wymyślono parametrów diod ani aktywnych oscylatorów.

`DecimalSpecAttribute` jest metadanymi domeny, niezależnymi od EF. Mapowanie SQL i walidator używają tych samych precyzji. Przed zapisem sprawdzany jest zakres `abs(x) < 10^(precision-scale)` oraz dokładna reprezentowalność w zadanej skali. Nadmiarowe niezerowe cyfry powodują błąd, zamiast zaokrąglenia przez SQL. Nadmiarowe zera końcowe nie są utratą dokładności. Sam SQL `decimal` może zaokrąglać wejścia pisane z pominięciem aplikacji, dlatego wszystkie zapisy aplikacyjne powinny przechodzić przez serwis i walidator.

Tolerancja kondensatora może być procentowa lub bezwzględna. Dopuszczalny jest brak obu; jednoczesne ustawienie obu (nawet na zero) odrzuca walidator i SQL CHECK.

ESR jest zawsze zapisany w **mΩ**. W zastanym kodzie nie było `EsrOhms` ani danych ESR wymagających przeliczenia. `ESR0025` oznacza 25 mΩ, a nie 0.025 mΩ ani 25 Ω. Prefiks jest usuwany przy parsowaniu, liczba interpretowana bez znaczenia zer wiodących; ułamki są dozwolone. Formatter techniczny zwraca `ESR25`, `ESR70`, `ESR7500`, bez skalowania do Ω.

## Wprowadzanie i prezentacja

Przepływ: `QuantityParser` → `QuantityConverter` → `DecimalRules`/`ComponentValidator` → zapis liczby w jednostce bazowej. `ParameterInput` udostępnia gotowe operacje dla pojemności, rezystancji i historycznego ESR.

Parser akceptuje jeden separator dziesiętny (kropkę lub przecinek), bez separatorów tysięcy. `1,234pF` oznacza 1.234 pF; `1,234.5pF` jest odrzucane. Obsługuje pF/nF/uF/µF/μF/mF/F oraz rezystancje mR/R/Ω/k/M/G i zapis `4k7`, `0R0`. Prefiksy są rozróżniane według wielkości liter. Niewłaściwa jednostka, liczba, przekroczenie zakresu czy utrata dokładności powodują czytelny błąd. Parser odrzuca również ciche zaokrąglenie podczas samego parsowania `decimal`.

`ParameterFormatter` operuje wyłącznie na kopii wartości. Skaluje rezystancje (mR/R/k/M/G), pojemności (pF/nF/uF/mF/F) i moc (mW/W/kW). Typowo wyświetla od 1 do 999.99; najmniejsza i największa jednostka dopuszczają wyjątki. Standardowo używa dwóch miejsc dziesiętnych, jawnego `AwayFromZero`, usuwa zera końcowe i promuje jednostkę, jeśli zaokrąglenie daje 1000. Mała niezerowa wartość w najmniejszej jednostce zachowuje dokładność potrzebną, by nie pokazać pozornego zera. Rzeczywiste zero jest zerem z symbolem bazowej jednostki, a NULL pozostaje brakiem wartości.

Tolerancja, ppm/°C, ESR i liczby wyprowadzeń nie są automatycznie skalowane. Formatter używa kultury invariant, kropki dziesiętnej oraz `u`. Pełna dokładność liczb pozostaje w modelu i bazie, dostępna przyszłemu edytorowi.

`ComponentValueFormatter` implementuje tylko Value rezystora: rezystancja, tolerancja, moc, kwalifikacje AEC, ppm, rozdzielone `_`. Puste części są pomijane; kwalifikacje są deduplikowane i porządkowane ordinalnie. Przykład: `10M_1%_250mW_AEC-Q200_100ppm`. Gdy nie ma żadnej części, wynikiem jest NULL. Nie ustalono kompletnego Value dla innych kategorii ani reguł opisu Nexo.

## PADS

Wszystkie pola są nullable. RC i TT: 0 albo 2; DL i BGA: 0 albo ≥3; QL: 0 albo ≥4; TP/THT/PreTHT/PressFIT/MECH: dowolny nieujemny `int` do `int.MaxValue`. BTC i NS: `bool?`.

Reguły są sprawdzane przez `ValidatePads` i osobne CHECK w SQL. NULL spełnia CHECK jako brak danych. Nie wylicza się PADS z kategorii, obudowy ani footprintu i nie narzuca ich wzajemnego wykluczania. Przyszły import musi rozróżniać pustą komórkę → NULL, jawne 0 → 0, jawne false → false.

## Zapis, historia i współbieżność

`IComponentService.GetAsync` zwraca odłączony cały agregat wraz z wersją. `SaveAsync(component, expectedVersion)` tworzy komponent dla `expectedVersion = null`; aktualizacja wymaga tokenu z odczytu lub poprzedniego udanego zapisu. Zapis jest pełną wymianą stanu agregatu: NULL usuwa opcjonalny zestaw, a brak elementu listy usuwa powiązanie. To nie jest operacja częściowego PATCH.

`SqlServerComponentService` tworzy nowy kontekst dla każdej operacji. Zapis w jednej jawnej transakcji obejmuje dane wspólne, parametry, PADS, odnośniki, kwalifikacje i historię. Błąd lub konflikt powoduje wycofanie całości przy zwolnieniu transakcji. Konteksty i transakcje są zwalniane asynchronicznie. Serwis nie scala duplikatów ani nie ponawia automatycznie konfliktującego zapisu.

Domyślnie serwis jest właścicielem transakcji i zatwierdza ją po zapisie. Gdy fabryka dostarcza kontekst z istniejącą transakcją, serwis korzysta z niej bez jej zatwierdzania i zwalniania; zakończenie należy wtedy do wywołującego. Testy integracyjne wykorzystują ten wariant do ROLLBACK całego testu. EF tworzy savepoint dla pojedynczego SaveChanges w zewnętrznej transakcji; testy wyłączają MARS, aby zachować tę ochronę.

`ComponentHistory` zapisuje dla każdej zmiany: OperationId, ComponentId, autora, czas UTC, Created/Updated, ścieżkę pola, poprzednią i nową wartość. OperationId i czas są wspólne dla całego zapisu. Ścieżki obejmują np. `Resistor.PowerWatts`, `Pads.TP`, `CustomerId`, `Qualifications.1`, `Links.<GUID>.Url`; markery `.Exists` rejestrują dodanie/usunięcie pustego zestawu parametrów lub odnośnika.

SQL NULL w historii oznacza brak wartości. Liczby i bool są zapisane niezmiennie kulturowo, a tekst jako literał JSON. Dzięki temu NULL, 0, false i tekst `"null"` są różne. Równe liczby o różnej skali dziesiętnej nie generują zmiany. Nie są zapisywane niezmienione pola ani techniczne zmiany Version. Zapis bez zmian nie dodaje historii ani nowej wersji. Zakres historii obejmuje wyłącznie agregat, nigdy konfigurację lub connection string. Zmiany treści samych słowników nie są historią komponentu; zmiany FK/powiązań są.

Autora dostarcza `ICurrentUser`. `WindowsCurrentUser` korzysta lokalnie z bieżącej tożsamości Windows i nie stanowi systemu logowania ani autoryzacji.

`Version` jest application-managed tokenem współbieżności. Kontekst zmienia go także dla zmian wyłącznie parametrów, PADS lub powiązań i wymusza UPDATE korzenia. SQL UPDATE porównuje poprzedni token, a brak pasującego wiersza daje czytelny konflikt „Komponent został zmieniony przez innego użytkownika…”. Serwis nie nadpisuje nowszego stanu. Zmiana zależnego rekordu bez śledzonego korzenia jest odrzucana. Nie ma triggerów chroniących przed zapisami spoza aplikacji; docelowym kanałem edycji jest ten serwis. Nie dodano pól potwierdzenia ani oceny zamienności.

## Filtrowanie i porównywanie

Parametry są liczbami SQL w ustalonych jednostkach, gotowymi do `WHERE` i `ORDER BY` bez parsowania Value. Dodano indeksy na rezystancji, tolerancji i mocy rezystora, pojemności i napięciu kondensatora, indukcyjności i prądzie, kolorze LED, rastrze i liczbie pozycji złącza oraz częstotliwości XTAL. Odniesienia do słowników mają indeksy FK. Przy około 24 tys. komponentów dalsze indeksy należy dobierać do rzeczywistych zapytań.

Nie ma ogólnej punktacji „lepszy/gorszy” ani automatycznego potwierdzania zamienności. Przyszłe porównywanie będzie korzystać z jawnych reguł, np. równej rezystancji, mniejszej tolerancji, nie mniejszej mocy i zgodnej obudowy.

## Migracja i weryfikacja

EF Core SqlServer, Design i lokalny `dotnet-ef` mają zgodną stabilną wersję 10.0.15. Jedyna migracja: `20261113125347_InitialComponentModel`, wraz z designerem i `IPartDbContextModelSnapshot`. WPF nie wykonuje migracji. Nie używa się `EnsureCreated`/`EnsureDeleted`. Po przeglądzie operacji i potwierdzeniu pustej bazy migrację zastosowano do `dataPart` na `(localdb)\MSSQLLocalDB`.

Polecenia z katalogu repozytorium:

```powershell
dotnet tool restore
dotnet build iPart.slnx
dotnet test iPart.slnx
dotnet ef migrations has-pending-model-changes --project src/iPart.Infrastructure --startup-project src/iPart.Infrastructure --context IPartDbContext -- --settings src/iPart.App/appsettings.json
```

Fabryka projektowa odczytuje JSON. Bez `--settings` szuka `appsettings.json` względem `AppContext.BaseDirectory`; jawna ścieżka względna jest interpretowana względem repozytorium, także gdy dotnet-ef zmieni bieżący katalog na katalog projektu. Connection string nie jest wpisany w fabrykę.

Lokalną bazą docelową jest `dataPart` na `(localdb)\MSSQLLocalDB`, wskazana przez niezmieniony klucz `ConnectionStrings:IPartDatabase`. Przed zmianą potwierdzono brak tabel użytkownika i tabeli historii migracji w `iPart_Dev` oraz brak bazy `dataPart`. Następnie utworzono pustą `dataPart`, pozostawiając `iPart_Dev` bez zmian. Nie zmieniano nazwy istniejącej bazy i nie stosowano migracji. Sama zmiana nazwy bazy w konfiguracji nie zmienia modelu ani istniejących plików migracji i snapshotu.

Dokładne wykonane polecenie zastosowania migracji do lokalnej `dataPart`:

```powershell
dotnet ef database update 20261113125347_InitialComponentModel --project src/iPart.Infrastructure --startup-project src/iPart.Infrastructure --context IPartDbContext -- --settings src/iPart.App/appsettings.json
```

Testy jednostkowe obejmują MPN, PADS, tolerancje, jednostki, zapis dziesiętny, ESR, Value, granice jednostek, promocję po zaokrągleniu, małe niezerowe liczby, kultury, historię, precyzje modelu, FK podkategorii i aktualizację tokenu przy zmianach zależnych. Testy tokenu przechwytują zapis przed SQL i nie stanowią dowodu działania współbieżności w silniku bazy.

Testy SQL uruchamia się przez `$env:IPART_RUN_SQL_TESTS='1'; dotnet test iPart.slnx`. Na rzeczywistym SQL Server sprawdzają unikalność z NULL, wielkość liter i białe znaki MPN oraz CHECK PADS i kondensatora w lokalnych tabelach `#temp`. Definicje CHECK i precyzje tabel parametrów pobierane są z modelu EF. Dane testowe istnieją wyłącznie w `tempdb` i znikają wraz z połączeniem; w `dataPart` odczytywane są jedynie dostępność i historia migracji. Nie używa się EF InMemory.

Dodatkowe testy `SqlServerDatabaseTests` włącza `IPART_RUN_DATABASE_TESTS=1`. Na rzeczywistych tabelach `dataPart` weryfikują klucz biznesowy dla wszystkich kombinacji NULL, zmianę producenta/klienta, wielkość liter MPN, NULL/0/false i CHECK PADS, dokładny zapis pojemności i ESR, alternatywne tolerancje, złożony FK kategorii/podkategorii, całość agregatu z historią i brak historii dla zapisu bez zmian. Sprawdzają konflikt dwóch odłączonych wersji komponentu, także przy zmianach samych parametrów, oraz deterministyczną zmianę tokenu pomiędzy wczytaniem a UPDATE, która musi zostać odrzucona przez SQL.

Każdy test tabel rzeczywistych wykonuje ROLLBACK i porównuje liczniki wierszy wszystkich tabel przed/po. Osobne testy sprawdzają transakcję tworzoną przez sam serwis: wymuszają błąd zapisu historii po faktycznym INSERT komponentu, a przy udanym zapisie obserwują cały agregat przed COMMIT i celowo powstrzymują COMMIT, aby zwolnienie transakcji wycofało dane testowe. Po testach liczba komponentów i wpisów historii wynosi 0; pozostają wyłącznie słowniki początkowe migracji i wpis migracji.

Nie wykonano testu obciążeniowego z jednocześnie działającymi użytkownikami ani testu wydajnościowego 24 tys. komponentów i automatycznego testu GUI. Testy konfliktu wersji stosują kontrolowane przeplatanie operacji, a nie losowy wyścig wątków.

## Przyszłe rozszerzenie magazynowe

Nie utożsamiamy technicznego Id komponentu z symbolem magazynowym i nie zakładamy relacji jeden komponent = jedna pozycja magazynowa. Model relacji i kierunki synchronizacji pozostają do uzgodnienia przed wdrożeniem.

Przyszłe dane magazynowe obejmą: symbol magazynowy, ilość w opakowaniu, fabryczną ilość w opakowaniu, aktualną liczbę opakowań, współczynnik zapasu produkcyjnego, PKWiU, ścisłe zarachowanie, cenę i walutę, generowany opis DESC.part1, dodatkowy opis DESC.part2 oraz skrócony opis etykiety ESSEGI. Wymagają osobnych uzgodnień jednostek ilościowych, dokładności, walut i relacji do opakowań.

Istniejące `AdditionalDescription` i `LabelDescription` opisują komponent, ale nie oznaczają jeszcze gotowego mapowania do DESC.part2/ESSEGI. Nie ustalono reguł generowania DESC.part1 ani nomenklatury Nexo. Integracje Nexo/ESSEGI i sposób importu starej historii z Excela pozostają poza bieżącą implementacją.

## Wynik bieżącej weryfikacji

- `dotnet build iPart.slnx`: powodzenie, 0 błędów i 0 ostrzeżeń.
- `dotnet test` z `IPART_RUN_SQL_TESTS=1` i `IPART_RUN_DATABASE_TESTS=1`: 127/127 testów zakończonych powodzeniem, bez pominięć. Z tego 16 testów korzysta z rzeczywistego SQL Server (4 #temp/odczyt oraz 12 tabel/serwisu).
- `dotnet ef migrations has-pending-model-changes`: brak zmian modelu od ostatniej migracji.
- `__EFMigrationsHistory` zawiera `20261113125347_InitialComponentModel`; brak oczekujących migracji. Są wszystkie 19 tabel modelu i tabela historii migracji.
- Skrypt wdrożenia i raporty przed/po znajdują się w `artifacts/migrations/`.

## Zmienione i dodane pliki

```text
iPart.slnx
dotnet-tools.json
README.md
docs/data-model.md
src/iPart.Core/Components/Component.cs
src/iPart.Core/Components/Dictionaries.cs
src/iPart.Core/Components/Parameters.cs
src/iPart.Core/Components/ComponentValidator.cs
src/iPart.Core/Components/ComponentServices.cs
src/iPart.Core/Components/ComponentChanges.cs
src/iPart.Core/Units/QuantityInput.cs
src/iPart.Core/Units/ValueFormatter.cs
src/iPart.Infrastructure/iPart.Infrastructure.csproj
src/iPart.Infrastructure/IPartDbContext.cs
src/iPart.Infrastructure/IPartDbContextFactory.cs
src/iPart.Infrastructure/SqlServerComponentService.cs
src/iPart.Infrastructure/Migrations/20261113125347_InitialComponentModel.cs
src/iPart.Infrastructure/Migrations/20261113125347_InitialComponentModel.Designer.cs
src/iPart.Infrastructure/Migrations/IPartDbContextModelSnapshot.cs
tests/iPart.Tests/iPart.Tests.csproj
tests/iPart.Tests/DomainTests.cs
tests/iPart.Tests/QuantityTests.cs
tests/iPart.Tests/FormattingTests.cs
tests/iPart.Tests/HistoryTests.cs
tests/iPart.Tests/InfrastructureModelTests.cs
tests/iPart.Tests/SqlServerConstraintTests.cs
tests/iPart.Tests/SqlServerDatabaseTests.cs
artifacts/migrations/dataPart.sql
artifacts/migrations/dataPart-state-before.json
artifacts/migrations/dataPart-state-after.json
artifacts/migrations/verification.md
```

Szablonowy `tests/iPart.Tests/UnitTest1.cs` został zastąpiony właściwymi testami w trakcie tworzenia projektu testowego.
