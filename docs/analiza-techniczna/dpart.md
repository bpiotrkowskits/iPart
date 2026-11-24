---
status: roboczy
zakres: analiza techniczna pliku dPart (szablon pojedynczy „dPart v2” i łączony dPart_v3.f) wraz z eksportem z cPart
przegląd: 2026-11-23
---

# dPart — analiza techniczna

Źródło: kod VBA wyciągnięty (tylko do odczytu) z `dPart.xlsm` i `dPart_v3.f.xlsm` — po 10 modułów, ok. 430 linii, dwa formularze — oraz układ arkuszy obu plików. Ponieważ dPart **nie pobiera danych sam**, przejrzano też moduł eksportu w cPart (`cZK_p5_ExportKompletacja`), który tworzy i wypełnia dPart. Pełny opis cPart jest w osobnym dokumencie. Porównanie z ustaleniami z [wywiadu](../wywiad-obieg-plikow.md).

Oznaczenia: **[zgodne z wywiadem]**, **[różnica względem wywiadu]**, **[nowe — nieomawiane]**, **[niepewne]** — kod tego nie rozstrzyga.

Pojęcia jak w [ipart.md](ipart.md). Dodatkowo:

- **Szablon** — pusty plik-wzór, z którego makro robi kopię i wypełnia ją danymi. dPart w folderze `zrodla/` to właśnie szablony — nie zawierają danych.
- **Eksport (push)** — dane „wypycha” plik źródłowy (cPart) do nowego pliku. dPart nie ma żadnego kodu, który by sam czytał cPart.

## 1. Przegląd modułów

Kod VBA obu szablonów jest **identyczny** (różnią się tylko wewnętrzne identyfikatory formularzy). Różnią się arkusze — patrz rozdział 2.1.

| Moduł | Rola |
|---|---|
| `Ten_skoroszyt` | Przy otwarciu: blokuje aktywny arkusz i strukturę skoroszytu, komunikat „Plik otwarto w trybie podglądu. Edycja i zapis są zablokowane.” |
| `D0_Start` | Jedna zmienna `PodgladAktywny` — ustawiana przy otwarciu, nigdzie nie odczytywana. |
| `D1_Otworz_iPart` | Przycisk: otwiera plik, którego ścieżka jest w A2 (iPart albo fusePart). |
| `D2_Otworz_cPart` | Przycisk: otwiera plik ze ścieżki w U2 (cPart). |
| `D3_ZmienIloscFuse` | Przycisk (wariant łączony): zmiana ilości pakietów per ZK i przeliczenie SumaQty w kopii arkusza. |
| `Module3`, `Module4`, `Arkusz1` | Puste. |
| `frmFUSE` | Formularz: 15 pól ilości, podpisanych numerami ZK. |
| `frmWybierzArkusz` | Formularz wyboru trybu — **nieużywany** w dPart (kopia z cPart/sPart). |

## 2. Mechanizmy

### 2.1. Dwa szablony — różnice w arkuszach

Oba szablony mają jeden arkusz `START` z nagłówkiem w wierszach 1–7 i tabelą od wiersza 10.

| Element | dPart (pojedynczy, „dPart v2 z dnia 20.10.2025”) | dPart_v3.f (łączony) |
|---|---|---|
| A1 / link | Link do iPart | Link do fusePart |
| Nagłówek | Nazwa projektu, Ilość [szt] | Nazwa fuse, „Komplet” = 1 |
| Kolumna A | Lp. | FuseID |
| C | Raport Zmian | Różnice |
| E, F | Designator, Quantity | FuseDes, SumaQty |
| Rdzeń BOM | Confirmed, Designator, Quantity, Part TS, Footprint, Description, Value, Part Number, Mounting, Notes | to samo + **EntPart** |
| Uwagi | Zakupy, Magazyn | Zakupy, Magazyn |
| Produkcja | **MPN zamówiony/powierzony/z magazynu**, Status MPN, Zapotrzebowanie, Wydano, Różnica | **Nazwa**, Status MPN, Zapotrzebowanie, Wydano, Różnica |
| Opakowania | Numer opakowania, Ilość w opakowaniu, **Numer Pojemnika**, Opakowania powtórzone | Numer opakowania, Ilość w opakowaniu, **Lokalizacja**, Opakowania powtórzone |
| DESIGNATORS | od kolumny Y | od kolumny Z |

- **[niepewne]** Kod eksportu w cPart dla pakietu pojedynczego używa szablonu o nazwie **dPart_v3.0.1**, a w `zrodla/` jest „dPart v2”. Opis może dotyczyć starszej wersji szablonu. Szablon v3.0.1 nie był analizowany.
- **[nowe — nieomawiane]** W szablonie łączonym kolumna nazywa się „Lokalizacja” — dopiero eksport zmienia jej nagłówek na „Numer Pojemnika” (rozdział 2.3).

### 2.2. Otwarcie dPart — tryb podglądu

- **Wejście:** otwarcie pliku.
- **Reguła:** zdjęcie ochrony z aktywnego arkusza, zablokowanie **wszystkich** komórek, ponowna ochrona (dozwolone filtrowanie i formatowanie wierszy/kolumn, bez sortowania), ochrona struktury skoroszytu (nie można dodawać ani usuwać arkuszy).
- **Wynik:** plik tylko do oglądania; komunikat o trybie podglądu.
- **Moduł:** `Ten_skoroszyt.Workbook_Open`.
- **[zgodne z wywiadem]** Produkcja korzysta z dPart do odczytu.
- **[nowe — nieomawiane]** Hasło ochrony jest stałą w kodzie (tej samej używa makro zmiany ilości). Komunikat mówi, że zapis jest zablokowany, ale kod blokuje tylko edycję komórek — zapis pliku nie jest technicznie zablokowany. **[niepewne]**

### 2.3. Powstanie dPart — eksport z cPart (kod po stronie cPart)

dPart nie ma kodu importu. Cały plik tworzy makro **„Eksport”** w cPart (`cZK_p5_ExportKompletacja.Eksport`).

- **Wejście:** aktywny arkusz ZK/PP w cPart (tylko w trybie pracy), nazwa projektu (H5), ścieżka pliku głównego (A2: iPart lub fusePart), szablon dPart z centralnego folderu sieciowego.
- **Reguła:**
  1. Folder docelowy: centralny folder sieciowy przygotowania produkcji, podfolder o nazwie folderu Klienta (brany z nazwy folderu, w którym leży cPart).
  2. Wszystkie wcześniejsze dPart tego arkusza/projektu są **przenoszone do podfolderu `z_NIEAKTUALNE`**; ten z najwyższym numerem staje się kopią odniesienia.
  3. Nowy plik: `<arkusz do PP#>_<projekt>_c<NN>_dPart.xlsm`, gdzie **NN = poprzedni najwyższy numer + 1** (licznik eksportów).
  4. Wybór szablonu: jeśli w cPart E10 = „Designator” → szablon pojedynczy, w przeciwnym razie → szablon łączony (`dPart_v3.f`).
  5. Kopiowany nagłówek: A2 (link do pliku głównego), C1, G1:J7 (ZK, daty, Klient, projekt, ilość, osoba odpowiedzialna). X1 = data eksportu, V2 = pełna ścieżka cPart. W cPart: AB7 = data eksportu, Z8 = ścieżka dPart.
  6. **Kolumny kopiowane po nagłówkach** z wiersza 10 (tylko kolumny o tych samych nazwach), wartości od wiersza 11.
  7. **Footprint** jest dociągany z pliku głównego (iPart arkusz BOM albo fusePart arkusz fuseBOM) po ID linii — czyli w dPart jest aktualny Footprint z iPart, nie kopia z cPart.
  8. Bloki „Nieprzypisane Opakowania” i „Niewykryte MPN” z cPart są doklejane pod tabelą (niebieskie pole).
  9. Wariant łączony: doklejany blok nagłówków ZK (wiersze 1–3) i pole tekstowe, a potem dane z arkusza fuseBOM pliku fusePart.
  10. Porównanie „Data Aktualizacji” z poprzednim dPart (po ID): zmieniona data → komórka podświetlona na pomarańczowo.
  11. **Rozdzielenie opakowań** (rozdział 2.4) i kolorowanie wierszy (SMT/PCB — biało-niebieskie pasy, THT — zielone pasy; pomijane linie niepotwierdzone i DNP).
  12. Ochrona arkusza dPart i zapis.
- **Wynik:** nowy plik dPart w folderze produkcji; stare w `z_NIEAKTUALNE`.
- **[zgodne z wywiadem]** dPart powstaje jako eksport z cPart; każde ponowne wydanie daje nowy plik.
- **[różnica względem wywiadu]** Pliki z **TS Kompletor nie są dołączane przy eksporcie**. Są wczytywane wcześniej do cPart osobnym makrem „Import” (`cZK_p5_ImportKompletacja.Import` — użytkownik wskazuje pliki .xls/.xlsx z Kompletora, opakowania są przypisywane do linii BOM po MPN). Eksport przenosi już przypisane opakowania. Szczegóły w analizie cPart.
- **[nowe — nieomawiane]** Numer „c<NN>” w nazwie pliku to licznik kolejnych dPart dla danego arkusza/projektu; nowy dPart od razu „unieważnia” poprzednie (przeniesienie do `z_NIEAKTUALNE`).
- **[nowe — nieomawiane]** Podświetlenie zmian w dPart porównuje tylko kolumnę „Data Aktualizacji” z poprzednim dPart — nie pokazuje, co się zmieniło.

### 2.4. Opakowania, numer pojemnika, opakowania powtórzone (w trakcie eksportu)

- **Wejście:** kolumny „Numer Opakowania”, „Ilość w opakowaniu”, „Lokalizacja” — w cPart jedna linia BOM może mieć kilka opakowań wpisanych w jednej komórce (każde w nowej linii komórki; ilości rozdzielone znakiem „+”).
- **Reguła:**
  1. **Rozdzielenie:** każde opakowanie dostaje własny wiersz pod linią BOM. Pierwszy wiersz ma pełne dane linii; kolejne tylko Mounting, Notes, Nazwa, Status oraz dane opakowania. „Wydano” jest sumowane w pierwszym wierszu grupy.
  2. Jeśli opakowania w jednej linii mają **różne MPN (Nazwa)**, numery opakowań i nazwy są pogrubiane na fioletowo.
  3. **Numer pojemnika:** z tekstu Lokalizacji (np. ścieżka magazynowa z „/”) wycinany jest fragment — przy co najmniej 4 częściach przedostatni, w przeciwnym razie ostatni. Nagłówek kolumny zmienia się na „Numer Pojemnika”.
  4. **Opakowania powtórzone:** jeśli ten sam numer opakowania jest przypisany do kilku linii, w kolumnie „Opakowania powtórzone” wpisywane jest „również dla ID: <lista ID>”.
- **Moduł:** `cZK_p5_ExportKompletacja.RozdzielOpakowania`.
- **[zgodne z wywiadem]** Numer pojemnika służy do szukania opakowania przy wydaniu.
- **[nowe — nieomawiane]** Numer pojemnika nie jest wpisywany ręcznie — jest wycinany z lokalizacji magazynowej według pozycji w tekście. Zmiana formatu lokalizacji w systemie magazynowym zepsuje wynik bez komunikatu.
- **[nowe — nieomawiane]** Jedno opakowanie przypisane do kilku linii jest tylko oznaczane, nie blokowane.

### 2.5. Sekcja DESIGNATORS i zmiana ilości (wariant łączony)

- **Wejście:** kolumna E (FuseDes) — w każdej komórce wiersze w formacie `<numer ZK>: <liczba desygnatorów>: <lista desygnatorów>`; w wierszu 1 (od kolumny Y) numery ZK, w wierszu 2 ilości pakietów dla każdego ZK.
- **Reguła (przycisk „zmień ilość”):**
  1. Formularz `frmFUSE` pokazuje 15 pól; podpis każdego pola = nagłówek z wiersza 1, wartość = wiersz 2. Pole bez numeru ZK jest nieaktywne.
  2. Po „OK” arkusz jest **kopiowany** jako nowy arkusz z sufiksem `_DDMM` (dzień i miesiąc), unikalnie.
  3. Nowe ilości wpisywane są do wiersza 2 kolumn Y…AM (tylko tam, gdzie w wierszu 1 jest numer ZK).
  4. Kolumna F (SumaQty) przeliczana: dla każdej linii suma po ZK z (ilość pakietów w ZK × liczba desygnatorów dla tego ZK).
  5. W AA6:AC6 zapisywane: „Ilości aktualizował:”, login, godzina.
- **Wynik:** nowy arkusz z przeliczonym zapotrzebowaniem; stary zostaje.
- **Moduł:** `D3_ZmienIloscFuse.ZmienIloscFuse`, `RecalcF_FromE`, `SumForECell`; formularz `frmFUSE`.
- **[zgodne z wywiadem]** Sekcja DESIGNATORS w wariancie łączonym zawiera listę projektów z fusePart i desygnatory z danego projektu dla linii.
- **[nowe — nieomawiane]** Na poziomie dPart można zmienić ilości pakietów per ZK i przeliczyć zapotrzebowanie — bez powrotu do fusePart/sPart/cPart. Zmiana nie wraca do innych plików.
- **[niepewne] Możliwa niespójność kolumn:** formularz czyta numery ZK i ilości z kolumn **M…AA**, a makro zapisuje i liczy z kolumn **Y…AM** (komentarze w kodzie wspominają jeszcze U…AI). Jeśli w wygenerowanym dPart numery ZK są od kolumny Y/Z, formularz pokaże puste/inne pola. Do sprawdzenia na prawdziwym pliku.

### 2.6. Formularze

**frmFUSE** — okno „zmiana ilości”: 15 etykiet (Label1…15) z numerami ZK, 15 pól tekstowych (TextBox1…15) z ilościami, przyciski OK i Anuluj. Nieaktywne pola dla pustych ZK. Brak sprawdzania, czy wpisano liczbę (tekst daje 0).

**frmWybierzArkusz** — okno „Wybierz arkusz do aktualizacji BOM”: lista rozwijana z „[Tryb Podglądu]”, „[Tryb Pracy] <arkusz>” dla każdego arkusza i „[Tryb Pracy] Nowe ZK”, przycisk OK. Zamknięcie krzyżykiem pyta o zamknięcie pliku. **W dPart nic go nie wywołuje** — to pozostałość po cPart/sPart (pomija arkusze „Kursy walut”, „Magazyn”, „obudowy”, „ZK”, których w dPart nie ma).

## 3. Reguły biznesowe zaszyte w kodzie

1. dPart jest tylko do odczytu po otwarciu (ochrona arkusza i struktury).
2. Każdy eksport tworzy nowy plik z licznikiem `c<NN>`; poprzednie trafiają do `z_NIEAKTUALNE`.
3. Wybór szablonu: E10 w cPart = „Designator” → pojedynczy; inaczej → łączony.
4. Footprint w dPart pochodzi zawsze z pliku głównego (iPart/fusePart), dopasowany po ID linii.
5. Jedno opakowanie = jeden wiersz; ilości wielu opakowań w cPart rozdzielane znakiem „+”, numery i lokalizacje — nową linią w komórce.
6. Numer pojemnika = przedostatni (≥4 części) lub ostatni fragment lokalizacji rozdzielonej „/”.
7. Opakowanie przypisane do kilku linii → „również dla ID: …”.
8. Kolorowanie: SMT/PCB i THT naprzemiennie wg parzystości ID; linie niepotwierdzone (Confirmed = fałsz) i DNP bez koloru.
9. Wariant łączony: SumaQty = Σ (ilość pakietów ZK × liczba desygnatorów ZK); format FuseDes `<ZK>: <liczba>: <desygnatory>`.

## 4. Odpowiedzi na otwarte pytania z wywiadu

| Pytanie | Odpowiedź z kodu |
|---|---|
| Co zawiera sekcja „Designators” w dPart? | W wariancie łączonym: w wierszach 1–3 nagłówki ZK z cPart, w wierszu 10 ich kopia; dla linii — desygnatory per projekt (z fusePart). Kolumna E zawiera rozbicie `<ZK>: <liczba>: <desygnatory>`. [zgodne z wywiadem] |
| Jak dPart dostaje dane z cPart? | Eksportem z cPart (push) do nowego pliku z szablonu; dPart nie czyta cPart. |
| Pliki TS Kompletor | Wczytywane do cPart osobnym importem przed eksportem, nie dołączane do dPart. [różnica względem wywiadu] |
| Numer pojemnika | Wycinany automatycznie z lokalizacji magazynowej. |
| Opakowania powtórzone | Oznaczenie „również dla ID: …”, bez blokady. |

## 5. Ryzyka i słabe punkty

1. **Hasło ochrony w kodzie** — znane każdemu, kto otworzy edytor VBA.
2. **Niespójne kolumny** formularza ilości (M…AA) i makra (Y…AM). [niepewne]
3. **Numer pojemnika z pozycji w tekście** — zależny od formatu lokalizacji z systemu magazynowego.
4. **Twarde ścieżki sieciowe** do szablonów i folderu produkcji w kodzie cPart.
5. **Zmiana ilości w dPart nie wraca** do fusePart/sPart/cPart — dPart może pokazywać inne zapotrzebowanie niż pozostałe pliki.
6. **Rozbieżność wersji szablonu** — w kodzie v3.0.1, w materiałach v2.
7. Formularz `frmWybierzArkusz` i zmienna `PodgladAktywny` — martwy kod.
8. Komunikat „zapis zablokowany” nie odpowiada działaniu (blokowana jest tylko edycja).

## 6. Pytania do potwierdzenia przez autora

| Pytanie | Moduł |
|---|---|
| Czy aktualnym szablonem pojedynczym jest dPart_v3.0.1? Czy różni się od „dPart v2” z materiałów? | cPart `cZK_p5_ExportKompletacja` |
| Czy przycisk „zmień ilość” w dPart jest używany? Jeśli tak — czy formularz pokazuje poprawne numery ZK (kolumny M…AA vs Y…AM)? | `D3_ZmienIloscFuse`, `frmFUSE` |
| Czy zmiana ilości w dPart powinna wracać do fusePart/sPart? | `D3_ZmienIloscFuse` |
| Czy reguła „przedostatni fragment lokalizacji” zawsze daje numer pojemnika? | cPart `RozdzielOpakowania` |
| Czy opakowanie przypisane do kilku linii powinno być blokowane, czy tylko oznaczane? | cPart `RozdzielOpakowania` |

## 7. Współzależności z innymi plikami

| Kierunek | Plik | Arkusz/zakres | Kiedy | Moduł/procedura | Co przenosi |
|---|---|---|---|---|---|
| cPart → dPart | cPart | arkusz ZK/PP: nagłówek A2, C1, G1:J7; tabela od wiersza 10 po nagłówkach; bloki nieprzypisanych | Przycisk „Eksport” w cPart | cPart `cZK_p5_ExportKompletacja.Eksport` | Rdzeń BOM, zakupy, magazyn, opakowania, lokalizacje; tworzy nowy plik [zgodne z wywiadem] |
| dPart ← iPart | iPart | BOM: ID, Footprint | Przy eksporcie | `CopyFootprintFromMainByID` (cPart) | Aktualny Footprint po ID linii [nowe — nieomawiane] |
| dPart ← fusePart | fusePart | fuseBOM | Przy eksporcie (wariant łączony) | `AppendFusePartData`, `CopyFootprintFromMainByID` (cPart) | Dane fuseBOM i Footprint [nowe — nieomawiane] |
| dPart ← poprzedni dPart | dPart (z_NIEAKTUALNE) | kolumna Data Aktualizacji | Przy eksporcie | `CompareUpdateDatesWithBackup` (cPart) | Podświetlenie zmienionych dat [nowe — nieomawiane] |
| cPart ← dPart (ślad) | cPart | AB7, Z8 | Przy eksporcie | `Eksport` (cPart) | Data eksportu i ścieżka dPart |
| TS Kompletor → cPart → dPart | pliki z Kompletora | pierwszy arkusz | Przed eksportem, import w cPart | cPart `cZK_p5_ImportKompletacja.Import` | Opakowania przypisane po MPN; do dPart trafiają pośrednio [różnica względem wywiadu] |
| dPart → iPart/fusePart | iPart/fusePart | ścieżka A2 | Przycisk „otwórz” | `D1_Otworz_iPart` | Tylko otwarcie pliku |
| dPart → cPart | cPart | ścieżka U2 | Przycisk „otwórz” | `D2_Otworz_cPart` | Tylko otwarcie pliku |
| dPart → (inne) | — | — | — | `D3_ZmienIloscFuse` | Zmiana ilości zostaje tylko w dPart [nowe — nieomawiane] |
