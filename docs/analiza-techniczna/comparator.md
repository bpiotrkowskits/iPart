---
status: roboczy
zakres: analiza techniczna mechanizmu porównania BOM/P&P (comparator) na podstawie dostarczonego fragmentu kodu
przegląd: 2026-11-23
---

# comparator — analiza techniczna

Źródło: plik tekstowy z kodem comparatora (ok. 440 linii) — główny mechanizm wczytania i porównania danych. Obsługa ekranów (przyciski akceptacji, eksport do iPart, Wykaz Różnic) nie została dostarczona; autor zdecydował, że będzie budowana od nowa. Dla porównania przejrzano układ pliku wejściowego `BOM_P&P_PCBA1.xlsx` i szablon `Wykaz Różnic Dokumentacyjnych.xlsx`. Porównanie z [wywiadem](../wywiad-obieg-plikow.md).

Oznaczenia: **[zgodne z wywiadem]**, **[różnica względem wywiadu]**, **[nowe — nieomawiane]**, **[niepewne]** — kod tego nie rozstrzyga.

## Najpierw: w czym jest napisany comparator

Kod **nie jest** zapytaniem Power BI ani Power Query. To **Power Apps** — narzędzie Microsoftu do budowania prostych aplikacji z ekranami, przyciskami i listami, w języku formuł **Power Fx** (podobnym do formuł Excela). Dane pobiera **przepływ Power Automate** (automat, który na żądanie aplikacji otwiera plik w chmurze firmowej i zwraca jego zawartość).

- **[różnica względem wywiadu]** W wywiadzie comparator opisano jako rozwiązanie w Power BI. Kod wskazuje na Power Apps + Power Automate. Dla nowej aplikacji nie ma to znaczenia — liczy się logika, opisana niżej.

Kilka pojęć:

- **Kolekcja** — tabela w pamięci aplikacji (jak arkusz, który istnieje tylko podczas pracy).
- **JSON** — tekstowy format przesyłania tabel między programami.
- **Normalizacja** — sprowadzenie tekstu do jednej postaci przed porównaniem (tu: wielkie litery, bez spacji na brzegach).

## 1. Przegląd

| Krok | Co robi |
|---|---|
| Wczytanie | Przepływ „pobranie danych” zwraca tabele BOM i P&P z wybranego pliku. |
| Rozbicie desygnatorów | Każda linia BOM zamienia się w osobne wiersze — jeden na desygnator; opcjonalnie rozwijane zakresy. |
| Dopasowanie | Każdy desygnator BOM szukany w P&P; desygnatory z P&P bez pary w BOM dopisywane osobno. |
| Normalizacja | Wielkie litery i obcięte spacje, z ochroną „m” i „µ”. |
| Porównanie | Kod rozbieżności z liter L, F, D, N. |
| Grupowanie | Desygnatory z powrotem łączone w linie, osobno dla każdej kombinacji wyniku. |
| THT | Oznaczenie linii jako THT według biblioteki elementów THT. |

## 2. Mechanizmy

### 2.1. Wejście

- **Wejście:** użytkownik wybiera z list: folder, projekt, nazwę tabeli BOM i nazwę tabeli P&P. Przepływ zwraca: listę nazw kolumn, tabelę BOM i tabelę P&P (jako JSON).
- **Plik źródłowy:** `BOM_P&P` — dwa arkusze z tabelami Excela **BOM** i **PP** (zgodne z przeglądem pliku przykładowego). Kolumny BOM: Designator, Part TS, Footprint, Description, Quantity, Value, Part Number, Notes, Column1…15. Kolumny P&P: Designator, Part TS, Footprint, Description, X [mm], Y [mm], Layer, Rotation.
- **Reguła:** pole porównywane „Name/Value/Comment” = **Part TS**, a gdy go brak — kolumna „Name/Value/Comment” (starsza nazwa). Z P&P brane są też współrzędne, warstwa i obrót (nieużywane w porównaniu).
- **Kolejność linii:** każda linia BOM dostaje numer **LineID = pozycja w tabeli BOM**.
- **[zgodne z wywiadem]** Stały układ kolumn pliku pośredniego; reguła kolejności linii ma tu swoje źródło (LineID).
- **[niepewne]** Lokalizacja pliku (folder/projekt) i źródło danych (chmura firmowa) wynikają z przepływu, którego kodu nie dostarczono.

### 2.2. Rozbicie list i zakresów desygnatorów

- **Wejście:** komórka Designator z BOM.
- **Reguła:**
  1. Jeśli zaznaczone jest pole **„przedziały”**: znaki `-`, `<`, `>` są zamieniane na znacznik zakresu (`...`), spacje wokół znacznika usuwane.
  2. Spacje zamieniane na przecinki, podwójne przecinki na pojedyncze; podział po przecinku → osobny wiersz na każdy desygnator (z kompletem danych linii).
  3. Zakres `R1...R5`: liczba z pierwszego i ostatniego desygnatora (pierwsza grupa cyfr), prefiks = pierwszy desygnator bez tej liczby; powstają `R1, R2, R3, R4, R5`.
- **Wynik:** jedna kolekcja „jeden desygnator = jeden wiersz”, z LineID linii źródłowej.
- **[zgodne z wywiadem]** Comparator rozbija zakresy i listy desygnatorów.
- **[nowe — nieomawiane]** Rozwijanie zakresów działa **tylko po zaznaczeniu pola „przedziały”**. Bez niego `C1-C5` zostaje jednym „desygnatorem”.
- **[nowe — nieomawiane]** Rozpoznawane separatory list: przecinek i spacja. **Średnik i nowa linia nie są separatorami.**

### 2.3. Dopasowanie BOM ↔ P&P

- **Reguła:**
  1. Dla każdego desygnatora BOM szukany jest **identyczny** desygnator w P&P (P&P z obciętymi spacjami); pobierane: Designator, Footprint, Description, Part TS z P&P.
  2. Desygnatory z P&P, których nie ma w BOM, dopisywane są jako osobne wiersze z pustą stroną BOM (bez LineID).
- **[zgodne z wywiadem]** Porównanie desygnator po desygnatorze; elementy tylko w P&P są wykazywane.
- **[niepewne]** Czy dopasowanie rozróżnia wielkość liter — zależy od zachowania Power Fx; normalizacja (2.4) następuje dopiero po dopasowaniu.

### 2.4. Normalizacja i porównanie

- **Reguła normalizacji:** obcięcie spacji na brzegach i zamiana na wielkie litery — **z wyjątkiem litery „m” i znaku „µ”**, które są chronione (zastępowane znacznikiem i przywracane), żeby `mF`/`MF` lub `µ` nie zlały się w jedno. Dotyczy Designator, Description i Part TS; Footprint — tylko wielkie litery i spacje (bez ochrony „m”). Pozostałe kolumny (Quantity, Value, Part Number, Notes, Column1…15) też są zamieniane na wielkie litery.
- **Reguła porównania — kod „Discrepancies”** (sklejane litery):
  - **L** — brak pary desygnatora (BOM ≠ P&P albo pusty po stronie BOM),
  - **F** — różny Footprint,
  - **D** — różny Description,
  - **N** — różny Part TS (Name/Value/Comment).

  Porównanie jest **dokładne po normalizacji**. Pusta wartość po stronie BOM zawsze daje rozbieżność, nawet gdy P&P też jest puste.
- **Wartości początkowe:** Oznaczenie = „brak”, **Akceptacja = fałsz**, **komponent_klienta = fałsz**.
- **[zgodne z wywiadem]** Porównywane są Designator, Part TS, Footprint, Description — dokładnie te pary kolumn ma Wykaz Różnic Dokumentacyjnych.
- **[nowe — nieomawiane]** Quantity **nie jest** porównywana z liczbą desygnatorów; Value i Part Number nie są porównywane z P&P (P&P ich nie ma).
- **[nowe — nieomawiane]** Pusty Footprint/Description po obu stronach jest liczony jako rozbieżność.

### 2.5. Akceptacja, Confirmed i komponent Klienta

- **Reguła w kodzie:** każdy wiersz startuje z Akceptacja = fałsz i komponent_klienta = fałsz. Kod, który je zmienia (przyciski na ekranie) i który wylicza Confirmed przy eksporcie, **nie został dostarczony**.
- **[niepewne]** Z wywiadu i kodu iPart: Confirmed = brak rozbieżności albo Akceptacja = prawda. W kodzie potwierdzone są tylko pola, z których ta reguła by korzystała.
- **[nowe — nieomawiane]** Pole **komponent_klienta** (powierzenie) istnieje na poziomie każdego wiersza porównania — to najpewniej źródło kolumny powierzenia, którą iPart czyta przy imporcie (w analizie iPart: „kolumna AF”). Opcji „cały BOM powierzony” w tym fragmencie nie ma — byłaby to akcja ekranowa ustawiająca pole dla wszystkich wierszy. [niepewne]

### 2.6. Grupowanie z powrotem w linie

- **Reguła:** wiersze grupowane po LineID **i wszystkich pozostałych polach**, w tym kodzie rozbieżności i polach Akceptacja/komponent_klienta. Desygnatory grupy sklejane przecinkiem (osobno BOM i P&P). Każda grupa dostaje numer kolejny (LineID_mainTable).
- **Wynik:** jedna linia BOM może się **rozpaść na kilka linii**, jeśli jej desygnatory mają różne wyniki porównania (np. część ma inny Footprint w P&P). Wiersze tylko z P&P mają puste LineID.
- **[nowe — nieomawiane]** To mechanizm, dzięki któremu można akceptować rozbieżność dla części desygnatorów linii. Jak te pod-linie wracają do jednej linii w iPart — nie wynika z kodu. [niepewne]

### 2.7. Rozpoznanie THT

- **Wejście:** biblioteka elementów THT (lista MPN/oznaczeń), ładowana osobno (flaga „biblioteka niewczytana” na starcie).
- **Reguła:** jeśli Part TS (BOM lub P&P), Footprint (BOM lub P&P) albo Description (BOM lub P&P), po zamianie na wielkie litery, **dokładnie** równa się pozycji z biblioteki → THT = prawda; dodatkowo zapisywane, w których polach nastąpiło trafienie.
- **[zgodne z wywiadem]** Comparator przekazuje do iPart informację o THT (iPart: kolumna CSV true → THT, false → SMT).
- **[niepewne]** Skąd pochodzi biblioteka THT (np. arkusz thtCase w dataPart?) — nie ma w dostarczonym kodzie.
- **[niepewne] Prawdopodobny błąd:** w opisie trafień (THTdetails) jeden warunek ma źle postawiony nawias — porównanie Part TS z BOM jest wykonywane przed zamianą na wielkie litery, a zamiana dotyczy wyniku porównania. Sam znacznik THT liczony jest poprawnie, więc szczegóły trafień mogą się różnić od znacznika.

### 2.8. Wyjście do iPart i Wykaz Różnic

- **iPart:** iPart importuje plik CSV z comparatora (Confirmed, Designator, Quantity, Name/Value/Comment, Case, Description, Value, Part Number, Notes, kolumna THT, kolumna powierzenia, Column1…15) — opis po stronie iPart w [ipart.md](ipart.md), rozdz. 2.2. Kod zapisu CSV nie został dostarczony.
- **Wykaz Różnic Dokumentacyjnych:** szablon z jednym arkuszem: pary „BOM / P&P” dla Designator, Part TS, Footprint, Description oraz kolumny PYTANIE i ODPOWIEDŹ. Układ odpowiada dokładnie polom porównania. Kod jego wypełnienia nie został dostarczony.
- **Porównanie dwóch BOM-ów** (z wywiadu) — w dostarczonym fragmencie **nie ma** takiego mechanizmu. [niepewne]

## 3. Reguły biznesowe zaszyte w kodzie

1. Porównywany „Part TS” = kolumna Part TS, a w starych plikach Name/Value/Comment.
2. Kolejność linii BOM = LineID; linie tylko z P&P nie mają LineID.
3. Separatory listy desygnatorów: przecinek i spacja; zakresy (`-`, `<`, `>`) tylko na żądanie (pole „przedziały”).
4. Zakres rozwijany numerycznie od pierwszej do ostatniej liczby z prefiksem pierwszego desygnatora.
5. Porównanie: dokładne po normalizacji (wielkie litery, bez spacji na brzegach), z zachowaniem „m” i „µ”.
6. Kod rozbieżności L/F/D/N; pusta strona BOM = rozbieżność.
7. Każdy wiersz ma niezależną Akceptację i znacznik komponentu Klienta.
8. Linia rozpada się na podlinie według wyników porównania desygnatorów.
9. THT = dokładne trafienie w bibliotekę THT w jednym z sześciu pól.

## 4. Odpowiedzi na otwarte pytania z wywiadu

| Pytanie | Odpowiedź z kodu |
|---|---|
| Które pola porównuje comparator i jak dokładnie? | Designator (L), Footprint (F), Description (D), Part TS (N); dokładnie po zamianie na wielkie litery i obcięciu spacji, z ochroną „m” i „µ”. |
| Skąd iPart bierze znacznik powierzenia (kolumna AF)? | Najpewniej z pola komponent_klienta — ustawiany w interfejsie, którego kodu nie ma. [niepewne] |
| „Cały BOM powierzony” | Brak w dostarczonym kodzie. [niepewne] |
| Porównanie dwóch BOM-ów | Brak w dostarczonym kodzie. [niepewne] |

## 5. Ryzyka i słabe punkty

1. **Zakresy z literą na końcu** (np. `C2A-C2D`) — liczba pierwsza i ostatnia są takie same (2), więc powstaje tylko `C2` — pozostałe desygnatory giną. [niepewne — zależy od zapisu w BOM]
2. **Zera wiodące** w zakresie (`R01-R05`) — wynik `R1…R5`, który nie znajdzie pary `R01` w P&P.
3. **Desygnatory z myślnikiem** (np. `J1-1`) przy zaznaczonym polu „przedziały” zostaną potraktowane jak zakres.
4. **Zakres malejący** (`R5-R1`) — kod nie obsługuje (ujemna długość sekwencji).
5. **Średnik / nowa linia** jako separator nie są rozpoznawane.
6. **Brak kontroli Quantity** względem liczby desygnatorów.
7. **Puste pola po obu stronach = rozbieżność** — generuje „fałszywe” rozbieżności dla linii bez Footprint/Description.
8. Prawdopodobny błąd nawiasu w opisie trafień THT. [niepewne]
9. Dane krążą przez JSON w tekście; znaki nowej linii w BOM są zamieniane na spacje (a spacja jest separatorem desygnatorów).

## 6. Pytania do potwierdzenia przez autora

| Pytanie | Miejsce w kodzie |
|---|---|
| Czy comparator to aplikacja Power Apps z przepływem Power Automate (nie Power BI)? | Wczytanie danych |
| Kiedy operator zaznacza pole „przedziały” — zawsze, czy tylko gdy BOM ma zakresy? | Rozbicie desygnatorów |
| Czy zdarzają się zakresy z literą (C2A-C2D), zerami (R01-R05) albo desygnatory z myślnikiem? | Rozwijanie zakresów |
| Skąd pochodzi biblioteka elementów THT? | THT |
| Jak liczony jest Confirmed przy eksporcie: brak rozbieżności LUB akceptacja? | Eksport (kod niedostarczony) |
| Jak ustawiany jest znacznik komponentu Klienta i czy istnieje opcja dla całego BOM? | Ekran (kod niedostarczony) |
| Gdzie jest porównanie dwóch wersji BOM? | — |
| Jak pod-linie powstałe z jednej linii BOM wracają do iPart — jako jedna linia czy kilka? | Grupowanie |

## 7. Współzależności z innymi plikami

| Kierunek | Plik | Arkusz/zakres | Kiedy | Moduł/procedura/zapytanie | Co przenosi |
|---|---|---|---|---|---|
| BOM_P&P → comparator | BOM_P&P (plik pośredni) | tabele BOM i PP | Wczytanie (wybór folderu, projektu, tabel) | przepływ „pobranie danych”, kolekcje dataBOMcol/dataPPcol | Cały BOM i P&P [zgodne z wywiadem] |
| biblioteka THT → comparator | [niepewne] | — | Wczytanie biblioteki | kolekcja elementsTHT | Lista elementów THT [niepewne] |
| comparator → iPart | CSV (wsad do iPart) | — | Eksport (kod niedostarczony) | — | Confirmed, rdzeń BOM, THT, komponent Klienta, Column1…15 [zgodne z wywiadem] |
| comparator → Wykaz Różnic | Wykaz Różnic Dokumentacyjnych | Arkusz1: pary BOM/P&P, PYTANIE, ODPOWIEDŹ | Ręczne wklejenie (wywiad) | — | Rozbieżności L/F/D/N do pytań [zgodne z wywiadem] |
| Wykaz Różnic → iPart | — | ODPOWIEDŹ | Ręcznie, przez Opiekuna Projektu | — | Odpowiedzi Klienta [zgodne z wywiadem] |
