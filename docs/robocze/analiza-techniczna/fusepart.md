---
status: roboczy
zakres: analiza techniczna kodu VBA pliku fusePart (rev3.4)
przegląd: 2026-11-23
---

# fusePart — analiza techniczna kodu VBA

Źródło: kod VBA wyciągnięty (tylko do odczytu) z `fusePart.xlsm` — 15 modułów (8 z kodem, ok. 3 200 linii; 7 modułów arkuszy pustych), formuły wybranych arkuszy oraz układ przycisków na arkuszach. Dokument porównuje kod z ustaleniami z wywiadu ([Wywiad obieg plików](../Wywiad%20obieg%20plików.md)) i z analizą [iPart](ipart.md).

Oznaczenia: **[zgodne z wywiadem]**, **[różnica względem wywiadu]**, **[nowe — nieomawiane]**, **[niepewne]** — kod tego nie rozstrzyga.

Pojęcia z analizy iPart (makro, zdarzenie, słownik, normalizacja, twarda ścieżka) obowiązują tu tak samo. Dodatkowo:

- **Slot** — jedna z 15 kolumn (C…Q) w arkuszu iParts, w której opisany jest jeden łączony pakiet: numer ZK, ilość, nazwa projektu i ścieżka do pliku iPart.
- **Klucz łączenia** — tekst, po którym fusePart uznaje dwie linie z różnych pakietów za ten sam komponent.
- **Część wspólna (przecięcie)** — elementy obecne we wszystkich zbiorach naraz, np. MPN występujący we wszystkich łączonych pakietach.

## 1. Przegląd modułów

| Moduł | Przycisk | Rola |
|---|---|---|
| `f1S_p1_1ImpMulti` | „Importuj pliki iPart” (arkusz START) | Zbiera do 15 ścieżek iPart, numery ZK i ilości; dla każdego slotu wywołuje silnik importu. |
| `f1S_p1_2ImpBOM` | — (wywoływany przez powyższy) | Silnik importu jednego iPart do arkusza iParts: pierwszy import albo aktualizacja z pogrubieniem zmian; przepisuje komentarze z START źródła. |
| `f2i_p1_fuse` | „Wygeneruj połączony BOM” (arkusz iParts) | Łączy linie wszystkich pakietów w arkuszu fuseBOM po kluczu Part TS/Case; liczy sumy, różnice, wspólne MPN; kopia zapasowa poprzedniego fuseBOM. |
| `f3f_p1_Confirm` | „Zatwierdź” (arkusz fuseBOM) | Zatwierdza fusePart (data + login), nadaje nazwę „fuse_…” i zapisuje plik w folderze dokumentacji do zamówienia. |
| `f3f_p2_EntPart` | „Wygeneruj Listę Komponentów Powierzonych” (arkusz fuseBOM) | Tworzy plik listy powierzonych (PL/EN) z fuseBOM, z ilością z zapasem wg scrapPart. |
| `Public_Function` | — | Wspólne funkcje: stan aplikacji, wyszukiwanie nagłówków, mapowanie kolumn, numer ZK, kolorowanie wg czasu, Klient z folderu. |
| `Public_Const` | — | Hasło do arkuszy (stała w kodzie), lista niedozwolonych znaków. |
| `Ten_skoroszyt`, `Arkusz2…18` | — | Puste — **brak zdarzeń** przy otwarciu i zapisie. |

Przyciski sprawdzone w rysunkach arkuszy:

| Arkusz | Napis na przycisku | Podpięte makro |
|---|---|---|
| START | Importuj pliki iPart | `ImportMultipleBOMData` |
| START | Eksportuj Historię Realizacji | `ImportujDaneZeStarego_iPart` — **takiego makra nie ma w fusePart** (nazwa pochodzi z iPart) |
| iParts | Wygeneruj połączony BOM | `BuildFuseBOM_NewVsOld` |
| fuseBOM | Zatwierdź | `Confirmation` |
| fuseBOM | Wygeneruj Listę Komponentów Powierzonych | `entPart` |

Arkusze `MPN zatwierdzone`, `MPN niedozwolone`, `MPNz_log`, `MPNn_log` nie mają kodu. Kolumny A–N to formuły kopiujące rdzeń z fuseBOM (A = FuseID, B…N = Confirmed…Data Aktualizacji, z pominięciem kolumny MPN Zatwierdzone); kolumny MPN od O w prawo są wypełniane ręcznie.

## 2. Mechanizmy

### 2.1. Import plików iPart (arkusz iParts)

- **Wejście:** ścieżki plików iPart w iParts!C7:Q7 (do 15), numery ZK w wierszu 4, ilości w wierszu 5, nazwy projektów w wierszu 6; w każdym pliku źródłowym arkusze `BOM`, `MPN zatwierdzone` i `START`.
- **Reguła:**
  1. Użytkownik w pętli dodaje pliki (okno wyboru, startowo w folderze Klienta na dysku sieciowym); maksymalnie 15.
  2. Puste pola slotu są uzupełniane: nazwa projektu — z nazwy folderu trzy poziomy nad plikiem; **numer ZK — wpisywany ręcznie** w formacie `ZK_numer_rrrr` (sprawdzany: tylko litery, cyfry, `_`, prefiks „ZK_”, rok na końcu); ilość — wpisywana ręcznie (slot z ilością ≤ 0 jest pomijany).
  3. Klient (iParts!H2) — z nazwy folderu dwa poziomy nad folderem dokumentacji firmy; użytkownik (H1) — login Windows.
  4. Każdy slot jest przetwarzany przez silnik importu (2.2).
- **Wynik:** arkusz iParts z liniami wszystkich pakietów, każda z ID `<numer ZK>|<pozycja>`; wpisy w arkuszu START.
- **Moduł:** `f1S_p1_1ImpMulti.ImportMultipleBOMData`.
- **[zgodne z wywiadem]** fusePart łączy kilka iPart z własnymi ZK i ilościami.
- **[nowe — nieomawiane]** Na początku każdego importu z całej tabeli iParts usuwane jest pogrubienie — widać tylko zmiany z ostatniego importu.

### 2.2. Silnik importu jednego iPart

- **Wejście:** arkusz `BOM` pliku iPart (otwieranego tylko do odczytu), opcjonalnie arkusz `MPN zatwierdzone` i `START`.
- **Reguła:**
  1. Wiersz startowy danych w źródle: 3 lub 11 (wg tego, gdzie w kolumnie A stoi „1”) — obsługa starszych i obecnych wersji iPart.
  2. Kolumny są dopasowywane **po nazwach nagłówków** (C…P w iParts ← kolumny 1…16 źródła). Wyjątek: jeśli źródło ma i „Case”, i „Footprint”, **Footprint jest pomijany**; jeśli ma tylko „Footprint”, trafia on do Case (stare iPart, gdzie Footprint znaczył Case).
  3. **ID linii = `<numer ZK bez roku>|<numer kolejny wiersza w źródle>`**. ID w kolumnie A źródła nie jest czytane — liczy się pozycja wiersza.
  4. Odczyt kończy się na **pierwszym wierszu z pustą kolumną 5** źródła (w iPart v3: Part TS).
  5. **Pierwszy import danego ZK** (brak linii z tym prefiksem): linie wpisywane po kolei na końcu tabeli, bez porównań.
  6. **Kolejny import:** każda linia odnajdywana po ID; wartości porównywane tekstowo — różnica = nadpisanie i **pogrubienie** komórki. Linia o nowym ID jest **wstawiana na końcu sekcji tego ZK**. Linie, które zniknęły ze źródła, zostają bez zmian.
  7. **EntPart:** „?” lub tekst „komponent powierzany (do potwierdzenia)” w Notes → „?”; ☑ lub tekst „komponent powierzany” w Notes → ☑; w pozostałych przypadkach wartość źródła.
  8. **MPN zatwierdzone:** z arkusza źródła, wiersz dopasowany po numerze w kolumnie A = pozycji linii; MPN-y **od kolumny P** w prawo łączone przecinkami (bez powtórzeń) w jedną komórkę iParts!M.
  9. Kolumna B (Data Aktualizacji) = teraz — **zawsze, dla każdej linii**, także bez zmian; pogrubiona tylko przy zmianie. Tło B kolorowane gradientem od białego do zielonego wg czasu aktualizacji (grupy co 59 s) — tylko przy kolejnych importach.
  10. **Log w START:** wiersz 15 (kolumny J…X) = numer ZK; wiersze 16–26 = kopia komentarzy Opiekuna Projektu z START!J16:J26 źródła (różnica = pogrubienie); lista „Historia Realizacji” od wiersza 34: numer ZK + ścieżka do iPart.
- **Wynik:** aktualne linie pakietu w iParts; iParts!C1 = czas importu.
- **Moduł:** `f1S_p1_2ImpBOM.AktualizujDaneZZewnetrznegoBOM`, `LogStartMessagesToMaster`.
- **[zgodne z wywiadem]** Zmiany z iPart są widoczne przez pogrubienie i gradient (podświetlenie wg czasu).
- **[różnica względem wywiadu]** fusePart nie odświeża danych sam przy otwarciu — trzeba ponownie kliknąć „Importuj pliki iPart”.
- **[nowe — nieomawiane]** Footprint z iPart nie trafia do fusePart (punkt 2). [niepewne] Czy to zamierzone — Footprint, o którym decyduje Technolog, nie jest dalej przekazywany przez fusePart?
- **[nowe — nieomawiane]** MPN-y czytane od kolumny P, podczas gdy w iPart v3 MPN1 jest w kolumnie O. [niepewne] Możliwe, że pierwszy MPN zatwierdzony każdej linii jest pomijany.
- **[nowe — nieomawiane]** MPN niedozwolone z iPart **nie są importowane w ogóle**.

### 2.3. Łączenie — „Wygeneruj połączony BOM”

- **Wejście:** arkusz iParts (od wiersza 11), sloty C…Q (ZK w wierszu 4, ilość w wierszu 5), dotychczasowy fuseBOM.
- **Reguła:**
  1. **Klucz łączenia** w iParts!R = formuła `Part TS & "/" & Case`. Przed porównaniem: obcięcie spacji na brzegach i usunięcie apostrofu na początku. Porównanie **bez rozróżniania wielkości liter**. Brak innej normalizacji (spacje wewnątrz, myślniki, zapis jednostek — muszą być identyczne).
  2. Linia trafia do łączenia tylko, gdy jej prefiks ZK z kolumny A (część przed „|”) odpowiada któremuś slotowi; numer ZK slotu skracany jest do samego numeru (bez roku).
  3. Dla każdego klucza zbierane są:
     - **macierz zamówień (R…AF, kolumna na ZK):** `ilość na PCB: desygnatory`; kilka linii jednego ZK z tym samym kluczem — ilości sumowane, desygnatory łączone przecinkiem; brak linii w ZK → „- / -”;
     - **SumaQty (F)** = Σ (Quantity linii × ilość w ZK) po wszystkich ZK;
     - **FuseDes (E)** = zestawienie „ZK: ilość: desygnatory” z niepustych pozycji macierzy;
     - **wartości kolumn D (Confirmed) i G…P** (Part TS, Case, Footprint, Description, Value, Part Number, MPN, EntPart, Mounting, Notes) — **lista różnych wartości** łączona „ / ”.
  4. **„Różnice” (C):** gdy w kolumnie jest więcej niż jedna różna wartość — komórka dostaje kolor (pomarańczowy), a nazwa kolumny trafia do C jako „Różnice w kolumnach: …”. Pusta wartość w jednym pakiecie i wypełniona w innym **też jest różnicą**.
  5. **MPN Zatwierdzone (M):** nadpisywane **częścią wspólną** — zostają tylko MPN-y obecne w liście każdego aktywnego slotu (wszystkich wpisanych ZK, nie tylko tych, w których klucz występuje).
  6. **Przed zatwierdzeniem fusePart** (fuseBOM!N2 puste) tabela budowana jest od zera przy każdym kliknięciu. **Po zatwierdzeniu** — dotychczasowa kolejność linii zostaje, nowe klucze dopisywane na końcu, a klucz, który zniknął, zostaje z pustymi danymi i „- / -” w całej macierzy.
  7. Po zatwierdzeniu każda zmieniona komórka (D…AF) jest pogrubiana względem kopii zapasowej, a Data Aktualizacji (Q) dostaje nowy czas; bez zmian — czas pozostaje stary. Q kolorowane gradientem wg czasu; macierz R…AF kolorowana odcieniami pomarańczu, gdy pakiety mają różne wpisy.
  8. Kolumna A (FuseID) **nie jest czyszczona** — w szablonie zawiera stałe numery 1, 2, 3…; po zatwierdzeniu istniejące klucze zachowują swój numer.
  9. Na koniec w iParts: Q = FuseID, S = Różnice dla każdej linii; iParts!R1 = czas łączenia.
  10. Każde kolejne kliknięcie tworzy **ukrytą kopię** całego fuseBOM (`fuseBOM_backup`, `(2)`, `(3)`…).
- **Wynik:** arkusz fuseBOM — jedna linia na komponent, z sumą do zakupu i macierzą ilości per ZK.
- **Moduł:** `f2i_p1_fuse.BuildFuseBOM_NewVsOld`.
- **[zgodne z wywiadem]** Klucz Part TS + Case; różnice nie blokują połączenia, tylko trafiają do kolumny „Różnice”; przegląda je osoba łącząca.
- **[różnica względem wywiadu]** Wywiad: „suma MPN niedozwolonych nie obowiązuje, a powinna”. W kodzie MPN niedozwolone **w ogóle nie przechodzą** z iPart do fusePart; arkusz „MPN niedozwolone” w fusePart jest wypełniany ręcznie.
- **[nowe — nieomawiane]** Wspólne MPN-y liczone są jako część wspólna **wszystkich** ZK w pliku. Komponent użyty tylko w części łączonych pakietów dostaje **pustą** listę MPN zatwierdzonych. [niepewne] Czy to zamierzone?
- **[nowe — nieomawiane]** Kolejność i FuseID są stabilne dopiero po zatwierdzeniu. Przed zatwierdzeniem numer FuseID jest po prostu numerem wiersza.

### 2.4. Zatwierdzenie fusePart

- **Wejście:** fuseBOM!N1; sloty iParts (ZK w wierszu 4, projekty w wierszu 6), Klient z H2.
- **Reguła:**
  1. Jeśli N1 puste: N1 = data, N2 = login. Brak innych warunków (np. sprawdzenia „Różnic” czy Part TS).
  2. Nazwa ZK do folderu: numery ZK pogrupowane wg roku i posortowane, np. `ZK_12_15_2026`.
  3. Proponowana nazwa: najdłuższy wspólny początek nazw projektów (min. 3 znaki), inaczej `fuse_<liczba>x`; użytkownik może wpisać własną. Zawsze z prefiksem `fuse_`.
  4. N3 = Klient, N4 = nazwa fuse, N5 = folder docelowy: `<Klient>\<projekt pierwszego slotu>\III. DOKUMENTACJA DO ZAMÓWIENIA\<nazwa ZK>` na dysku sieciowym (tworzony, jeśli brak).
  5. Zapis jako `fuse_<nazwa>_iPart.xlsm` w tym folderze (pytanie o nadpisanie). Stary plik **nie jest usuwany**.
- **Moduł:** `f3f_p1_Confirm.Confirmation`, `NowaNazwaPliku`.
- **[nowe — nieomawiane]** Zatwierdzenie przełącza tryb łączenia z „od zera” na „przyrostowy” (2.3, punkt 6). Nie da się go cofnąć z poziomu przycisku.

### 2.5. Lista komponentów powierzonych z fuseBOM

- **Wejście:** fuseBOM (EntPart, SumaQty, Case, Mounting, FuseDes; wiersze 2–3 od kolumny R), plik listy Klientów, szablony entPart/powPart i scrapPart arkusz `entScrap` — wszystkie z folderu szablonów na dysku sieciowym.
- **Reguła:**
  1. Język wg kodu kraju Klienta z listy (zaczyna się od „PL” → `Powierzany_<Klient>_<nazwa fuse>.xlsx`, inaczej `Entrusted_…`).
  2. Plik zapisywany **w folderze pliku fusePart**.
  3. Kopiowane są linie, w których EntPart zawiera **cokolwiek** (☑, „?”, nazwa lub połączenie kilku wartości „ / ”).
  4. Kolumny dopasowane po nagłówkach (Quantity ← SumaQty, Footprint ← Case, Designator ← FuseDes), poza kolumną J.
  5. J = ilość z zapasem: ta sama reguła progów co w iPart, liczona od **SumaQty** (całość wszystkich ZK). DNP → 0.
  6. Liczba pakietów w C8 = ilości ZK połączone „ | ”.
- **Moduł:** `f3f_p2_EntPart.entPart`.
- **[zgodne z wywiadem]** Lista powierzonych generowana także dla połączonych BOM-ów.
- **[nowe — nieomawiane]** Jeśli tylko jeden z łączonych pakietów ma pozycję powierzoną, do listy trafia **cała** SumaQty wszystkich pakietów.

### 2.6. Kierunki przepływu danych

| Kierunek | Co | Gdzie w kodzie |
|---|---|---|
| iPart → fusePart | rdzeń BOM (bez Footprint), EntPart, MPN zatwierdzone, komentarze z START | 2.2 |
| fusePart → plik dla Klienta | lista powierzonych | 2.5 |
| scrapPart → fusePart | zapas dla powierzanych (`entScrap`) | 2.5 |
| fusePart → sPart / cPart / dPart | **brak kodu w fusePart** — te pliki muszą same czytać fuseBOM | — |
| fusePart → iPart (synchronizacja wsteczna) | **brak kodu** | — |

## 3. Reguły biznesowe zaszyte w kodzie

1. **Klucz łączenia:** `Part TS/Case`, porównanie bez wielkości liter, tylko obcięte brzegi i apostrof.
2. **ID linii w iParts:** `<numer ZK bez roku>|<pozycja wiersza w iPart>` — opiera się na regule stałej kolejności linii iPart.
3. **Numer ZK:** format `ZK_numer_rrrr`; do kluczy używany jest sam numer (bez roku).
4. **SumaQty** = Σ Quantity × ilość w ZK; **macierz** per ZK: `ilość na PCB: desygnatory`, brak = „- / -”.
5. **Różnice:** każda kolumna rdzenia z więcej niż jedną różną wartością (pusta też się liczy) → kolor + nazwa kolumny w „Różnice”.
6. **MPN zatwierdzone w fuseBOM:** część wspólna list ze wszystkich aktywnych ZK.
7. **MPN niedozwolone:** nie są przenoszone — tylko ręczny wpis w fusePart.
8. **Footprint:** pomijany przy imporcie, gdy iPart ma kolumnę Case.
9. **EntPart:** ☑ / „?” / tekst z Notes (stare wersje iPart).
10. **Tryb łączenia:** od zera do zatwierdzenia; potem przyrostowo z zachowaniem kolejności i FuseID.
11. **Nazwa i miejsce pliku po zatwierdzeniu:** `fuse_<nazwa>_iPart.xlsm` w folderze dokumentacji do zamówienia, podfolder z numerami ZK.
12. **Zapas dla powierzanych:** reguła progów z `entScrap` jak w iPart, od SumaQty.

## 4. Odpowiedzi na otwarte pytania z wywiadu

| Pytanie z wywiadu | Co mówi kod |
|---|---|
| Czy przy łączeniu obowiązuje suma MPN niedozwolonych? | Nie — MPN niedozwolone nie są w ogóle importowane z iPart. Potwierdza odpowiedź z wywiadu, a nawet ją zaostrza. |
| Co trafia do „Różnice”? | Nazwy kolumn (Confirmed, Part TS, Case, Footprint, Description, Value, Part Number, MPN zatwierdzone, EntPart, Mounting, Notes), w których łączone linie mają różne wartości — także pusta kontra wypełniona. |
| Czy decyzje z fusePart wracają do iPart? | Nie — brak jakiegokolwiek kodu zapisującego do iPart. Zgodne z wywiadem. |
| Jak wyróżniane są zmiany? | Pogrubienie zmienionych komórek, gradient biało-zielony w kolumnie Data Aktualizacji wg czasu, pomarańczowe odcienie w macierzy ZK. Widać tylko zmiany z ostatniego importu/łączenia. |
| Format `[#,#] [#]` w Historii Zmian? | fusePart nie ma własnej historii zmian. Kolumna „Historia Zmian” w iParts to kopia kolumny z iPart (dopasowanie po nagłówku). |
| Co zawiera sekcja „Designators” w dPart_v3.f? | Najpewniej macierz R…AF z fuseBOM (ilość i desygnatory per ZK) — [niepewne] do potwierdzenia w analizie dPart. |

## 5. Ryzyka i słabe punkty

1. **Ten sam numer ZK z różnych lat.** Do kluczy używany jest numer ZK bez roku. Dwa ZK o tym samym numerze z różnych lat zostaną zmieszane w jednej sekcji iParts, a drugi slot zostanie pominięty przy łączeniu.
2. **Puste MPN zatwierdzone dla komponentów z części pakietów** (2.3, punkt 5). Jeśli sPart korzysta z tej kolumny, zakupy nie widzą zamienników dla takich linii.
3. **Footprint nie jest przenoszony** z iPart v3 — kolumna Footprint w fuseBOM jest pusta.
4. **Możliwe pominięcie MPN1** — odczyt od kolumny P zamiast O.
5. **MPN niedozwolone gubione** przy łączeniu — zabezpieczenie przed powtórzeniem błędu nie działa w ścieżce fusePart.
6. **Przerwanie importu na pustym Part TS** — linie iPart po pierwszej linii z pustą kolumną 5 nie są importowane, bez komunikatu.
7. **ID oparte na pozycji wiersza** — przestawienie lub wstawienie linii w iPart przypisze dane do złego ID (to techniczne źródło reguły „nie zmieniamy kolejności”).
8. **Puste wartości liczone jako różnice** — linie z danymi uzupełnionymi tylko w jednym pakiecie zawsze będą oznaczone jako konflikt, co zaszumia kolumnę „Różnice”.
9. **Lista powierzonych z całej SumaQty**, nawet gdy powierza tylko jeden z pakietów.
10. **Przycisk „Eksportuj Historię Realizacji”** jest podpięty pod makro nieistniejące w fusePart — kliknięcie skończy się błędem.
11. **Kopie zapasowe narastają** — każde łączenie dodaje ukryty arkusz `fuseBOM_backup (n)`; plik rośnie, nic ich nie usuwa.
12. **Brak historii zmian i logów MPN** — fusePart nie ma zdarzeń przy zapisie (w przeciwieństwie do iPart); ręczne zmiany w fuseBOM nie są nigdzie rejestrowane, a arkusze `_log` nie mają kodu.
13. **Brak aktualizacji szablonu** — fusePart nie ma mechanizmu MASTER/ForwardUpdate z iPart; nowe makra trzeba przenosić ręcznie.
14. **Twarde adresy:** sloty C…Q, wiersze 4–7, START J15:X27 i J34…, fuseBOM N1–N5, R…AF, A11… (FuseID), entScrap (wiersz 1, od 3 i od 23).
15. **Ścieżki sieciowe wpisane w kod** (folder dokumentacji, folder szablonów, plik listy Klientów, scrapPart) oraz Klient z nazwy folderu.
16. **Stary plik nie jest usuwany po zatwierdzeniu** — mogą istnieć dwie kopie fusePart (robocza i zatwierdzona).
17. **Etykieta „numerZK” w module entPart czyta wiersz z nazwami projektów** — bez skutku dla wyniku (wartość używana tylko do sprawdzenia, czy wiersz jest niepusty), ale myląca przy zmianach kodu.

## 6. Pytania do potwierdzenia przez autora

| Pytanie | Moduł |
|---|---|
| Czy pomijanie Footprint przy imporcie (gdy iPart ma Case) jest zamierzone? Skąd sPart/cPart/dPart biorą Footprint dla połączonych BOM-ów? | `Public_Function.BuildHeaderMap` |
| Czy MPN zatwierdzone mają być czytane od kolumny O (MPN1), a nie od P? | `f1S_p1_2ImpBOM` |
| Czy wspólne MPN-y mają być częścią wspólną wszystkich ZK, czy tylko tych, w których komponent występuje? | `f2i_p1_fuse.ComputeCommonMPN` |
| Czy MPN niedozwolone powinny być automatycznie sumowane z iPart (zgodnie z potrzebą z wywiadu)? | `f1S_p1_2ImpBOM` |
| Czy klucz łączenia powinien być normalizowany mocniej (spacje wewnętrzne, zapis jednostek), np. jak w iPart? | `f2i_p1_fuse.CleanKey` |
| Czy pusta wartość w jednym pakiecie powinna być traktowana jako różnica? | `f2i_p1_fuse.WriteDistinctToCell` |
| Jak obsłużyć ten sam numer ZK z różnych lat? | `Public_Function.ExtractZkPrefix` |
| Co ma robić przycisk „Eksportuj Historię Realizacji” na START? | (brak makra) |
| Czy lista powierzonych powinna liczyć ilość tylko z ZK, w których pozycja jest powierzona? | `f3f_p2_EntPart` |
| Kto i kiedy wypełnia ręcznie arkusze MPN zatwierdzone/niedozwolone w fusePart i czy ktoś czyta je dalej (sPart)? | arkusze MPN |
| Czy wypełniane są kolumny sPart/cPart/dPart w „Historii Realizacji” na START (kod fusePart wpisuje tylko ZK i ścieżkę iPart)? | `LogStartMessagesToMaster` |
| Czy zatwierdzenie fusePart powinno wymagać przejrzenia „Różnic” (np. blokada przy nierozstrzygniętych konfliktach)? | `f3f_p1_Confirm` |
