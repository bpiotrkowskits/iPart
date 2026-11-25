---
status: roboczy
zakres: decyzje dotyczące mechanizmów VBA przy przenoszeniu do aplikacji iPart — przegląd w toku (sesje 1–2 z 4 zakończone)
przegląd: 2026-11-25
---

# Decyzje VBA — notatki z przeglądu kodu

Źródło: przegląd kodu z Kierownikiem działu Obsługi Projektu, 2026-11-24. Podstawa: [analiza-techniczna/](analiza-techniczna/README.md). Szkic roboczy — pełna wersja według uzgodnionego szablonu powstanie po zakończeniu przeglądu.

Oznaczenia: **[kod]** — jak działa dziś, **[zamiar]** — jak miało działać, **[decyzja]** — co robimy w aplikacji, **[niepewne]** — do potwierdzenia.

Zasada ogólna: **plików Excel/VBA już nie poprawiamy.** Znalezione błędy służą tylko temu, by aplikacja ich nie powtórzyła.

## Sesja 1 — comparator

| # | Mechanizm | [kod] dziś | [decyzja] |
|---|---|---|---|
| 1.1 | Rozbijanie desygnatorów | Zakresy tylko z polem „przedziały” (zabezpieczenie: świadoma decyzja); gubi `C2A-C2D`, `R01-R05`, średnik | **Zmienić:** reguły z 0.1.0; decyzja per rozbicie („rozwiń” / „zostaw jako tekst”, „Akceptuj wszystkie”); domyślnie „rozwiń” tylko dla pełnych zakresów; skrócone/niejednoznaczne ręcznie; zapis zablokowany bez decyzji |
| 1.2 | Normalizacja | Wielkie litery także w Value/Part Number/Notes trafiających do iPart; `m` chronione tylko w 3 polach; obejścia znaków specjalnych (ograniczenie Power Apps) | **Zmienić (obowiązkowo):** normalizacja tylko do porównania, dane zawsze oryginalne, pełne znaki specjalne (Unicode); `µ` = `μ` przy porównaniu; `u` ≠ mikro |
| 1.3 | Kod L/F/D/N | Puste po obu stronach = rozbieżność; Quantity niesprawdzana | **Zmienić:** puste po obu stronach → ostrzeżenie „brak danych”, nie rozbieżność; Quantity vs liczba desygnatorów → ostrzeżenie |
| 1.4 | Wycinki i akceptacja | Akceptacja grupowa (szybkość); nierozwiązane wycinki jako osobne linie na końcu iPart | **Przenieść/usprawnić:** akceptacja wycinka jednym kliknięciem; zaakceptowany zostaje w linii; nierozwiązany → linia wydzielona z odnośnikiem do źródłowej; potem „scal z linią N” (numer nieużywany ponownie, linia zostaje w historii jako scalona) albo „osobny komponent”. Reguła stałej kolejności dotyczy linii BOM Klienta — linie wydzielone mogą być na końcu |
| 1.5 | Potwierdzenie linii | Bez śladu podstawy | **Zmienić:** podstawa obowiązkowa: odpowiedź Klienta / wcześniejsza realizacja / decyzja Opiekuna (uzasadnienie) + kto, kiedy; Opiekun może potwierdzić linię wydzieloną bez Klienta |
| 1.6 | THT | Osobna biblioteka, dokładne dopasowanie | **Zmienić:** Mounting z dataPart po Part Number; bez MPN w bazie — po Footprint: ≥ 6 wystąpień i zawsze ten sam Mounting → automatycznie (oznaczone „z Footprint”); 1–5 lub różne → podpowiedź do zatwierdzenia; brak → puste |

Reguła przekrojowa: **rozpoznany Part Number → iPart od razu uzupełnia jak najwięcej danych z dataPart.**

## Sesja 1 — iPart

| # | Mechanizm | [kod] dziś | [decyzja] |
|---|---|---|---|
| — | Aktualizacja z MASTER, blokada zapisu MASTER, import iPart v1/v2 | Makra w każdym pliku | **Porzucić** (migracja starych danych — część B) |
| — | CSV comparator → iPart | Twarde mapowanie A…AF | **Porzucić** — comparator i iPart jednym modułem |
| — | Klient/projekt z nazw folderów | — | **Zmienić:** pola pakietu |
| 2.1 | DNP przy imporcie | Fragment tekstu na brzegu (`850nm`, `NIS…` → DNP) | **Zmienić:** tylko osobne słowo DNP/DNM/DNI/NM/NI; do zatwierdzenia w podglądzie |
| 2.2 | PCB | Linia dopisywana przez comparator | **Przenieść:** zwykła linia, Mounting PCB, automatycznie na końcu (zakupy, kompletacja) |
| 2.3 | Kilka MPN w komórce | Separator za każdym razem; pierwszy MPN nadpisuje Part Number | **Zmienić:** separator główny + wyjątek per komórka; oryginał zostaje; MPN-y na liście zatwierdzonych z podstawą „z BOM Klienta”; pierwszy = preferowany |
| 2.4 | Value z dataPart | Nadpisuje Value Klienta bez ostrzeżenia | **Zmienić:** sekcja **TS** (wiodąca) i **Oryginał**; niezgodność → rozbieżność do decyzji (porównanie liczb po przeliczeniu jednostek, tylko podane przez Klienta parametry); nierozpoznany zapis Klienta → blokada do „sprawdziłem”; aplikacja może się „uczyć” zapisów po zatwierdzeniu [niepewne — forma] |
| 2.5 | Nowe MPN do dataPart | Automatycznie (celowo — „nic nie zgubić”) | **Zmienić:** lista „Nowe MPN spoza bazy”: dodaj bez zatwierdzenia / z zatwierdzeniem / pomiń |
| 2.6 | PPM i Part TS | Trzy sprzeczne reguły PPM | **Zmienić:** Value zawsze pełny (z PPM); Part TS = konfigurowalny widok; domyślnie PPM gdy tol. < 1% lub PPM ≤ 50; ustawienia: firma → komponent w bazie → Klient → pakiet → linia; zmiana ustawień w istniejących pakietach → lista „było → będzie” do zatwierdzenia |
| 2.6a | Parametry nieukrywalne | — | Rezystor i drabinka: rezystancja, tolerancja, moc. Kondensator: pojemność, napięcie, dielektryk, tolerancja. Tantal/elektrolit: pojemność, napięcie, tolerancja |
| 2.7 | Zatwierdzenie P&P | Zapisuje Footprinty całego BOM (też DNP/THT), nadpisuje, tworzy MPN | **Zmienić:** tylko linie SMT; lista zmian „było → będzie” z wyborem „w bazie / tylko w projekcie”; Footprint raz ustalony obowiązuje wszędzie; footprint dedykowany = ustawienie jednego Klienta |
| 2.8 | Typ komponentu / Part TS | Z liter desygnatora (C, CE, CT, R) | **Zmienić:** typ z dataPart (litery tylko podpowiedzią); Part TS z parametrów: rezystory, drabinki, kondensatory, tantale, elektrolity; MPN: dławiki, ferryty, kryształy, LED, reszta |
| 2.9 | Zamienniki | Tekst Value + Case, automatycznie | **Zmienić:** po liczbach; takie same lub lepsze → akceptowane automatycznie; liczą się tylko parametry widoczne w Part TS (ukryty = nieistotny); Case zawsze równy; dielektryk: C0G → X8R → X7R → X5R → X7S → X6S → X7T → Y5V (lepszy dozwolony); AEC wymagany, jeśli linia go ma |
| 2.10 | Zatwierdzenie BOM | Jednorazowe; blokada dla sPart/cPart | **Przenieść:** po parametrach, Part TS i zamiennikach dla potwierdzonych linii → zakupy i magazyn działają; bez cofania. Nowe wersje BOM: dla ZK (zmiany tymczasowe) albo nowa rewizja (nowy BOM Klienta, przez comparator: „nanieś wszystkie” / część automatycznie + reszta ręcznie / „od początku” z wyborem starego lub nowego P&P); poprzednią wersję można archiwizować; kilka aktywnych; przejście ZK w toku: Opiekun proponuje, zakupowiec zatwierdza. Poprawki w sekcji TS = ta sama rewizja |
| 2.11 | Odświeżanie zamienników po zmianie linii | Kasuje wszystkie zatwierdzone i niedozwolone (+ błąd literówki) | **Zmienić:** niedozwolone zostają (usuwanie ręczne z powodem); „parametry” → aktualizowane; Klient / TS / lista z BOM → „odrzuć / zachowaj / wstrzymaj decyzję” (wstrzymane niewidoczne dla zakupów i magazynu; zatwierdzenie przez zakupy aktywuje tylko ten jeden) |
| 2.12 | Historia zmian | Porównanie z kopią po pozycji; numery dopiero po zatwierdzeniu | **Zmienić:** zapis przy zmianie, zawsze; zmiany po zatwierdzeniu BOM → do obsłużenia przez zakupy i magazyn; Footprint tylko dla produkcji |
| 2.12a | Sesja edycji | Zapis pliku | **Zmienić:** wersja robocza → „Zapisz zmiany” = jedna operacja (jeden wpis, jedno powiadomienie); w czasie edycji inni widzą ostatni zapis; blokada całego BOM (wariant A), później może blokada per sekcja (B) [niepewne — zwalnianie porzuconej blokady] |
| 2.13 | entPart | Bez importu zwrotnego; „?” razem z resztą; język z listy krajów | **Zmienić:** lista dla ZK z używanej wersji BOM; „?” oznaczone „do potwierdzenia przez Klienta”; język = ustawienie Klienta; stały układ i import odesłanego pliku jako dostawy od Klienta |
| 2.14 | Formularze | Listy obudów w kodzie; ESR `0.3` → 3 mΩ | **Zmienić:** słowniki w bazie (edytują Opiekunowie); Footprint edytuje Technolog w P&P; ESR z jednostką (bez jednostki = mΩ), w bazie dokładnie, w Value `ESR0300` |

Reguły pod konkretnych Klientów (wpisane dziś w kod) → **ustawienia Klienta**: PPM zawsze, MPN zawsze (także RC), footprinty dedykowane.

Kierunek ogólny: przy dobrze zoptymalizowanej pracy — **wszystko według nowych założeń**, zamiast wciągania starych danych 1:1.

## Sesja 2 — fusePart

| # | Mechanizm | [kod] dziś | [decyzja] |
|---|---|---|---|
| 3.1 | Czym jest łączenie | Osobny plik z własnymi listami MPN i zatwierdzeniem; nic nie wraca do iPart | **Zmienić:** łączenie = zestawienie (widok): przechowuje tylko ZK, wersje BOM i ilości; linie, MPN, zamienniki zawsze z BOM pakietów; różnica między pakietami rozwiązywana w BOM pakietu. Decyzje dotyczą zawsze BOM albo BOM dla ZK — łączenie nie ma własnych decyzji |
| 3.2 | Klucz łączenia | Tekst `Part TS/Case`, tylko obcięte brzegi i apostrof; różnica spacji = osobne komponenty | **Zmienić:** (1) ten sam MPN + ten sam Case → łączenie automatyczne; ten sam MPN, różny Case → decyzja operatora. (2) te same parametry (po liczbach) + ten sam Case → automatycznie; różny Case → decyzja; parametry zgodne, ale jedna linia ma dodatkowe **nieukryte** parametry → decyzja operatora |
| 3.3 | Decyzja o łączeniu | Brak (klucz identyczny albo nie) | **Nowe:** łączenie tworzy i decyduje **zakupowiec** (wybiera BOM-y kupowane razem, rozstrzyga wątpliwe pozycje). Decyzja „połącz / nie łącz” zapisana w łączeniu (kto, kiedy, które linie) i zapamiętana jako podpowiedź dla tej samej pary komponentów. Błąd w BOM poprawia się w BOM pakietu |
| 3.4 | MPN w połączonej pozycji | Zatwierdzone = część wspólna wszystkich ZK w pliku (także bez tego komponentu → pusta lista); niedozwolone nie przechodzą | **Zmienić:** zatwierdzone = część wspólna tylko połączonych linii; niedozwolone = suma ze wszystkich połączonych linii. Pusta część wspólna → zakupowiec może połączyć, wskazując MPN z uzasadnieniem; to decyzja o zamienniku w BOM pakietu, zatwierdzana przez zakupowca z podstawą (wg 4.1) |
| 3.5 | Kolumna „Różnice” | Każda kolumna z >1 wartością (także pusta vs wypełniona) → kolor + nazwa kolumny; Footprint nie przechodzi | **Zmienić:** informacja o różnicy w konkretnej komórce (Description, Value, Notes itd.) — różnic jest niewiele; Confirmed nie dotyczy (tylko zatwierdzone wersje BOM); Footprint nie pokazujemy. **Linie DNP** nie łączą się z innymi, zostają na dole zestawienia. **Nowe:** rozłączenie pojedynczej linii z połączonej pozycji, z podaniem powodu. Pusta vs wypełniona komórka → pokazywana słabiej niż sprzeczność („uzupełnione tylko w pakiecie A”) |
| 3.6 | Towar powierzony w łączeniu | Każda pozycja z czymkolwiek w EntPart → na listę cała SumaQty; zapas od sumy | **Zmienić:** ilość powierzona tylko z linii powierzonych; pozycja mieszana (powierzona w jednym pakiecie, kupowana w innym) → pokazana zakupowcowi jako różnica, do decyzji. „?” oznaczone jak w 2.13. Eksport listy towaru powierzonego z poziomu łączenia (układ i język wg 2.13), **jedna lista na łączenie, zapas od sumy**. **Łączenie tylko w obrębie jednego Klienta** |
| 3.7 | Zatwierdzenie łączenia i zmiany | Zatwierdzenie bez warunków, przełącza na tryb przyrostowy; zmiany = pogrubienie z ostatniego importu; ZK wpisywany ręcznie, klucz bez roku | **Zmienić:** brak osobnego zatwierdzenia — zestawienie dostosowane przez zakupowca jest stanem obowiązującym i w tej formie idzie do kompletacji. Zmiana BOM lub ilości ZK → lista „było → będzie” dla zakupowca (dokupić / anulować / nadmiar ze zmiany), nic po cichu [niepewne: od którego momentu zmiany wymagają obsłużenia — np. od pierwszego zamówienia]. ZK = rzeczywisty numer w formacie `nnn/rrrr`. Dodanie ZK do istniejącego łączenia dozwolone: bez wątpliwości łączy się automatycznie, wątpliwości rozstrzyga zakupowiec |
| — | Błędy techniczne fusePart | Footprint pomijany, MPN od kolumny P, przerwanie na pustym Part TS, ID z pozycji wiersza, kopie zapasowe, przycisk bez makra, dwie kopie pliku | **Porzucić** — rozwiązane przez model 3.1 |

## Sesja 2 — sPart

| # | Mechanizm | [kod] dziś | [decyzja] |
|---|---|---|---|
| 4.1 | Status MPN i zamiennik od zakupowca | Status automatyczny z list iPart; ręczny status miał trafiać do iPart, ale przez błąd wielkości liter nigdy nie trafia; zakupowiec nie widzi niedozwolonych | **Zmienić:** zakupowiec widzi przy pozycji MPN zatwierdzone (z podstawą) i niedozwolone; wybór z zatwierdzonych → status automatyczny; niedozwolonego nie da się wybrać. Nowy MPN spoza listy = zamiennik do BOM: takie same lub lepsze parametry → automatycznie; w pozostałych przypadkach **zatwierdza zakupowiec z podaniem podstawy** (także „zatwierdzony przez Klienta”). Zakres: na stałe (BOM pakietu) albo tylko dla ZK (wersja BOM dla ZK — dawny „Jednorazowo Dopuszczony”). Zamówienia (ZD) powstają w innym systemie — iPart ich nie blokuje |
| 4.2 | Ilości, zapas, stan magazynu | Formuły; stan wklejany ręcznie; MPN do szukania stanu wpisywany ręcznie przy każdej linii; zablokowane doliczane do stanu; brak rezerwacji; zapas z łącza do „Obudowy.xlsx”, Case spoza tabeli → „bł.” | **Zmienić:** zapas z reguł zapasu w iPart wg Case; Case bez reguły → ostrzeżenie i decyzja zakupowca, **zapamiętana** jako podpowiedź (Case, przypisanie, kto, kiedy, przy jakim ZK). Stan magazynu: w pierwszej wersji **import pliku**, docelowo odczyt z systemu magazynowego. Zakupowiec wskazuje MPN z magazynu (nie opakowanie); wybór MPN, który ma ilość „w kompletacji”, „na ZD” albo „zablokowane” → ostrzeżenie do akceptacji; przy nim lista dostępnych opakowań i pole „ilość do pobrania z magazynu” [niepewne: czy import stanu zawiera dane opakowań]. Rezerwacje — poza zakresem (wywiad) |
| 4.3 | Linie niepotwierdzone | Tylko podświetlenie; liczone do zamówienia jak potwierdzone | **Zmienić:** widoczne, **nieliczone** do zamówienia do czasu potwierdzenia; zakupowiec może świadomie odblokować linię z podaniem powodu (np. długi LT) |
| 4.4 | Wycena, kursy, ceny | WKLEJ ze stałych zakresów własnego arkusza Excel zakupów, dopasowanie po kolejności; kursy banku przez Power Query; cennik magazynowy z łącza; ceny chronione tylko nazwami kolumn; blokady trybu nie działają | **Zmienić:** import wyceny z mapowaniem kolumn i dopasowaniem po MPN lub numerze linii, niedopasowane do decyzji; kurs waluty zapisany przy wycenie [niepewne: kurs z dnia wyceny — do potwierdzenia]; cennik magazynowy importowany plikiem, docelowo z systemu magazynowego; ceny widzą tylko uprawnione role: **Zakupowiec, Kierownik Logistyki, Kierownik działu Obsługi Projektu** |
| 4.5 | Nadwyżka zakupowa i podsumowanie kosztów | Nadwyżka = (zamówione + cały stan magazynu) − BOM; „Zablokuj wartości” zamraża formuły; rozliczenie łączenia z tekstu FuseDes | **Odłożone — nierozstrzygnięte.** Sekcję dodały zakupy; odpowiedzi zna Kierownik Logistyki (pytania w „Otwarte pytania”) |
| 4.6 | Przekazanie do kompletacji | sPart → cPart przy każdym zamknięciu, tylko PP1; ceny ukryte nazwami kolumn; zapis iPart i cPart przed decyzją o zapisie sPart; Mounting „jednorazowo DNP / zamiennik” chroniony tekstem | **Zmienić:** kompletacja czyta dane zakupowe bezpośrednio, dla wszystkich partii; ceny ukrywają uprawnienia (poza cenami magazyn widzi wszystko); zakupowiec widzi wydania i uwagi magazynu per partia na bieżąco; zapis przez sesję edycji (2.12a); zmiany „tylko dla ZK” = wersja BOM dla ZK. **Uwagi zakupów magazyn obsługuje tak jak zmiany** (do obsłużenia, z potwierdzeniem) |
| — | Administracja dostępów | Brak (hasło w kodzie, tryby pliku) | **Nowe:** moduł administracji dostępów (Usługi wspólne): konkretne dostępy × role, dla każdego: edycja / odczyt / brak dostępu |

## Błędy, których aplikacja nie może powtórzyć (dotychczas)

- Zmiana wielkości liter w danych przekazywanych dalej (comparator).
- Zakresy desygnatorów z literą, zerami, malejące; myślnik w desygnatorze (comparator).
- DNP z fragmentu tekstu (iPart).
- Trzy różne reguły jednej rzeczy (PPM).
- Ciche nadpisywanie danych Klienta (Value) i bazy (Footprint przy zatwierdzeniu P&P).
- Kasowanie zakazów MPN przy zmianie linii; literówka w nazwie arkusza przerywająca operację w połowie.
- Ciche „naprawianie” wpisu użytkownika (ESR: usuwanie separatora dziesiętnego).
- Porównanie tekstu zamiast danych przy łączeniu (podwójna spacja = inny komponent) (fusePart).
- Część wspólna MPN liczona z pakietów, w których komponentu nie ma; gubienie MPN niedozwolonych (fusePart).
- Cała ilość pozycji na liście towaru powierzonego, gdy powierza tylko jeden pakiet (fusePart).
- Klucz ZK bez roku (fusePart).
- Decyzja użytkownika, która po cichu nigdzie nie trafia (statusy MPN: błąd wielkości liter) (sPart).
- Zapis innych plików, zanim użytkownik zdecyduje o zapisie (sPart).
- Ręcznie wpisywany klucz wyszukiwania przy każdej linii (PN magazynu) (sPart).
- Dopasowanie danych po kolejności wierszy (wycena, cPart, iPart) (sPart, fusePart).
- Ochrona danych przez nazwy kolumn zamiast uprawnień (ceny) (sPart).

## Otwarte pytania (dotychczas)

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Czy Pin-In-Paste podlega zatwierdzeniu P&P przez Technologa SMT? | Technolog SMT |
| Format Value dla drabinki rezystorowej | Kierownik działu Obsługi Projektu |
| Pełna tabela reguł zamienników (dielektryki i pozostałe typy) — rozszerzenie | Kierownik działu Obsługi Projektu |
| Lista parametrów zawsze widocznych dla pozostałych typów | Kierownik działu Obsługi Projektu, Opiekunowie Projektu |
| 30–50 przykładowych zapisów Value od różnych Klientów (nauka porównania) | Operatorzy danych BOM |
| Zwalnianie porzuconej blokady edycji BOM (po jakim czasie, kto może) | Kierownik działu Obsługi Projektu |
| Do czego służy nadwyżka i podsumowanie kosztów; czy wliczanie całego stanu magazynu do nadwyżki jest zamierzone; propozycja: nadwyżka tylko z zamówień pod ZK, osobno zapas i nadwyżka ponad zapas, ceny i kursy zapisane przy wycenie, rozliczenie łączenia z ilości per ZK | Kierownik Logistyki |
| Kurs waluty: z dnia wyceny? | Zakupowiec, Kierownik Logistyki |
| Wzór własnego arkusza wyceny zakupów (do mapowania importu) | Zakupowiec |
| Czy import stanu magazynu zawiera dane opakowań (do wyboru ilości z magazynu)? | Magazyn, drugi developer |
| Od którego momentu zmiana w łączeniu wymaga obsłużenia przez zakupowca („było → będzie”)? | Kierownik działu Obsługi Projektu, Zakupowiec |
