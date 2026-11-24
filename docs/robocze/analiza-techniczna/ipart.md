---
status: roboczy
zakres: analiza techniczna kodu VBA pliku iPart (rev3.3)
przegląd: 2026-11-23
---

# iPart — analiza techniczna kodu VBA

Źródło: kod VBA wyciągnięty (tylko do odczytu) z `iPart.xlsm` — 37 modułów, ok. 9 300 linii, oraz układ przycisków na arkuszach. Dokument porównuje kod z ustaleniami z wywiadu ([Wywiad obieg plików](../Wywiad%20obieg%20plików.md)).

Oznaczenia: **[zgodne z wywiadem]**, **[różnica względem wywiadu]**, **[nowe — nieomawiane]**, **[niepewne]** — kod tego nie rozstrzyga.

Kilka pojęć, które będą się powtarzać:

- **Makro / procedura** — nazwany fragment kodu VBA uruchamiany przyciskiem albo automatycznie.
- **Zdarzenie** — sytuacja, w której Excel sam uruchamia kod, np. otwarcie pliku (`Workbook_Open`) albo zapis (`Workbook_BeforeSave`).
- **Słownik (Dictionary)** — struktura w pamięci, która pozwala szybko znaleźć wiersz po kluczu (np. po MPN), bez przeszukiwania arkusza za każdym razem.
- **Normalizacja** — sprowadzenie tekstu do jednej postaci przed porównaniem (np. wielkie litery, usunięte spacje), żeby „abc-1” i „ABC 1” były traktowane jak to samo.
- **Twarda ścieżka / twardy numer kolumny** — wartość wpisana na stałe w kod, która przestaje działać, gdy coś się przesunie.

## 1. Przegląd modułów

### Import

| Moduł | Rola |
|---|---|
| `iS_p1_Import` | Przycisk „Importuj Dokumentację”: kolejno import BOM, kopia zapasowa BOM, import P&P. |
| `iS_p1A_ImportBOM` | Wczytuje plik CSV z comparatora do arkusza BOM; ustala Confirmed, montaż (PCB/DNP/THT/SMT) i znacznik powierzenia. |
| `iS_p1B_backupBOM` | Tworzy ukryty arkusz `BOM_backup_<data>_<inicjały>` i zwiększa licznik „Rewizja Importu”. |
| `iS_p1C_ImportPP` | Wczytuje CSV P&P do bloku „WSAD P&P” i dopisuje desygnatory z BOM, których brakuje w P&P. |
| `iS_p2_ImportOld` | Pusty moduł (właściwy import ze starej wersji jest w `A_BackwardUpdate`). |

### Kroki opracowania BOM (`iB_p…`)

Numeracja modułów różni się od numeracji przycisków na arkuszu BOM — poniżej podano obie.

| Moduł | Przycisk na arkuszu BOM | Rola |
|---|---|---|
| `iB_p1_RozkodujPN` | 1. Rozkoduj Part Numer | Rozdziela kilka MPN z komórki, przypisuje Value z dataPart, dopisuje nowe MPN do dataPart. |
| `iB_p3_Parametry` | 2. Przypisz Parametry | Formularze parametrów RC dla MPN bez kompletu danych; buduje Value; zapisuje do dataPart. |
| `iB_p5_Case` | 3. Standaryzuj Obudowy | Ustala Case, Footprint i Mounting z dataPart lub formularzem; zapisuje Case/Mounting do dataPart. |
| `iB_p4_PartTS` | 4. Systematyzuj Part TS | Wylicza Part TS dla zaznaczonych wierszy. |
| `iB_p2_Zamienniki` | 5. Wylistuj Zamienniki | Dopisuje MPN zatwierdzone na podstawie Part Number i dopasowania Value + Case w dataPart. |
| `iB_p6_ZatwierdzenieBOM` | Zatwierdź BOM do procesów logistycznych | Zatwierdza cały iPart (data + login) i zmienia nazwę pliku. |
| `iB_p7_Powierzone` | Wygeneruj Listę Komponentów Powierzonych | Tworzy plik entPart/powPart dla Klienta z ilościami z zapasem. |
| `iB_p0_uproszczona` | Procedura Uproszczona | Part TS + obudowy + zmiana nazwy pliku. [niepewne] przycisk wydaje się podpięty pod zatwierdzenie BOM, nie pod tę procedurę. |
| `iB_p8_exportBOM` | Wygeneruj BOM wyjściowy | Procedura `ExportDoKlienta` jest pusta. [niepewne] przycisk wydaje się podpięty pod zatwierdzenie BOM. |

### Zatwierdzenie P&P

| Moduł | Przycisk | Rola |
|---|---|---|
| `iP_p1_ZatwierdzeniePP` | Zatwierdź do Produkcji (arkusz P&P) | Zatwierdza P&P (data + login) i zapisuje Footprinty z BOM do dataPart jako zatwierdzone. |

### Mechanizmy automatyczne i wersje pliku (`A_…`)

| Moduł | Rola |
|---|---|
| `Ten_skoroszyt` | Zdarzenia: przy otwarciu — sprawdzenie wersji szablonu; przy zapisie — historia zmian, odświeżenie zamienników, logi MPN, ewentualne wydanie nowej wersji szablonu. |
| `A_HistoriaZmian` | Porównuje BOM z ostatnią kopią zapasową i zapisuje każdą różnicę w arkuszu „Historia Zmian”. |
| `A_SubstUpdate` | Po wykryciu zmian Part TS/Case/Value/Part Number proponuje ponowne przypisanie zamienników dla zmienionych wierszy. |
| `A_LogMPN` | Dla każdego nowego MPN w arkuszach MPN zatwierdzone/niedozwolone wpisuje do logu login i datę. |
| `A_ForwardUpdate` | Przy otwarciu: jeśli plik ma starszą wersję szablonu niż w folderze MASTER, przenosi dane do nowego szablonu. |
| `A_MasterUpdate` | Wydanie nowej wersji szablonu (rev i znacznik czasu) — tylko w folderze MASTER i tylko dla autora. |
| `A_BackwardUpdate` | Przycisk „Importuj nieaktualną wersję iPart”: przenosi dane ze starego iPart (v1/v2) do nowego szablonu v3. |
| `A_User` | Pomocnicze okienko pokazujące login Windows. |

### Formularze

| Formularz | Rola |
|---|---|
| `frRES` | Parametry rezystora: rezystancja, tolerancja, moc, AEC-Q200, PPM, obudowa. |
| `frCAP` | Parametry kondensatora ceramicznego: pojemność, napięcie, dielektryk, tolerancja, AEC-Q200, obudowa. |
| `frCTCE` | Kondensator tantalowy/elektrolityczny: pojemność, napięcie, tolerancja, ESR, lowESR, AEC-Q200, obudowa. |
| `frCFM` | Case / Footprint / Mounting dla jednej linii BOM. |

### Funkcje i stałe wspólne

| Moduł | Rola |
|---|---|
| `Public_Const` | Nazwy arkuszy i kolumn, ścieżka sieciowa do dataPart, hasła do arkuszy (stałe w kodzie). |
| `Public_Function` | Normalizacja MPN, Case i wartości; wyszukiwanie kolumn po nagłówku; otwieranie dataPart; odczyt Klienta z folderu. |
| `Arkusz1…8` | Moduły arkuszy — puste (brak kodu reagującego na zmiany w komórkach). |

## 2. Mechanizmy

### 2.1. Otwarcie pliku — kontrola wersji szablonu

- **Wejście:** START!C1 (nazwa szablonu), D1 (rev), E1 (znacznik czasu); folder MASTER na dysku sieciowym.
- **Reguła:**
  1. Z C1–E1 budowana jest oczekiwana nazwa szablonu, np. „iPart — rev3.3 — 20260324_2351.xlsm”.
  2. Jeśli taki plik istnieje w folderze MASTER — nic się nie dzieje.
  3. Jeśli nie — szukany jest najnowszy (wg daty pliku) szablon o tej samej nazwie bazowej. Po zgodzie użytkownika: kopia szablonu → usunięcie z niej arkuszy → skopiowanie **wszystkich arkuszy z pliku roboczego** → poprawa odwołań przycisków i formuł → wpis nowej rev i znacznika czasu.
  4. Stary plik trafia do podfolderu `z_NIEAKTUALNE` z dopiskiem daty, nowy zapisuje się pod starą nazwą.
- **Wynik:** plik z nowymi makrami i dotychczasowymi danymi.
- **Moduł:** `Ten_skoroszyt.Workbook_Open` → `A_ForwardUpdate.CheckAndUpdateForward`.
- **[nowe — nieomawiane]** To jest mechanizm rozwiązujący problem „makra w każdym pliku” z wywiadu: przy otwarciu plik sam przechodzi na nowe makra. Ponieważ arkusze kopiowane są z pliku roboczego, aktualizują się **tylko makra**, a nie układ arkuszy. [niepewne] Jak dziś wprowadzane są zmiany układu arkuszy (nowe kolumny)?

### 2.2. Import dokumentacji (BOM i P&P)

**Import BOM**

- **Wejście:** plik CSV wskazany w START!D16 (domyślnie podfolder „Analiza Dokumentacji\4. Pliki Wsadowe do iPart”). Wszystkie kolumny czytane jako tekst, kodowanie UTF-8, pomijany pierwszy wiersz.
- **Reguła:**
  1. Czyszczony jest stary BOM (B10:AC…) i komórki zatwierdzenia N1:N4.
  2. Kolumny CSV są przepisywane do stałych kolumn BOM (twarde mapowanie): A→Confirmed, B→Designator, C→Quantity, D→Name/Value/Comment (później Part TS), E→Case, F→Description, G→Value, H→Part Number, J→Notes, od L dalej → od kolumny O dalej. Footprint dostaje formułę „= Case”.
  3. **Confirmed:** tekst true/prawda → „true” i wpis w kolumnie N „Conf.: <2 litery loginu>[data]”; false/fałsz → „false”.
  4. **Mounting:** kolumna B CSV = „pcb” → PCB (i dopisek „PCB ” w Part TS); słowo DNP na początku lub końcu Part TS, Value albo Part Number → DNP; kolumna I CSV true → THT, false → SMT.
  5. **Słowa uznawane za DNP:** dnp, dnm, dni, nm, ni — gdy stoją na brzegu tekstu (ignorowane są znaki ` _/\-()[]`).
  6. **Powierzenie:** kolumna AF CSV zawiera „true” → w EntPart wpisywana jest nazwa z kolumny „EntPart” CSV, a gdy jej brak — znak ☑; gdy AF zawiera i true, i false → „?”.
- **Wynik:** arkusz BOM od wiersza 11, START!F14 = data importu.
- **Moduł:** `iS_p1A_ImportBOM.ImportBOM`.
- **[zgodne z wywiadem]** Comparator przekazuje Confirmed oraz informację o THT i powierzeniu.
- **[nowe — nieomawiane]** Automatyczne rozpoznawanie PCB i DNP po słowach kluczowych.
- **[niepewne]** „Cały BOM powierzony” z wywiadu nie istnieje w iPart jako opcja. Do iPart trafia tylko znacznik per linia z kolumny AF. W kodzie comparatora nie znaleziono tej kolumny — skąd się bierze?

**Kopia zapasowa BOM**

- **Reguła:** arkusz BOM jest kopiowany do ukrytego arkusza `BOM_backup_<ddmmrr>_<2 litery loginu>` (przy powtórzeniu nazwy dopisywany jest numer). W BOM i kopii: D1 = data, D2 = login, **D3 = licznik +1**.
- **Wynik:** punkt odniesienia dla historii zmian.
- **Moduł:** `iS_p1B_backupBOM.BackupBOM`.
- **[zgodne z wywiadem]** D3 to „Rewizja Importu” — rośnie przy każdym imporcie nowego BOM dla tego samego pakietu. D2 to „Zaimportował”.

**Import P&P**

- **Wejście:** CSV wskazany w START!D23.
- **Reguła:** dane wklejane do bloku „WSAD P&P” (kolumny AA i dalej). Następnie każdy desygnator z kolumny Designator w BOM (rozdzielony po przecinku), którego nie ma w P&P, jest **dopisywany na końcu** — bez współrzędnych.
- **Moduł:** `iS_p1C_ImportPP.ImportPP`.
- **[zgodne z wywiadem]** Element z BOM bez współrzędnych trafia na koniec P&P.
- **[różnica względem wywiadu]** Dopisywane są **wszystkie** brakujące desygnatory, także THT, mechaniczne i DNP — nie tylko SMT.
- **[niepewne]** Lewa część arkusza P&P (Designator, X, Y, Verified…) jest liczona formułami z bloku WSAD; kolumny „Verified” nie ma w kodzie.

### 2.3. Krok 1 — Rozkoduj Part Number

- **Wejście:** arkusz „MPN zatwierdzone” (kolumna Part Number), BOM (Value), dataPart: partsData (Value, defValue, flagValue) i resValue (PPM, tolerancja).
- **Reguła:**
  1. Użytkownik podaje **separator MPN** (dowolny ciąg znaków, np. „ / ”).
  2. Part Number każdej linii jest dzielony separatorem. Każdy nowy MPN trafia do kolejnej kolumny od O w arkuszu „MPN zatwierdzone”, a do logu wpisywane jest „BOM [data]”. **Pierwszy MPN zastępuje zawartość Part Number w BOM.**
  3. Dla każdego MPN szukany jest wiersz w partsData (po znormalizowanym MPN):
     - jest Value → używane Value; brak Value, jest defValue → defValue; brak obu → Value z BOM, która zostaje też wpisana do defValue w dataPart;
     - MPN-a nie ma w bazie, a desygnator zaczyna się od C, CE, CT lub R → **nowy wiersz w partsData** z defValue z BOM i flagą „|” (niezatwierdzone);
     - inne desygnatory nie są dopisywane do bazy.
  4. Dla rezystorów z PPM w resValue dopisywany jest dopisek `_<n>ppm` (reguła w punkcie 3 rozdziału „Reguły biznesowe”). Użytkownik może wymusić PPM dla wszystkich rezystorów.
  5. Jeśli MPN-y jednej linii dają różne wartości → Value = „?wartość1 | wartość2”.
- **Wynik:** Value w BOM, lista MPN w „MPN zatwierdzone”, nowe wiersze i defValue w dataPart (plik zapisywany).
- **Moduł:** `iB_p1_RozkodujPN.RozkodujPartNumber_Fast`.
- **[zgodne z wywiadem]** Krok rozdziela kilka MPN z jednej komórki (pytanie otwarte 4 rozstrzygnięte) i pobiera Value z dataPart.
- **[nowe — nieomawiane]** Separator podaje się ręcznie przy każdym uruchomieniu. Value z BOM Klienta jest nadpisywana wartością z dataPart bez ostrzeżenia, gdy obie się różnią (oryginał zostaje w kolumnie Name/Value/Comment do czasu Part TS oraz w kopii zapasowej).

### 2.4. Krok 2 — Przypisz Parametry

- **Wejście:** aktywny arkusz (BOM), dataPart: partsData, resValue, capValue, ctceValue.
- **Reguła:**
  1. Przetwarzane są tylko desygnatory C…, CE…, CT…, R…, od zaznaczonego wiersza do końca, potem od początku.
  2. Typ elementu: najpierw z bazy (jeśli MPN jest w ctceValue → tantal/elektrolit; w resValue → rezystor; w capValue → kondensator), w przeciwnym razie z desygnatora (C → kondensator, CE/CT → tantal/elektrolit, R → rezystor).
  3. Formularz pojawia się, gdy: MPN-a nie ma w partsData **albo** flagValue = „|” lub „?” **albo** defValue puste **albo** brakuje parametrów (rezystor: rezystancja; kondensator: pojemność i napięcie; tantal: pojemność, napięcie oraz ESR lub lowESR).
  4. Pusty Part Number → formularz służy tylko do opisu linii; nic nie trafia do bazy.
  5. Formularz ma przyciski: **Przypisz** (OK), **Przypisz i zatwierdź** (CONFIRM), Pomiń, Anuluj oraz przełączniki na inny typ formularza.
  6. Po OK/CONFIRM: Value i Case w BOM z formularza; parametry do odpowiedniego arkusza dataPart (tylko pola niepuste — puste pola nie kasują starych danych); nowe MPN dopisywane do partsData z defValue i flagą:
     - OK → flaga „|” (**dopisz bez zatwierdzenia**),
     - CONFIRM → flaga „<login> [data]” (**dopisz z zatwierdzeniem**).
  7. Gdy MPN już był w bazie z flagą „|”/„?” lub pustym defValue → defValue nadpisywane; przy CONFIRM stara flaga i wartość przesuwane do historii Tag1 (pozostałe Tagi przesuwają się w prawo).
- **Wynik:** Value/Case w BOM, dane w dataPart (plik zapisywany).
- **Moduł:** `iB_p3_Parametry.PrzypiszParametry` + formularze `frRES`, `frCAP`, `frCTCE`.
- **[zgodne z wywiadem]** Tryby „dopisz z zatwierdzeniem / bez zatwierdzenia”; Tagi jako historia zmian.
- **[nowe — nieomawiane]** Flaga „?” (do weryfikacji) jest przez kod tylko odczytywana — żadne makro iPart jej nie ustawia. [niepewne] Kto i kiedy ją wpisuje?

### 2.5. Krok 3 — Standaryzuj Obudowy

- **Wejście:** BOM (Part Number, Case, Footprint, Mounting), partsData (Case, flagCase, Footprint lub rmFootprint, flagFootprint, Mounting, Tag1).
- **Reguła:**
  1. Pomijane linie DNP, PCB i bez Part Number. Part Number dzielony separatorem podanym przez użytkownika.
  2. MPN-a nie ma w partsData → **od razu dopisywany** z Case, Footprint i Mounting z BOM oraz flagami „|”.
  3. **Automatyczne przypisanie bez formularza:** linia ma jeden MPN, jego Case w bazie jest zatwierdzony (flagCase ≠ puste i ≠ „|”), a Mounting w bazie = Mounting w BOM → Case i Mounting z bazy; Footprint z bazy tylko gdy flagFootprint też zatwierdzony.
  4. W pozostałych przypadkach formularz `frCFM` pokazuje porównanie BOM ↔ baza i listę możliwych Case. Jeśli którykolwiek Footprint jest niezatwierdzony, **Footprint podąża za Case** (pole tylko do odczytu).
  5. Normalizacja Case: usunięcie spacji i znaków `/ \ ; : ,`, wielkie litery, „x” między cyframi jako małe (np. `3x3`).
  6. Mounting z listy: SMT, THT, MECH, PressFit, Pin-In-Paste, Bezpośrednio do Klienta, Materiały Dodatkowe.
  7. Po „Przypisz” / „Przypisz z zatwierdzeniem obudowy”: BOM aktualizowany; w partsData dla każdego MPN linii Case, Mounting i flagCase („|” lub „<login> [data]”), poprzedni stan do Tag1 jako „Prev: <montaż>:<case> (<flaga>)”. **Footprint z formularza nie jest zapisywany do bazy** — robi to dopiero zatwierdzenie P&P.
  8. Dla jednego wyróżnionego Klienta (nazwa wpisana w kod) Footprint czytany jest z osobnej kolumny rmFootprint.
- **Moduł:** `iB_p5_Case.ZsynchronizujFootprinty_Fast` + `frCFM`.
- **[zgodne z wywiadem]** Case i montaż z dataPart, porównanie montażu, dopisywanie brakujących komponentów.
- **[nowe — nieomawiane]** Wartości montażu „Bezpośrednio do Klienta” i „Materiały Dodatkowe” (pokrywają się z ideą „sposobu wydania” z cPart). Reguła Footprintu specyficzna dla Klienta.

### 2.6. Krok 4 — Systematyzuj Part TS

- **Wejście:** zaznaczone wiersze BOM (Designator, Value, Part Number).
- **Reguła:**
  1. Desygnator C…, CE…, CT…, R… → Part TS = **Value**, a gdy Value puste — Part Number.
  2. Pozostałe desygnatory → Part TS = **Part Number** (gdy pusty — zostaje dotychczasowa treść).
  3. Dla jednego wyróżnionego Klienta (nazwa wpisana w kod) MPN jest obowiązkowy także dla RC — zawsze Part Number.
  4. Gdy przetworzono co najmniej 50,01% wierszy, nagłówek kolumny E zmienia się z „Name/Value/Comment” na „Part TS”.
- **Moduł:** `iB_p4_PartTS.Systematyzuj_PartTS`.
- **[zgodne z wywiadem]** Part TS = Part Number dla aktywnych, parametry dla RC; ustalane automatycznie, człowiek weryfikuje.
- **[nowe — nieomawiane]** „RC” w kodzie to wyłącznie desygnatory C, CE, CT, R — nie typ komponentu w bazie. Np. rezystor oznaczony „RN1” albo kondensator „CX1” spełnia warunek, ale dławik „L1” już nie.

### 2.7. Krok 5 — Wylistuj Zamienniki

- **Wejście:** „MPN zatwierdzone” (Part Number, Value, Case), partsData (Part Number, Value, Case).
- **Reguła:**
  1. Ten sam podział Part Number separatorem co w kroku 1 (MPN-y z BOM, log „BOM [data]”).
  2. Z partsData budowana jest mapa **Value + Case → lista MPN**. Każdy MPN z bazy o identycznym (po normalizacji: wielkie litery, przecinek→kropka, pojedyncze spacje) Value i Case jest dopisywany jako zamiennik z logiem „parameters | BOM [data]”.
  3. Nic nie jest usuwane — dopisywane są tylko nowe MPN.
- **Moduł:** `iB_p2_Zamienniki.MatchyMatchy`.
- **[zgodne z wywiadem]** Źródła MPN zatwierdzonych 1 i 2 (z Part Number i z parametrów).
- **[różnica względem wywiadu]** Zamienniki dobierane są tylko po zgodności tekstu Value i Case — nie po parametrach liczbowych (np. tolerancji ≤, mocy ≥). Wpisy od zakupowca (źródło 4) nie pochodzą z kodu iPart. [niepewne] Czy dopisuje je sPart bezpośrednio do pliku iPart, czy ktoś przepisuje ręcznie?

### 2.8. Zatwierdzenie BOM

- **Wejście:** BOM!E10 (nagłówek), BOM!N1.
- **Reguła:** jeśli nagłówek E10 ≠ „Part TS” → blokada „Kolumna Part TS nie została usystematyzowana”. Jeśli N1 puste → N1 = data, N2 = login; potem zmiana nazwy pliku na `<projekt>_iPart.xlsm` (Klient i projekt odczytywane ze struktury folderów: dwa poziomy nad folderem dokumentacji firmy → Klient, jeden → projekt) i usunięcie pliku pod starą nazwą.
- **Wynik:** iPart zatwierdzony „do procesów logistycznych”; od tej chwili zmiany dostają numery w historii.
- **Moduł:** `iB_p6_ZatwierdzenieBOM.Confirmation`, `NowaNazwaPliku`.
- **[różnica względem wywiadu]** W wywiadzie pola „Data zatwierdzenia / Opiekun Projektu” opisano jako osobę odpowiedzialną za projekt. W kodzie to **login osoby, która kliknęła „Zatwierdź BOM”**. Zatwierdzenie jest jednorazowe dla całego pliku — nie ma cofnięcia.

### 2.9. Zatwierdzenie P&P (Technolog)

- **Wejście:** P&P!N1, BOM (Part Number, Footprint), partsData (Footprint lub rmFootprint, flagFootprint, Tagi).
- **Reguła:**
  1. Jeśli P&P!N1 niepuste → komunikat „już zatwierdzony”.
  2. Po potwierdzeniu i podaniu separatora: dla **każdej** linii BOM z Part Number i każdego MPN — gdy Footprint w bazie różni się od Footprint w BOM, stary trafia do Tag1, nowy jest zapisywany, a flagFootprint = „<login> [data]”. MPN-a nie ma w bazie → nowy wiersz z Footprint i flagą zatwierdzenia.
  3. P&P!N1 = data, N2 = login.
- **Moduł:** `iP_p1_ZatwierdzeniePP.EksportujFootprinty` (przycisk „Zatwierdź do Produkcji”).
- **[zgodne z wywiadem]** Technolog zatwierdza Footprint w dataPart.
- **[nowe — nieomawiane]** Zatwierdzenie P&P **jednocześnie zatwierdza Footprinty w bazie dla całego BOM**. Kod nie sprawdza montażu, więc obejmuje też linie DNP i THT z Part Number. Blokada „bez zatwierdzenia P&P nie można zakończyć kompletacji” nie jest w kodzie iPart — [niepewne] jest prawdopodobnie w cPart (czyta P&P!N1).

### 2.10. Lista komponentów powierzonych (entPart)

- **Wejście:** numer ZK i liczba pakietów (okienka), BOM (EntPart, Quantity, Case, Mounting), plik listy Klientów (kraj), szablony entPart/powPart, scrapPart arkusz `entScrap`.
- **Reguła:**
  1. Klient i projekt z folderów. Jeśli kod kraju Klienta na liście zaczyna się od „PL” → szablon polski „Powierzany_…”, w przeciwnym razie angielski „Entrusted_…”.
  2. Plik zapisywany w centralnym folderze „III. Dokumentacja Do Zamówienia\<numer ZK>\”.
  3. Kopiowane są linie, w których EntPart jest **cokolwiek** wpisane (☑, „?”, nazwa).
  4. Kolumny wyniku dopasowywane po nagłówkach (A–Z), poza kolumną J.
  5. Kolumna J = ilość z zapasem (wzór w rozdziale „Reguły biznesowe”).
  6. Z kolumny Note/Uwagi usuwany tekst „Dodane Automatycznie”.
- **Moduł:** `iB_p7_Powierzone.entPart`.
- **[zgodne z wywiadem]** Generowane z iPart na podstawie kolumny EntPart.
- **[różnica względem wywiadu]** Arkusz zapasu w scrapPart nazywa się w kodzie `entScrap`, nie `entPart`. Linia oznaczona „?” (do potwierdzenia) też trafia do listy.

### 2.11. Zapis pliku — historia zmian, zamienniki, logi

Przy **każdym** zapisie (`Workbook_BeforeSave`), kolejno:

1. **Blokada MASTER:** w folderze MASTER zapis jest dozwolony tylko dla jednego loginu wpisanego w kod (autora). Pozostali dostają komunikat i zapis jest anulowany.
2. **Historia zmian** (`A_HistoriaZmian.RaportZmian`):
   - Wybierana jest „najnowsza” kopia `BOM_backup_…`.
   - Kolumny A–M BOM są porównywane z kopią **wiersz po wierszu, według pozycji** (wiersz 15 z wierszem 15).
   - Każda różnica = wpis w arkuszu „Historia Zmian” (ID, nr pozycji, kolumna, poprzednia wartość, nowa, login, data, ID kopii); nowe wpisy na górze.
   - **Zmiany Footprint są zapisywane bez numeru ID.**
   - Confirmed false→true: w kolumnie N dopisywane „Conf.: <inicjały>[data]”, tło komórki czyszczone. true→false: na początku N „ANULOWANO POTWIERDZENIE: <inicjały>[data]” i **czerwone tło**.
   - Tylko gdy iPart jest zatwierdzony (N1): numery zmian trafiają na początek kolumny N jako `ID_Zmian[5,6,7]`. Przed zatwierdzeniem numery w kolumnie A historii są czyszczone.
   - N9 = „LastUpdate: <inicjały>[data]”; w kopii zapasowej (AZ i BA:BM) zapis, które pozycje zmieniły się w każdej kolumnie.
   - Na koniec **kopia zapasowa jest nadpisywana bieżącym BOM** — kolejny zapis porównuje już do nowego stanu.
3. **Odświeżenie zamienników** (`A_SubstUpdate`): jeśli zmieniły się Part TS, Case, Value lub Part Number — pytanie, czy zresetować zamienniki dla tych wierszy. Po zgodzie: czyszczenie MPN zatwierdzonych **i niedozwolonych** (z logami) od kolumny O, potem ponowny podział Part Number i dopasowanie Value + Case.
4. **Logi MPN** (`A_LogMPN`): każdy niepusty MPN w arkuszach zatwierdzonych/niedozwolonych bez wpisu w logu dostaje „<login> [data]”.
5. **Wydanie szablonu** (`A_MasterUpdate`) — tylko w folderze MASTER dla autora: pytanie o nowy numer minor/major, zapis jako „iPart — revX.Y — rrrrmmdd_ggmm.xlsm” i kopia do folderu kopii zapasowych.

- **[zgodne z wywiadem]** Automatyczna historia zmian; „ANULOWANO POTWIERDZENIE”; log kto/kiedy dopisał MPN.
- **[różnica względem wywiadu]** Format `[#, #, #] [#, #]` z wywiadu nie występuje w iPart. W kolumnie N widać **tylko numery z ostatniego zapisu** w postaci `ID_Zmian[…]` — poprzednie są usuwane z komórki (zostają w arkuszu „Historia Zmian”). [niepewne] Format z nawiasami może dotyczyć sPart/cPart.
- **[różnica względem wywiadu]** Log MPN zapisuje **kto i kiedy**, ale nie „na jakiej podstawie”. Podstawę widać tylko pośrednio: „BOM [data]” lub „parameters | BOM [data]”.
- **[nowe — nieomawiane]** Odświeżenie zamienników kasuje także MPN niedozwolone i ręcznie dopisane MPN zatwierdzone (np. od Klienta) dla zmienionych wierszy.

### 2.12. Import ze starej wersji iPart

- **Wejście:** wybrany plik starego iPart (v1/v2).
- **Reguła:** blokada, jeśli nowy plik leży w folderze szablonów lub kopii zapasowych. Kopia starego pliku do `z_NIEAKTUALNE`. Przeniesienie: START (ścieżki, komentarze), BOM (mapowanie po nagłówkach — dawna kolumna **Footprint staje się Case**, Footprint dostaje formułę = Case), P&P (blok WSAD), „Raport Zmian” → „Historia Zmian” (z zamianą „Footprint” na „Case”), MPN, wszystkie kopie zapasowe. Uwagi „komponent powierzany” → EntPart ☑, „komponent powierzany (do potwierdzenia)” → „?”. Zapis jako `<projekt>_iPart.xlsm` w folderze starego pliku.
- **Moduł:** `A_BackwardUpdate.ImportujDaneZeStarego_iPart` (przycisk „Importuj nieaktualną wersję iPart”).
- **[zgodne z wywiadem]** Rewizja Importu miała znaczenie przy przenoszeniu v1/v2 → v3.
- **[nowe — nieomawiane]** W starych wersjach powierzenie było zapisywane tekstem w Notes, a Footprint znaczył to, co dziś Case.

### 2.13. Kierunki przepływu danych

Nazwy modułów `A_ForwardUpdate` i `A_BackwardUpdate` sugerują wymianę danych z innymi plikami, ale dotyczą **wersji szablonu iPart**, nie przepływu do sPart/cPart.

| Kierunek | Co | Gdzie w kodzie |
|---|---|---|
| comparator → iPart | CSV BOM i P&P | Import |
| dataPart → iPart | Value, Case, Footprint, Mounting, parametry, flagi | Kroki 1–5 |
| iPart → dataPart | nowe MPN, defValue, parametry RES/CAP/CTCE, Case, Mounting, Footprint, flagi, Tagi | Kroki 1–3, zatwierdzenie P&P |
| scrapPart → iPart | zapas dla powierzanych (`entScrap`) | entPart |
| iPart → plik dla Klienta | lista powierzonych | entPart |
| MASTER ↔ iPart | nowe makra / wydanie szablonu | A_ForwardUpdate, A_MasterUpdate |
| iPart → sPart / fusePart / cPart | **brak kodu w iPart** — te pliki same czytają iPart | — |
| sPart → iPart (zamienniki) | **brak kodu w iPart** | [niepewne] |

### 2.14. Formularze (UserForm)

**UserForm** to okienko dialogowe zaprojektowane w edytorze VBA. Opis powstał z kodu formularzy oraz z napisów wyciągniętych z ich projektu. Napisy są częściowo uszkodzone, dlatego podpisy części pól i przycisków podano na podstawie komentarzy w kodzie — są oznaczone [niepewne]. Wszystkie formularze używają czcionki Tahoma.

Wspólne cechy formularzy parametrów (`frRES`, `frCAP`, `frCTCE`):

- **Pole informacyjne** na górze (kontrolka `Label1`) — pokazuje kolumny A–K bieżącego wiersza BOM, każdą w osobnej linii, żeby użytkownik widział, co opisuje.
- **Pola liczbowe** przyjmują tylko cyfry i jeden separator dziesiętny (przecinek zamieniany na kropkę). Po wyjściu z pola wartość jest obcinana do zakresu, np. tolerancja 0–100.
- **Listy rozwijane** wymagają wyboru wartości z listy — wpisanie innej daje komunikat „Wybierz wartość z listy.”.
- **Przyciski:**

  | Przycisk | Skutek |
  |---|---|
  | PRZYPISZ | Value i Case do BOM, parametry do dataPart, nowe MPN z flagą „\|” (niezatwierdzone). |
  | PRZYPISZ Z ZATWIERDZENIEM PARAMETRÓW | Jak wyżej, ale flaga = „<login> [data]” (zatwierdzone). |
  | Pomiń [niepewne — podpis z komentarza] | Linia bez zmian, przejście do następnej. |
  | Anuluj [niepewne] | Koniec przetwarzania; to, co już zebrano, zostaje zapisane do dataPart. |
  | Dwa przyciski przełączenia typu [niepewne] | Otwierają formularz innego typu dla tej samej linii (np. z rezystora na kondensator). |

- **Wynik:** tekst Value budowany z pól (format w rozdziale 3, punkty 1–3) trafia do kolumny Value w BOM, wybrana obudowa — do Case w BOM. Parametry zapisywane są osobno do arkusza resValue / capValue / ctceValue w dataPart (tylko pola niepuste).
- **Ważne:** „zatwierdzenie parametrów” ustawia wyłącznie flagę Value (flagValue) w partsData. Arkusze resValue/capValue/ctceValue nie mają własnego znacznika zatwierdzenia.

#### frRES — rezystor

- **Kiedy:** krok 2, desygnator R… (albo MPN znaleziony w resValue), gdy brakuje danych w bazie.
- **Pola:**

  | Podpis | Kontrolka | Wartości |
  |---|---|---|
  | Obudowa | lista | 0201R, 0402R, 0603R, 0805R, 1206R, 1210R, 2010R, 2512R, 0804R_RAC, 1206R_RAC |
  | Rezystancja | pole liczbowe (0–1000) + lista jednostek | R, k, M, mR |
  | Tolerancja | pole liczbowe (0–100) + lista | tylko „%” |
  | Moc | pole liczbowe (0–1000) + lista jednostek | mW, W |
  | PPM | pole liczbowe (0–1000) | — |
  | AEC-Q200 | pole wyboru (tak/nie) | — |

- **Szczególne:** rezystancja 0 → Value „0R_<moc>”, tolerancja pomijana. PPM trafia do Value tylko wg reguły z rozdziału 3, punkt 4 (zawsze w resValue, jeśli wpisane).

#### frCAP — kondensator ceramiczny

- **Kiedy:** krok 2, desygnator C… (albo MPN w capValue).
- **Pola:**

  | Podpis | Kontrolka | Wartości |
  |---|---|---|
  | Obudowa | lista | 0201C, 0402C, 0603C, 0805C, 1206C, 1210C, 1808C, 1812C, 2020C, 2220C, 1206C_RAC |
  | Pojemność [niepewne — podpis] | pole liczbowe (0–1000) + lista | pF, nF, uF, mF |
  | Napięcie [niepewne — podpis] | pole liczbowe (0–1000) + lista | V, kV |
  | Tolerancja | pole liczbowe (0–100) + lista | %, pF (wpisanie „5”, „5%” ustawia „%”) |
  | Dielektryk | lista | C0G, X5R, X7R, X8R, X6S, X7S, X7T, Y5V |
  | AEC-Q200 | pole wyboru | — |

- **Szczególne:** tolerancja z „%” zapisywana do bazy jako tekst (żeby Excel nie zamienił jej na ułamek); tolerancja w pF — jako bezwzględna.

#### frCTCE — kondensator tantalowy / elektrolityczny

- **Kiedy:** krok 2, desygnator CE… lub CT… (albo MPN w ctceValue).
- **Pola:**

  | Podpis | Kontrolka | Wartości |
  |---|---|---|
  | Obudowa | lista | CAP_ALU_, TANTAL_A, TANTAL_B, TANTAL_C, TANTAL_D, TANTAL_D2, TANTAL_D3L, TANTAL_E, TANTAL_L, TANTAL_P, TANTAL_R, TANTAL_V, TANTAL_W, TANTAL_X |
  | Pojemność [niepewne — podpis] | pole liczbowe (0–1000) + lista | pF, nF, uF, mF, F |
  | Napięcie [niepewne — podpis] | pole liczbowe (0–1000) + lista | V, kV |
  | Tolerancja | pole liczbowe (0–100) + lista | %, pF |
  | ESR | pole liczbowe (0–1 000 000) + lista | tylko „mR”; tylko liczba całkowita |
  | lowESR | pole wyboru | — |
  | AEC-Q200 | pole wyboru | — |

- **Szczególne:** ESR z przecinkiem lub kropką jest sklejany w liczbę całkowitą (kropka usuwana, np. „0.3” → 3). [niepewne] Czy to zamierzone? Dla aluminiowych elektrolitów lista obudów ma tylko jedną pozycję „CAP_ALU_” — bez wymiaru.

#### frCFM — Case / Footprint / Mounting

- **Kiedy:** krok 3 (Standaryzuj Obudowy), gdy linii nie da się przypisać automatycznie (kilka MPN, Case w bazie niezatwierdzony albo inny montaż niż w BOM).
- **Okno:** pole informacyjne z porównaniem dla każdego MPN linii — „PN: …”, „BOM: <montaż> : <case>”, „DATA: <montaż> : <case>”.
- **Pola:**

  | Podpis | Kontrolka | Wartości |
  |---|---|---|
  | Case | lista z możliwością wpisania | Case z BOM i Case z bazy dla każdego MPN (bez powtórzeń); wpisanie nowej wartości dodaje ją do listy. Klawisze spacja, `/ \ ; :` są blokowane; tekst normalizowany na bieżąco. |
  | Footprint | pole tylko do odczytu | Gdy którykolwiek Footprint w bazie jest niezatwierdzony — kopiuje się z Case; w przeciwnym razie pokazuje Footprint z BOM. |
  | Mounting | lista | SMT, THT, MECH, PressFit, Pin-In-Paste, Bezpośrednio do Klienta, Materiały Dodatkowe |

- **Przyciski:** PRZYPISZ, PRZYPISZ Z ZATWIERDZENIEM OBUDOWY, Pomiń [niepewne — podpis], Zakończ [niepewne — podpis].
- **Wynik:** Case, Footprint i Mounting do BOM. Do partsData dla każdego MPN linii: Case, Mounting i flagCase („|” albo „<login> [data]”), poprzedni stan do Tag1. Footprint nie trafia do bazy (dopiero przy zatwierdzeniu P&P).
- **Szczególne:** Footprint nie da się w tym oknie wpisać ręcznie — inny niż Case Footprint (decyzja Technologa z wywiadu) można wprowadzić tylko bezpośrednio w arkuszu BOM.

## 3. Reguły biznesowe zaszyte w kodzie

Reguły, które aplikacja musi przejąć (albo świadomie zmienić):

1. **Format Value — rezystor:** `<R><jedn.>_<tol>%_<moc><jedn.>[_AEC-Q200][_<n>ppm]`, jednostki R/k/M/mR, mW/W. Dla 0 Ω: `0R_<moc>` (bez tolerancji). Przykład: `10k_1%_100mW`.
2. **Format Value — kondensator ceramiczny:** `<C><jedn.>/<U><jedn.>/<dielektryk>[_<tol>(%|pF)][_AEC-Q200]`, jednostki pF/nF/uF/mF, V/kV; dielektryki C0G, X5R, X7R, X8R, X6S, X7S, X7T, Y5V. Przykład: `100nF/16V/X7R_10%`.
3. **Format Value — tantal/elektrolit:** `<C>/<U>[_<tol>][_ESRxxxx | _LESRxxxx][_lowESR][_AEC-Q200]`. ESR w mΩ jako liczba całkowita, do 9999 uzupełniana zerami do 4 cyfr (`ESR0025` = 25 mΩ); powyżej — 5+ cyfr. `LESR` gdy zaznaczono lowESR i podano ESR; `lowESR` gdy zaznaczono bez wartości. W dataPart zapisywane jako `ESR<cyfry>`.
4. **Dopisek PPM do rezystora** — dwie **różne** reguły w kodzie:
   - w kroku 1 (z bazy): zawsze dla jednego wyróżnionego Klienta; dla innych gdy tolerancja **< 1%** albo PPM **≤ 50**;
   - w formularzu kroku 2: zawsze dla tego Klienta; dla innych gdy tolerancja **≥ 1%** albo PPM **> 50**.
   Warunki są odwrotne — patrz pytania.
5. **Listy obudów w formularzach:** rezystory 0201R…2512R, 0804R_RAC, 1206R_RAC; kondensatory 0201C…2220C, 1206C_RAC; tantale/elektrolity CAP_ALU_, TANTAL_A…X.
6. **Normalizacja MPN (porównywanie):** zamiana przecinka na kropkę, usunięcie spacji i znaków `/ \ * # _ - + =`, wielkie litery. Tekst MPN zapisywany do bazy pozostaje oryginalny.
7. **Normalizacja Case/Footprint:** usunięcie spacji i `/ \ ; : ,`, wielkie litery, „x” między cyframi małe.
8. **Klucz zamienników:** identyczne Value i Case po normalizacji (wielkie litery, przecinek→kropka, pojedyncze spacje).
9. **Part TS:** C/CE/CT/R → Value (lub Part Number), pozostałe → Part Number; wyjątek dla jednego Klienta (zawsze MPN).
10. **„Elementy RC”** to desygnatory zaczynające się od C, CE, CT, R + cyfra. Tylko dla nich: formularze parametrów i automatyczne dopisywanie do partsData w kroku 1.
11. **Flagi w dataPart:** puste = brak informacji; `|` = przypisane, niezatwierdzone; `?` = do weryfikacji; `<login> [dd.mm.rrrr gg:mm]` = zatwierdzone. Zmiana zatwierdzonego Case/Mounting/Footprint przenosi poprzedni stan do Tag1 i przesuwa Tagi w prawo.
12. **Kolejność linii:** historia zmian porównuje BOM z kopią **po pozycji wiersza** — to techniczne źródło reguły „nie zmieniamy kolejności linii” z wywiadu. Nr pozycji w historii = wiersz − 10.
13. **DNP przy imporcie:** słowa dnp/dnm/dni/nm/ni na początku lub końcu Part TS, Value albo Part Number.
14. **Mounting:** PCB, DNP, SMT, THT, MECH, PressFit, Pin-In-Paste, Bezpośrednio do Klienta, Materiały Dodatkowe.
15. **Zapas dla powierzanych:** ilość = pakiety × Quantity; grupa wg Case z arkusza `entScrap` (brak Case w tabeli → grupa 14 dla SMT/innych, 15 dla THT); próg = najmniejszy próg ≥ ilość (albo ostatni); wynik = ilość + stała + mnożnik × ilość. DNP → 0.
16. **Zatwierdzenie iPart** wymaga nagłówka „Part TS” w E10. Numery zmian w historii nadawane są dopiero po zatwierdzeniu.
17. **Klient i projekt** odczytywane z położenia pliku w strukturze folderów (Klient\Projekt\folder dokumentacji firmy\…).
18. **Wersjonowanie szablonu:** `revMajor.Minor` + znacznik czasu `rrrrmmdd_ggmm`; wydanie tylko z folderu MASTER.

## 4. Odpowiedzi na otwarte pytania z wywiadu

| Pytanie z wywiadu | Co mówi kod |
|---|---|
| Czy „Rozkoduj Part Number” rozdziela kilka MPN? | Tak — po separatorze podawanym przy każdym uruchomieniu; pierwszy MPN zostaje w Part Number BOM. |
| Które pola porównuje comparator? | Nie w kodzie iPart. iPart dostaje gotowe Confirmed (kolumna A CSV). |
| Co się dzieje przy cofnięciu potwierdzenia? | Kolumna N: „ANULOWANO POTWIERDZENIE: <inicjały>[data]” na początku i czerwone tło; wpis w Historii Zmian. |
| Jak wyróżniane są zmiany? | W iPart: numery `ID_Zmian[…]` w kolumnie N (tylko z ostatniego zapisu) i czerwone tło przy anulowaniu. Gradientu kolorów nie ma w kodzie VBA — [niepewne] formatowanie warunkowe albo sPart/cPart. |
| Czy wykrywana jest niezgodność Value i MPN w BOM Klienta? | Częściowo: gdy kilka MPN jednej linii ma różne wartości w bazie → `?wartość1 | wartość2`. Różnica między Value Klienta a Value z bazy **nie jest zgłaszana** — Value Klienta jest nadpisywana. Potwierdza brak mechanizmu z wywiadu. |
| Co widzi magazynier po zmianie pozycji? | Poza zakresem iPart. |
| Jak działa tryb podsumowania w sPart? | Poza zakresem iPart. |
| Czy zmiana zatwierdzonego parametru kasuje zatwierdzenie? | Częściowo automatycznie: zmiana Case/Mounting w kroku 3 ustawia nową flagę („|” albo nowe zatwierdzenie), poprzednia trafia do Tag1. Zmiana Value w kroku 2 dla zatwierdzonego MPN nie wywołuje formularza w ogóle. |

## 5. Ryzyka i słabe punkty

1. **Przestawione dzień i rok w nazwie kopii zapasowej.** Kopia nazywana jest `BOM_backup_ddmmrr_…`, a historia zmian odczytuje datę jako `rrmmdd`. Przy kilku kopiach „najnowsza” może być wybrana błędnie (np. kopia z 31 stycznia uznana za nowszą niż z 15 marca), a historia porównuje wtedy BOM z niewłaściwym stanem. [niepewne] Skala problemu zależy od tego, ile kopii zwykle jest w pliku.
2. **Literówka w nazwie arkusza przy odświeżaniu zamienników.** `A_SubstUpdate` odwołuje się do arkusza „MPN niedozowolone” (powinno być „niedozwolone”). Skutek: po wyczyszczeniu MPN zatwierdzonych dla zmienionych wierszy makro kończy się błędem, **zanim** przypisze je ponownie i zanim wyczyści MPN niedozwolone. Zmienione wiersze zostają bez zamienników.
3. **Rozbieżna kolumna startowa zamienników.** Krok 5 (`MatchyMatchy`) zaczyna listę MPN od kolumny M, pozostałe makra od kolumny O. Działa tylko dzięki temu, że M i N zawierają formuły. [niepewne] W pustym wierszu krok 5 mógłby wpisać MPN do kolumn Notes/Historia Zmian.
4. **Fałszywe DNP.** Słowa „nm” i „ni” na końcu Value lub Part Number oznaczają linię jako DNP — np. dioda LED z wartością „850nm” zostanie uznana za DNP.
5. **Sprzeczna reguła PPM** w kroku 1 i w formularzu rezystora (rozdział 3, punkt 4).
6. **Luźna normalizacja MPN.** Usuwanie `-`, `_`, `/`, `+` sprawia, że różne MPN mogą zostać uznane za ten sam (np. `ABC-1` i `ABC1`). To inna zasada niż w modelu bazy w repozytorium (MPN porównywany dokładnie, tylko z obciętymi spacjami na brzegach). Decyzja do podjęcia przed migracją.
7. **Niespójne wyszukiwanie w resValue/capValue/ctceValue.** Zapis parametrów szuka MPN dokładnym tekstem, a odczyt — po normalizacji. Możliwe duplikaty wierszy dla tego samego MPN zapisanego inaczej.
8. **Wiele osób zapisuje do jednego pliku dataPart.** Kroki 1–3 i zatwierdzenie P&P otwierają dataPart do zapisu i zapisują cały plik. Gdy ktoś inny ma go otwartego, działa tylko odczyt. Brak kontroli, czy między odczytem a zapisem ktoś inny nie zmienił danych.
9. **Historia porównuje po pozycji wiersza.** Wstawienie lub usunięcie linii w środku BOM zostanie zapisane jako zmiana wszystkich kolejnych pozycji.
10. **Twarde adresy komórek i kolumn.** Import BOM, START (D16, D23, F14, F21), BOM (N1–N4, N9, D1–D3), P&P (N1), kopie (AZ, BA:BM), scrapPart (wiersz 1, od wiersza 3 i 23) — przesunięcie układu psuje makra bez komunikatu.
11. **Reguły dla konkretnych Klientów wpisane w kod** (3 przypadki: obowiązkowy MPN w Part TS, osobna kolumna Footprint, zawsze PPM). Nowy wyjątek wymaga zmiany kodu i wydania szablonu.
12. **Klient i projekt z nazw folderów.** Plik przeniesiony w inne miejsce traci Klienta (reguły specyficzne przestają działać, entPart dostaje „UnknownClient”).
13. **Ścieżki sieciowe i login autora wpisane w kod.** Zmiana serwera, dysku lub osoby odpowiedzialnej za szablon wymaga zmiany kodu.
14. **Zatwierdzenie P&P zatwierdza Footprint w bazie dla całego BOM**, także dla linii DNP/THT; pusty Footprint w BOM nadpisałby Footprint w bazie.
15. **Przepełnienie Tagów.** Przesuwanie historii w prawo może zapisać dane za kolumną Tag15, gdy wszystkie Tagi są zajęte.
16. **Szerokie wyciszanie błędów** (`On Error Resume Next`) przy ochronie arkuszy i otwieraniu plików — część błędów nie daje żadnego komunikatu.
17. **Przyciski bez działania:** „Procedura Uproszczona” i „Wygeneruj BOM wyjściowy” [niepewne] wywołują zatwierdzenie BOM; procedura eksportu jest pusta.
18. **Hasła w kodzie.** Hasła do arkuszy są stałymi w kodzie VBA — ochrona chroni przed przypadkową zmianą, nie przed świadomym działaniem (co zgadza się z wywiadem: hasło znają wszyscy).

## 6. Pytania do potwierdzenia przez autora

| Pytanie | Moduł |
|---|---|
| Która reguła dopisywania PPM jest właściwa: tolerancja < 1% lub PPM ≤ 50, czy tolerancja ≥ 1% lub PPM > 50? | `iB_p1_RozkodujPN`, `frRES` |
| Skąd bierze się kolumna AF (powierzenie) w CSV z comparatora i czy to jest dzisiejsza opcja „cały BOM powierzony”? | `iS_p1A_ImportBOM` |
| Czy przyciski „Procedura Uproszczona” i „Wygeneruj BOM wyjściowy” mają być podpięte pod `Procedura_Uproszczona` i eksport? Do czego służy BOM wyjściowy? | `iB_p0_uproszczona`, `iB_p8_exportBOM` |
| Czy Procedura Uproszczona (Part TS + obudowy, bez parametrów, zamienników i zatwierdzenia) to zaczątek trybu dla ofert / BOM wstępnego? | `iB_p0_uproszczona` |
| Kto i kiedy ustawia flagę „?” w dataPart? | `iB_p3_Parametry` |
| Czy zatwierdzenie P&P powinno pomijać linie DNP i THT przy zatwierdzaniu Footprintów w bazie? | `iP_p1_ZatwierdzeniePP` |
| Czy odświeżenie zamienników po zmianie ma kasować także MPN niedozwolone i ręcznie dopisane MPN zatwierdzone? | `A_SubstUpdate` |
| Jak zamiennik od zakupowca trafia do iPart — przez sPart bezpośrednio czy ręcznie? | (sPart) |
| Czy „Opiekun Projektu” w BOM!N2 to osoba, która kliknęła zatwierdzenie, czy osoba odpowiedzialna za projekt? Czy zatwierdzenie iPart powinno dać się cofnąć? | `iB_p6_ZatwierdzenieBOM` |
| Czy zamienniki powinny być dobierane po parametrach liczbowych (tolerancja, moc, napięcie), a nie po identycznym tekście Value? | `iB_p2_Zamienniki` |
| Jak wprowadzane są zmiany układu arkuszy (nowe kolumny), skoro aktualizacja szablonu przenosi tylko makra? | `A_ForwardUpdate` |
| Czy luźna normalizacja MPN (usuwanie `-`, `_`, `/`) jest zamierzona? | `Public_Function` |
| Czy w BOM zdarzają się Value kończące się na „nm” lub „ni” (LED, długość fali) błędnie oznaczane jako DNP? | `iS_p1A_ImportBOM` |
| Gdzie ustalany jest gradient podświetlenia zmian i format `[#,#] [#]` — w iPart (formatowanie warunkowe) czy w sPart/cPart? | `A_HistoriaZmian` |
| Czy Value z BOM Klienta różna od Value z dataPart powinna być zgłaszana zamiast nadpisywana? | `iB_p1_RozkodujPN` |
| Czy Mounting „Bezpośrednio do Klienta” i „Materiały Dodatkowe” są używane i czy pokrywają się z „sposobem wydania” w cPart? | `frCFM` |
