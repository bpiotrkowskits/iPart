---
status: roboczy
zakres: decyzje dotyczące mechanizmów VBA przy przenoszeniu do aplikacji iPart — przegląd w toku (sesja 1 z 4 zakończona)
przegląd: 2026-11-24
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

## Błędy, których aplikacja nie może powtórzyć (dotychczas)

- Zmiana wielkości liter w danych przekazywanych dalej (comparator).
- Zakresy desygnatorów z literą, zerami, malejące; myślnik w desygnatorze (comparator).
- DNP z fragmentu tekstu (iPart).
- Trzy różne reguły jednej rzeczy (PPM).
- Ciche nadpisywanie danych Klienta (Value) i bazy (Footprint przy zatwierdzeniu P&P).
- Kasowanie zakazów MPN przy zmianie linii; literówka w nazwie arkusza przerywająca operację w połowie.
- Ciche „naprawianie” wpisu użytkownika (ESR: usuwanie separatora dziesiętnego).

## Otwarte pytania (dotychczas)

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Czy Pin-In-Paste podlega zatwierdzeniu P&P przez Technologa SMT? | Technolog SMT |
| Format Value dla drabinki rezystorowej | Kierownik działu Obsługi Projektu |
| Pełna tabela reguł zamienników (dielektryki i pozostałe typy) — rozszerzenie | Kierownik działu Obsługi Projektu |
| Lista parametrów zawsze widocznych dla pozostałych typów | Kierownik działu Obsługi Projektu, Opiekunowie Projektu |
| 30–50 przykładowych zapisów Value od różnych Klientów (nauka porównania) | Operatorzy danych BOM |
| Zwalnianie porzuconej blokady edycji BOM (po jakim czasie, kto może) | Kierownik działu Obsługi Projektu |
