---
status: roboczy
zakres: analiza techniczna plików bez makr — dataPart.xlsx, scrapPart.xlsx, entPart.xlsx
przegląd: 2026-11-23
---

# dataPart, scrapPart, entPart — analiza techniczna

Źródło: struktura arkuszy i formuł plików `dataPart.xlsx`, `scrapPart.xlsx`, `entPart.xlsx` (tylko odczyt) oraz miejsca w kodzie VBA innych plików, które z nich korzystają. Formaty flag i Tagów opisano wzorcem — bez przepisywania loginów i nazwisk. Porównanie z [wywiadem](../wywiad-obieg-plikow.md) i [ipart.md](ipart.md).

Oznaczenia: **[zgodne z wywiadem]**, **[różnica względem wywiadu]**, **[nowe — nieomawiane]**, **[niepewne]**.

Pojęcia:

- **Formuła wyszukująca** (`VLOOKUP`, `INDEX`/`MATCH`, `HLOOKUP`) — formuła Excela, która znajduje wiersz po kluczu (np. MPN) i zwraca wartość z innej kolumny.
- **Nazwa zdefiniowana** — nazwa nadana zakresowi komórek (np. `obudowa_zapasv3`), używana w formułach zamiast adresu.
- **Łącze zewnętrzne** — formuła w jednym pliku, która czyta komórki z innego pliku.

## 1. Przegląd

| Plik | Arkusze | Rola |
|---|---|---|
| dataPart.xlsx | partsData, railmilCase, thtCase, altCase, stdCase, qtyCase, resValue, capValue, ctceValue | Baza komponentów (ok. 40,6 tys. wierszy w partsData). |
| scrapPart.xlsx | obudowy (ukryty), **supplyScrap**, **entScrap** | Reguły zapasu wg obudowy — dla zakupów i dla komponentów powierzanych. |
| entPart.xlsx | Component List | Szablon (angielski) listy komponentów powierzanych wysyłanej do Klienta. |

## 2. Mechanizmy

### 2.1. dataPart — arkusz partsData

| Kolumna | Zawartość | Kto/co wypełnia |
|---|---|---|
| A Part Number | MPN (klucz) | iPart (dopisywanie nowych MPN) |
| B Mounting | SMT / THT / PCB / … | iPart krok 3 |
| C Case | Obudowa | iPart krok 3 |
| D flagCase | Zatwierdzenie Case | iPart krok 3 |
| E Footprint | Footprint | iPart krok 3 / zatwierdzenie P&P |
| F rmFootprint | **Formuła:** pierwszy footprint z arkusza railmilCase dla tego MPN | automat |
| G flagFootprint | Zatwierdzenie Footprint | iPart zatwierdzenie P&P |
| H Value | **Formuła** składająca Value z arkuszy parametrów (niżej) | automat |
| I defValue | Wartość „domyślna” wpisana ręcznie/z BOM, gdy brak parametrów | iPart kroki 1–2 |
| J flagValue | Zatwierdzenie Value/parametrów | iPart krok 2 |
| K Notes, L Critical, M altCase | Uwagi, krytyczność (prawie nieużywana — 1 wpis), alternatywna nazwa obudowy (162 wpisy) | ręcznie |
| N…X | PADS: RC, TT, DL, QL, BGA, NonS, ThP, THT, preTHT, pressFIT, Mech | ręcznie [niepewne] |
| Y…AM Tag1–Tag15 | Historia zmian | iPart (przesunięcie w prawo przy zmianie) |

**Formuła Value (kolumna H)** — Value **nie jest przechowywana**, tylko liczona z arkuszy parametrów po MPN:

- rezystor (resValue): `Rezystancja_Tolerancja_Moc_AEC-Q200` (części puste pomijane; AEC tylko gdy wpisano dokładnie „AEC-Q200”),
- kondensator ceramiczny (capValue): `Pojemność/Napięcie/Dielektryk_Tolerancja_AEC-Q200`,
- tantal/elektrolit (ctceValue): `Pojemność/Napięcie_Tolerancja_ESR…_AEC-Q200`, przy czym gdy jest i ESR, i lowESR → `_L` + ESR; gdy tylko lowESR → `_lowESR`.
- MPN-y spoza tych arkuszy mają pustą Value (wtedy używane jest defValue).

- **[zgodne z wywiadem]** Value w autorskim formacie firmy pochodzi z dataPart.
- **[różnica względem wywiadu / niespójność]** Formuła Value rezystora **nie zawiera PPM**, a iPart dopisuje PPM osobno w VBA (dwiema sprzecznymi regułami — patrz ipart.md). Ta sama dana ma więc trzy różne reguły budowania.
- **[nowe — nieomawiane]** Value istnieje tylko dla R, C i tantali/elektrolitów; pozostałe komponenty mają wyłącznie defValue.

**Flagi zatwierdzeń (D, G, J) — format:**

| Wartość | Znaczenie (wg kodu iPart) | Liczba wpisów (D / G / J) |
|---|---|---|
| puste | brak informacji | — |
| `\|` | przypisane, **niezatwierdzone** („dopisz bez zatwierdzenia”) | ok. 590 / **ok. 19 600** / ok. 680 |
| `?` | do weryfikacji — czytane przez iPart, ale żadne makro iPart go nie ustawia | [niepewne] |
| `<login> [dd.mm.rrrr gg:mm]` | **zatwierdzone** przez osobę o danym loginie w danym czasie | ok. 6 500 / ok. 4 700 / ok. 2 400 |
| inne (dwa słowa, bez daty) | starszy, ręczny zapis — 155 wpisów w flagCase | [niepewne] |

- **[zgodne z wywiadem]** Flagi mówią, kto i kiedy zatwierdził parametr.
- **[nowe — nieomawiane]** Około **80% footprintów w bazie jest niezatwierdzonych** („|”). Zatwierdzenie P&P w iPart zatwierdza footprinty całego BOM hurtowo (ipart.md), ale stan bazy pokazuje, że większość nie przeszła tej ścieżki.

**Tagi (historia) — format:** głównie `Prev: <Mounting>:<Case> (<poprzednia flaga>)` — zapisywany przy zmianie obudowy/montażu; rzadziej opisy footprintów i inne teksty. Wypełnione: Tag1 ok. 7 900 wierszy, Tag2 ok. 4 500, Tag3 ok. 1 500; dalsze prawie puste.

- **[zgodne z wywiadem]** Tag1–15 to historia zmian.
- **[nowe — nieomawiane]** Historia przechowuje tylko poprzedni Mounting/Case z flagą — bez daty zmiany i bez informacji, kto zmienił (o ile nie wynika z przeniesionej flagi). Po 15 zmianach najstarsza znika.

### 2.2. dataPart — arkusze pomocnicze

| Arkusz | Zawartość | Formuły |
|---|---|---|
| railmilCase | MPN → rmFootprint (ok. 1 760 wierszy; MPN może mieć kilka footprintów) | — |
| thtCase | MPN → Footprint dla elementów THT (ok. 4 090) | — |
| altCase | Rodziny obudów (DO, MELF, SC, SO, SOD, SOP, SSOP, SOT, SSOT, TO, DPAK…) i ich równoważne nazwy | — |
| stdCase | Wzorce nazewnictwa obudów (np. `####C_#PIN` — kondensatory, wymiary calowe) z opisem | — |
| qtyCase | Lista obudów i liczba komponentów w bazie z daną obudową | `COUNTIF` po kolumnie Case partsData |
| resValue | MPN, Rezystancja, Tolerancja, Moc, AEC-Q200, T.min, T.max, Features, ppm, Temp | — |
| capValue | MPN, Pojemność, Napięcie, Dielektryk, Tolerancja, AEC-Q200 | — |
| ctceValue | MPN, Pojemność, Napięcie, Tolerancja, ESR [mR], lowESR, AEC-Q200, Life, Temp, Footprint Tantal, Obudowa-cal, wymiary | — |

- **[nowe — nieomawiane]** rmFootprint bierze tylko **pierwszy** footprint z railmilCase, choć arkusz ma po kilka dla jednego MPN.
- **[nowe — nieomawiane]** Liczniki w qtyCase sumują partsData tylko do wiersza 28 635 — baza ma ok. 40 600 wierszy, więc część komponentów nie jest liczona.
- **[niepewne]** Czy thtCase jest „biblioteką THT” używaną przez comparator.

### 2.3. scrapPart — reguły zapasu

**Arkusze supplyScrap (zakupy) i entScrap (powierzane)** mają ten sam układ:

1. **Tabela grup (wiersze 1–17):**
   - wiersz 1 — **progi ilości**: 0 (entScrap: 1), 10, 50, 100, 250, 500, 1000;
   - dla każdego progu dwie kolumny: **zapas** (stała liczba sztuk) i **mnożnik** (procent ilości, np. 0,01 = 1%);
   - wiersze = **grupy obudów** opisane słownie, od najmniejszych (≤ 0201, 0402, 0603/0805…) po duże (D2PAK, BGA > 24 wyprowadzeń, THD). supplyScrap: 13 grup; entScrap: 15 grup (dodatkowo 14 „nieprzypisane SMD” i 15 „nieprzypisane THD”).
2. **Tabela obudów (od wiersza 23):** kolumna A = numer grupy, B = konkretna obudowa (Case, np. 0402C, SOD123), C…P = parametry zapasu pobrane formułą z tabeli grup, Q = wymiary (pusta), R = potwierdzenie 0/1, S = uwagi.
3. entScrap ma dodatkowo pomocniczą tabelę **min/max** (kolumny T…AH): dla każdego przedziału ilości pokazuje, ile sztuk wyjdzie z zapasem na początku i na końcu przedziału.

**Reguła zapasu:** `ilość do zamówienia/dostarczenia = ilość + zapas(grupa, próg) + mnożnik(grupa, próg) × ilość`.

Liczby: supplyScrap — 1 188 obudów, 3 niepotwierdzone, 3 bez grupy; entScrap — 2 128 obudów, **1 031 bez grupy i niepotwierdzonych**.

**Kto z tego korzysta i jak wybiera próg:**

| Miejsce | Źródło | Wybór progu | Brak obudowy w tabeli |
|---|---|---|---|
| sPart (kolumna zapasu) | **łącze zewnętrzne do pliku „Obudowy”** (nazwy `obudowa_zapasv3`, `obudowav3`, `ilośćv3`), wynik zaokrąglany w górę | **największy próg ≤ ilość** | obudowa „obudowa” (brak montażu) → zapas 0; THT/PCB jako osobne „obudowy” |
| iPart / fusePart — lista powierzonych | scrapPart, arkusz `entScrap`, czytany makrem | **najmniejszy próg ≥ ilość** (albo ostatni) | grupa 14 (SMT/inne) lub 15 (THT) |
| entScrap — tabela min/max | ten sam arkusz | największy próg ≤ ilość | — |

- **[zgodne z wywiadem]** Zapas zależy od obudowy; dwa zestawy wartości — dla zakupów i dla powierzanych; reguły ustalają Kierownik Logistyki i Kierownik działu Obsługi Projektu.
- **[różnica względem wywiadu]** Arkusze nazywają się `supplyScrap` i `entScrap` (w wywiadzie: supplyPart i entPart).
- **[różnica względem wywiadu]** sPart **nie czyta scrapPart.xlsx** — czyta łączem zewnętrznym plik o nazwie „Obudowy” (nazwy zakresów pasują do scrapPart, więc to prawdopodobnie poprzednia nazwa lub kopia tego pliku). [niepewne] Jeśli to osobna kopia, zmiany w scrapPart nie trafiają do zakupów.
- **[nowe — nieomawiane] Niespójny wybór progu:** sPart i tabela min/max biorą próg „od dołu”, a makro listy powierzonych — „od góry”. Przykład (grupa 4 w entScrap, ilość 30): według tabeli min/max → próg 10 → 30 + 15 = 45 szt.; według makra → próg 50 → 30 + 20 = 50 szt.
- **[nowe — nieomawiane]** W entScrap ok. połowa obudów nie ma grupy — dla nich makro bierze grupę „nieprzypisane” (14/15). Formuły w samej tabeli nie obsłużyłyby grup 14 i 15 (zakres `dane_wejściowe` kończy się na grupie 13), więc arkusz nie pokazuje zapasu dla tych obudów, choć makro liczy go z tabeli grup.
- **[nowe — nieomawiane]** Arkusz `obudowy` (ukryty) to starsza wersja tabeli (12 grup); w pliku są też nazwy zdefiniowane wskazujące na nieistniejące zakresy (`#REF!`) — pozostałości po wcześniejszych wersjach.

### 2.4. entPart — szablon listy powierzonych

- **Nagłówek (wypełnia firma):** nazwa i adres Klienta, NIP/VAT, nazwa PCBA, zamówiona ilość, opiekun projektu.
- **Sekcja dla Klienta:** oświadczenie o powierzeniu komponentów (z danymi firmy), data i podpis.
- **Kolumny tabeli (od wiersza 10):**

| Kolumny | Kto wypełnia | Zawartość |
|---|---|---|
| A–I | firma (z iPart/fusePart) | No, Designator, Quantity, Footprint, Description, Value, PartNumber, Mounting, Notes |
| J Production Demand | firma (makro) | Ilość z zapasem wg entScrap |
| K–N | **Klient** | Entrusted PartNumber, Customer PartNumber, Entrusted Quantity, Customer Notes |
| O–Q | [niepewne] firma po odesłaniu | Symbol (magazynowy?), Nazwa PL, Result |

- **Wypełnianie:** makro w iPart (`iB_p7_Powierzone`) lub fusePart (`f3f_p2_EntPart`) kopiuje linie z niepustym EntPart, dopasowując kolumny po nagłówkach; kolumnę J liczy z entScrap. Szablon wybierany wg kraju Klienta: polski („Powierzany…”) lub angielski („Entrusted…”) — w materiałach jest tylko angielski.
- **[zgodne z wywiadem]** Klient odsyła MPN, które powierza, i ilości (kolumny K–N).
- **[zgodne z wywiadem]** Nie ma automatycznego importu odesłanego pliku do sPart/cPart.
- **[nowe — nieomawiane]** Kolumny Symbol / Nazwa PL / Result sugerują planowane lub ręczne porównanie z magazynem po odesłaniu. [niepewne]

## 3. Reguły biznesowe zaszyte w plikach

1. Value = złożenie parametrów R/C/CTCE w stałym formacie (separatory `_` i `/`); bez PPM.
2. Flaga zatwierdzenia: puste / „|” (niezatwierdzone) / „?” (do weryfikacji) / `<login> [data]` (zatwierdzone).
3. Historia komponentu: Tag1 = ostatni poprzedni stan, starsze przesuwane w prawo, maks. 15.
4. rmFootprint = pierwszy footprint z railmilCase dla MPN.
5. Zapas = ilość + stała + mnożnik × ilość; stała i mnożnik zależą od grupy obudowy i progu ilości (0/1, 10, 50, 100, 250, 500, 1000).
6. Obudowa → grupa: tabela obudów w scrapPart; brak w tabeli → grupa „nieprzypisane” (tylko dla powierzanych).
7. Lista powierzonych: tylko linie z wpisanym EntPart; Klient uzupełnia kolumny K–N.

## 4. Odpowiedzi na otwarte pytania z wywiadu

| Pytanie | Odpowiedź z plików |
|---|---|
| Jak zapas zależy od Case? | Case → grupa (tabela obudów) → para (stała, mnożnik) dla progu ilości → wzór z rozdz. 2.3. |
| Czy zmiana zatwierdzonego parametru kasuje zatwierdzenie? | W plikach brak mechanizmu — zależy od makr iPart (przy zmianie Case/Footprint flaga jest nadpisywana nową). [niepewne] |
| Co oznacza „?” we flagach? | Stan „do weryfikacji”; nie wiadomo, kto go ustawia. [niepewne] |

## 5. Ryzyka i słabe punkty

1. **Trzy różne reguły Value rezystora** (formuła dataPart bez PPM, dwie reguły PPM w iPart).
2. **Niespójny wybór progu zapasu** (zakupy „od dołu”, powierzane „od góry”).
3. **sPart liczy zapas z pliku „Obudowy” przez łącze zewnętrzne** — może się rozjechać ze scrapPart.
4. **Zakresy formuł krótsze niż dane** (qtyCase do wiersza 28 635; inne zakresy z „zapasem” do ~200 tys. wierszy — działają, ale spowalniają przeliczanie).
5. **80% footprintów niezatwierdzonych** — automatyczne przypisanie footprintu w iPart (tylko zatwierdzone) rzadko zadziała.
6. **Połowa obudów w entScrap bez grupy** — zapas dla powierzanych liczony domyślną grupą.
7. **Historia w Tagach** — bez dat zmian, z limitem 15, w formacie tekstowym trudnym do przeszukania.
8. **Martwe nazwy (`#REF!`) i ukryty stary arkusz** w scrapPart.

## 6. Pytania do potwierdzenia przez autora

| Pytanie | Plik / miejsce |
|---|---|
| Czy plik „Obudowy” (łącze w sPart) to obecny scrapPart pod starą nazwą, czy osobna kopia? | sPart → scrapPart |
| Który wybór progu jest właściwy: „od dołu” (sPart) czy „od góry” (lista powierzonych)? | scrapPart, iPart `iB_p7_Powierzone` |
| Czy Value rezystora ma zawierać PPM? Jeśli tak — według jakiej reguły? | dataPart H, iPart |
| Kto i kiedy ustawia flagę „?”? | dataPart flagi |
| Co oznacza wpis w flagCase w postaci dwóch słów bez daty (155 wpisów)? | dataPart D |
| Kto wypełnia PADS (kolumny N…X) i czy są zatwierdzane? | dataPart |
| Czy obudowy bez grupy w entScrap (ok. 1 000) wymagają przypisania? | scrapPart entScrap |
| Do czego służą kolumny Symbol / Nazwa PL / Result w entPart? | entPart |
| Czy kolumna Critical jest jeszcze potrzebna (1 wpis)? | dataPart L |

## 7. Współzależności z innymi plikami

| Kierunek | Plik | Arkusz/zakres | Kiedy | Moduł/procedura/formuła | Co przenosi |
|---|---|---|---|---|---|
| dataPart → iPart | iPart | partsData, resValue, capValue, ctceValue | Kroki 1–5 iPart | `iB_p1`…`iB_p5` | Value, defValue, Case, Footprint, Mounting, flagi, parametry [zgodne z wywiadem] |
| iPart → dataPart | iPart | partsData (nowe wiersze, flagi, Tagi), arkusze parametrów | Kroki 1–3, zatwierdzenie P&P | `iB_p1`, `iB_p3`, `iB_p5`, `iP_p1` | Nowe MPN, parametry, Case/Mounting/Footprint, zatwierdzenia, historia [zgodne z wywiadem] |
| resValue/capValue/ctceValue → partsData | dataPart | kolumna H | Stale (formuła) | formuła Value | Złożona Value [nowe — nieomawiane] |
| railmilCase → partsData | dataPart | kolumna F | Stale (formuła) | VLOOKUP | rmFootprint [nowe — nieomawiane] |
| dataPart (thtCase?) → comparator | comparator | biblioteka THT | Wczytanie biblioteki | kolekcja elementsTHT | Lista THT [niepewne] |
| scrapPart (entScrap) → iPart | iPart | entScrap: wiersz 1, tabela grup, obudowy od wiersza 23 | Generowanie listy powierzonych | `iB_p7_Powierzone` | Zapas dla powierzanych [zgodne z wywiadem] |
| scrapPart (entScrap) → fusePart | fusePart | entScrap | Generowanie listy powierzonych | `f3f_p2_EntPart` | Zapas dla powierzanych [nowe — nieomawiane] |
| „Obudowy” (scrapPart?) → sPart | sPart | nazwy `obudowa_zapasv3`, `obudowav3`, `ilośćv3`; ukryty arkusz „obudowy” w sPart | Przeliczanie formuł / aktualizacja łączy | formuła kolumny zapasu | Zapas zakupowy [różnica względem wywiadu] [niepewne] |
| iPart / fusePart → entPart | entPart (szablon) | Component List A–J | Generowanie listy | `iB_p7_Powierzone`, `f3f_p2_EntPart` | Linie powierzane + ilość z zapasem [zgodne z wywiadem] |
| entPart → Klient → (brak) | entPart (odesłany) | K–N | Po odesłaniu przez Klienta | brak | Odpowiedź Klienta — nie jest importowana [zgodne z wywiadem] |
