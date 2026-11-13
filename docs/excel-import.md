# Import Excel: etap odczytu, mapowania i podglądu

Ten etap nie zapisuje komponentów, historii, słowników ani rekordów importu do SQL Server. Nie zmienia modelu EF, snapshotu ani zastosowanej migracji. Nie ma przycisku zatwierdzania importu do bazy.

## Uruchomienie i ręczne sprawdzenie

1. `dotnet run --project src/iPart.App`.
2. Na zakładce „Połączenie z bazą” sprawdź dotychczasowe połączenie z `dataPart`.
3. W „Import Excel” kliknij „Wybierz plik .xlsx” i wskaż lokalny skoroszyt. Ścieżka pojawi się nad przyciskami.
4. W „Mapowanie” opcjonalnie zaznacz jawne decyzje `ThP → TP` i `NonS → NS`. Bez zaznaczenia dane te pozostają w źródle z ostrzeżeniem.
5. Kliknij „Analizuj plik”. Operacja działa poza wątkiem UI. Przycisk „Anuluj” przerywa odczyt lub analizę; częściowy wynik nie jest publikowany. Przy błędzie lub anulowaniu można uruchomić analizę ponownie.
6. Przejrzyj podsumowanie i stronicowaną tabelę. Zaznaczenie komponentu pokazuje propozycje pól z pełną dokładnością i adresy komórek źródłowych. Kolejne strony mają po 200 rekordów.
7. „Problemy” filtruje informacje, ostrzeżenia i błędy. „Źródło” zawiera wszystkie odczytane niepuste komórki wszystkich arkuszy, również niezamapowane dane, oryginalne formuły i ich cache. Po zaznaczeniu komórki panel poniżej pokazuje jej pełną treść, również długie formuły i notatki. Można pokazać tylko komórki niezamapowane. „Arkusze i kolumny” pokazuje arkusze pomocnicze, nieimportowane arkusze i nieustalone mapowania.
8. Zmiana decyzji mapowania unieważnia poprzedni wynik. Wykonaj analizę ponownie.
9. „Eksportuj raport…” jawnie zapisuje pełny JSON lub CSV. Raport nie może zastąpić źródłowego `.xlsx`.

W razie niedostępności bazy odczyt pliku nadal jest możliwy. Cały wynik otrzymuje jednak błąd blokujący: kolizje i słowniki nie zostały wiarygodnie sprawdzone, więc gotowych rekordów jest 0.

## Biblioteka i bezpieczeństwo odczytu

Użyto **Microsoft Open XML SDK (`DocumentFormat.OpenXml` 3.5.1)**, stabilnego wydania zgodnego z .NET 10. Jest utrzymywany w repozytorium .NET Foundation/Microsoft, na licencji **MIT**. Źródła: [pakiet NuGet](https://www.nuget.org/packages/DocumentFormat.OpenXml/3.5.1), [repozytorium](https://github.com/dotnet/Open-XML-SDK), [licencja MIT](https://github.com/dotnet/Open-XML-SDK/blob/main/LICENSE).

SDK pozwala odczytywać strukturę ZIP/XML i wiersze strumieniowo przez `OpenXmlReader`, bez instalacji Excela i bez silnika obliczającego formuły. Plik jest otwierany `FileAccess.Read`, a pakiet `SpreadsheetDocument.Open(..., false)` z wyłączonym AutoSave. Importer nie otwiera odnośników zewnętrznych, nie wykonuje makr ani tekstu komórek. Zapisane wyniki formuł są danymi źródłowymi; nie są obliczane ani uznawane za źródło parametrów zamiast arkuszy pomocniczych.

Przed i po analizie porównywany jest SHA-256 pliku. Raport zachowuje arkusz, wiersz, numer kolumny, adres, nagłówek, oryginalną wartość/formułę, zapisany wynik, format liczbowy i zastosowane mapowanie. Przygotowane wiersze samych formuł są liczone osobno i nie stają się komponentami; ich komórki pozostają dostępne w źródle i eksporcie. Puste komórki zawierające tylko formatowanie nie są materializowane.

UI stosuje stronicowanie po 200 pozycji oraz wirtualizację wierszy i kolumn DataGrid. Nie tworzy kontrolek dla całego skoroszytu. Zużycie pamięci analizy zależy od liczby niepustych danych i długości formuł, ponieważ raport ma zachować pełne źródło; nie jest to importer o stałym zużyciu pamięci.

## Nagłówki i MPN

Arkusze wejściowe to `partsData`, `resValue`, `capValue`, `ctceValue`. Ich nazwy są jawne. Nagłówki są wyszukiwane po nazwach, z pominięciem białych znaków na krawędziach i bez rozróżniania wielkości liter nagłówków. Nie ma stałych numerów kolumn ani założenia nagłówka zawsze w pierwszym wierszu.

Wymaganym nagłówkiem jest `Part Number`; pozostałe mapowane pola są opcjonalne i ich nieobecność jest raportowana. Brak jednoznacznego nagłówka klucza lub powtórzone nagłówki blokują gotowość wyniku. Powtórzone kolumny nie są wybierane według kolejności. Wiersze przed nagłówkiem i dane bez nagłówka pozostają w źródle.

Komponenty powstają wyłącznie z wierszy `partsData` z niepustym Part Number, poza wierszami samych przygotowanych formuł. Nie tworzy się komponentów z arkuszy parametrów. Formuła MPN bez cache nie jest podstawą zgadywania identyfikatora.

Tekstowy MPN przechodzi przez istniejące `MpnRules.Normalize`: tylko `Trim()`, bez usuwania znaków wewnętrznych, zer i nawiasów ani zmiany wielkości liter. Producent i klient pozostają NULL; nie ma reguły wyciągania producenta z nawiasów i nie mapuje się nieuzgodnionych kolumn producenta/klienta na podstawie domysłów.

Liczbowy MPN uwzględnia prostą, jednoznaczną maskę zer (`000000`). General lub `@` daje ostrzeżenie, że wcześniej utraconych zer nie da się odtworzyć. Liczba ułamkowa, ponad 15 cyfr dokładności Excela, wartość logiczna lub inny format liczbowy daje błąd wymagający rozstrzygnięcia. Oryginał i format pozostają w raporcie. Notacja wykładnicza w numerycznym XML jest rozwijana dokładnie jako cyfry, bez użycia `double` ani cichego zaokrąglenia; tekstowe zapisy parametrów nadal podlegają istniejącym parserom domeny.

## Mapowanie

| Źródło partsData | Propozycja |
|---|---|
| Part Number | Mpn |
| Mounting | MountingMethodId, odczytany słownik |
| Case | CaseName |
| Footprint | Footprint |
| Notes | Notes |
| Critical | bool? Critical |
| Value | Wyłącznie porównawcza wartość źródłowa |
| RC, TT, DL, QL, BGA, TP, THT, preTHT, pressFIT, Mech/MECH, BTC, NS | Właściwe pola nullable PADS |
| ThP, NonS | TP, NS dopiero po jawnej decyzji użytkownika |

Jawny alias montażu: `MECH → Mech`. Pozostałe wartości montażu muszą odpowiadać odczytanemu słownikowi. Nieznane wartości i brak znanej kwalifikacji w słowniku są błędami; podczas podglądu nie tworzy się słowników.

Puste wartości oznaczają NULL. Liczniki PADS korzystają z istniejącego walidatora, a nie są wyliczane z obudowy czy kategorii. Wartości bool akceptują jawne pary `true/false`, `1/0`, `tak/nie`, `yes/no` bez rozróżniania wielkości liter. Pole lowESR przyjmuje również jawny tekst `lowESR`, a kwalifikacja — `AEC-Q200`. Nieznane oznaczenia nie stają się automatycznie true.

`Case confirmation`, `Footprint confirmation`, `Value confirmation`, `Tag1` i dalsze Tagi, komórki bez nagłówka za Tagami i `rmFootprint` pozostają wyłącznie źródłem. Nie tworzy się stanów potwierdzenia, nie interpretuje nieznanych dat i autorów i nie zapisuje starej historii do ComponentHistory. Tak samo zachowuje się nieustalone temperatury, wymiary, Features i inne pola. `RMCase_source`, `standardCase` i pozostałe arkusze są wykrywane i pokazywane, ale nie tworzą komponentów ani danych słowników.

## Parametry i łączenie

MPN jest łączony ordinalnie po istniejącym Trim. NULL producenta/klienta nie jest wildcardem. Jeśli MPN wskazuje wiele wierszy partsData, wszystkie otrzymują konflikt i nie przypisuje się parametrów arbitralnie. Wszystkie wiersze grupy duplikatu biznesowego są oznaczane.

- `resValue`: Rezystancja, Tolerancja, Moc, AEC-Q200 i jawna kolumna ppm.
- `capValue`: Pojemność, Napięcie, Dielektryk, Tolerancja i AEC-Q200.
- `ctceValue`: Pojemność, Napięcie, Tolerancja, ESR [mR], lowESR, AEC-Q200, Life (hs).

Wykorzystuje się `QuantityParser`, `QuantityConverter`, `ParameterInput`, `DecimalRules`, `ComponentValidator` i `ComponentValueFormatter`. Istniejący konwerter rozszerzono o jawne jednostki mW/W/kW i mV/V/kV, bez dublowania reguł pojemności lub rezystancji.

Tekstowe wielkości wymagają znanej jednostki. Pojemności trafiają do pF; ESR do mΩ. `ESR0025` daje 25, `ESR0070` daje 70; dopuszczalne są również liczby w zadanej przez nagłówek jednostce mΩ i jawne sufiksy mR/mΩ. Tolerancja kondensatora z `%` jest procentowa, a z jednostką pojemności — bezwzględna w pF. Jednoczesne ustawienie obu blokuje wynik.

Liczbowe komórki bez tekstowego sufiksu oznaczają jednostkę bazową mapowanego pola: Ω, W, pF, V, punkty procentowe, ppm/°C, mΩ i h. Format procentowy komórki jest uwzględniany (`0.01` z formatem `%` daje 1%). Tekstowe tolerancje wymagają `%` lub jednostki pojemności. PPM odczytuje się wyłącznie z kolumny `ppm` (liczba lub jawny sufiks ppm/ppm/°C); nie z Features/Temp. Godziny i PADS wymagają nieujemnych liczb całkowitych w zakresie int.

Każde pole zbiera wszystkie propozycje i pochodzenie. Równe wartości po przeliczeniu są oznaczane jako powtórzenia. Sprzeczne wartości nie mają zwycięzcy — pole pozostaje nierozstrzygnięte z błędem i wszystkimi źródłami. Zgodne dane z capValue i ctceValue mogą utworzyć wspólną propozycję. Nie stosuje się zasady pierwszy/ostatni wygrywa. Dwa alternatywne rodzaje tolerancji albo jednoczesne propozycje rezystora i kondensatora wymagają rozstrzygnięcia.

Generowane Value istnieje tylko dla rezystora i stosuje istniejący formatter z PPM. Dla innych kategorii nie ustalono pełnego formatu Value. Dane liczbowe zachowują dokładność niezależną od prezentacji.

## Odczyt bazy

`IImportReferenceReader` ma wyłącznie metodę odczytu. Implementacja SQL pobiera trzy zestawy przez `AsNoTracking`: sposoby montażu, kwalifikacje AEC i klucze biznesowe komponentów. Nie wywołuje serwisu zapisu, SaveChanges, migracji ani dowolnego SQL mutującego. Konfiguracja pochodzi z tego samego appsettings co sprawdzanie połączenia.

Klucze w pamięci zachowują porównanie ordinalne MPN i dokładne nullable identyfikatory, zgodnie z indeksem SQL BIN2 bez filtra NULL. Kolizje z bazą są błędami, nie propozycją automatycznego aktualizowania lub scalania. Odczyt słowników i kluczy jest migawką czasu analizy; po tym etapie nadal nie ma zapisu.

## Liczniki

- Rekordy partsData: wyłącznie propozycje komponentów z niepustym MPN po regułach powyżej.
- Gotowe: rekordy bez błędów, również z ostrzeżeniami; nie oznacza to autoryzacji zapisu.
- Z błędami: rekordy z co najmniej jednym błędem. Gotowe + z błędami = partsData.
- Z ostrzeżeniami: rekordy z co najmniej jednym ostrzeżeniem; mogą należeć do obu powyższych grup.
- Grupy duplikatów: liczba grup identycznego klucza biznesowego w partsData, nie liczba wierszy grupy ani powtórzeń pól pomocniczych.
- Kolizje z bazą: liczba wierszy partsData mających istniejący klucz w bazie.
- Niedopasowane pomocnicze: liczba niepustych wierszy pomocniczych bez klucza, z niepewnym kluczem, bez dopasowania lub z wieloznacznym dopasowaniem. Nie są nowymi komponentami.
- Problemy: liczba wpisów raportu, także informacyjnych; jeden problem może dotyczyć wielu źródeł/rekordów.
- Dotknięte rekordy: liczba unikalnych rekordów partsData z ostrzeżeniem lub błędem. Globalne błędy nagłówków/bazy blokują wszystkie rekordy; globalne ostrzeżenia o opcjonalnej kolumnie nie są powielane na każdy rekord.
- Niezamapowane kolumny: różne pary arkusz/kolumna zawierające niepuste dane bez ustalonego mapowania; obejmują kolumny bez nagłówka. Ich komórki pozostają w raporcie.

## Eksport i testy

JSON zachowuje typowaną propozycję, wszystkie źródła, pochodzenie pól, decyzje mapowania i problemy. CSV ma postać długiego raportu z sekcjami podsumowania, propozycji, pochodzenia, źródła i problemów; pola są cytowane, a niebezpieczne teksty zaczynające się od `=`, `+`, `-`, `@` (również po białych znakach) oraz od tabulatora/CR/LF dostają prefiks apostrofu. CSV jest bezpiecznym tekstowym raportem, a JSON przechowuje dokładny oryginał bez takiej neutralizacji.

Odpowiednie testy bez ponawiania wcześniejszych testów mutujących SQL:

```powershell
$env:IPART_RUN_IMPORT_SQL_TESTS = '1'
dotnet test iPart.slnx --filter 'FullyQualifiedName!~SqlServer'
Remove-Item Env:IPART_RUN_IMPORT_SQL_TESTS
```

Kontrolowane pliki obejmują kolejność i brak/powielenie nagłówków, formuły i brak cache, tekstowe/numeryczne MPN, maski zer i wykładniki XML, duplikaty i niejednoznaczne dopasowania, zgodne/sprzeczne parametry, PADS, ESR, jednostki, precyzję, eksport, zachowanie niezamapowanych danych, anulowanie oraz plik z 24 000 komponentów. Osobne testy WPF sprawdzają rzeczywiste otwarcie zakładki, anulowanie i unieważnianie wyniku po zmianie mapowania. Test odczytu SQL blokuje polecenia inne niż SELECT i porównuje zawartość wszystkich tabel oraz bajty skoroszytu przed i po analizie.

Pełnego źródłowego skoroszytu użytkownika nie dostarczono; test 24 000 rekordów korzysta z kontrolowanego pliku, nie traktuje próbki jako pełnej bazy. Podgląd wymaga ręcznego rozstrzygnięcia zgłoszonych nieznanych formatów i konfliktów; celowo nie zawiera edytora rozstrzygnięć dla dowolnych pól ani zapisu do bazy.

Końcowa weryfikacja: `dotnet build iPart.slnx` bez błędów i ostrzeżeń; 137/137 odpowiednich testów (134 domeny/importu wraz z jednym odczytowym testem SQL oraz 3 WPF), bez powtarzania wcześniejszych 16 testów SQL. Sprawdzono również uruchomienie zakładki w aplikacji i zachowane sprawdzanie połączenia. Analiza próbki i test odczytu SQL potwierdziły niezmienność pliku wejściowego i zawartości bazy. Model nadal odpowiada zastosowanej migracji.
