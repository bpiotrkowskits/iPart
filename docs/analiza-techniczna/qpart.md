---
status: roboczy
zakres: analiza techniczna pliku TS_KOSZTORYSANT v1.3 — najnowszej wersji niedoszłego qPart (estymator czasu i kosztorys oferty)
przegląd: 2026-11-23
---

# qPart (TS_KOSZTORYSANT v1.3) — analiza techniczna

Źródło: kod VBA wyciągnięty (tylko do odczytu) z `TS_KOSZTORYSANT_V1.3.xlsm` — 28 modułów, ok. 1 150 linii (z czego 15 modułów arkuszy i skoroszytu jest pustych), formuły arkuszy, przypisanie przycisków do makr oraz zapytanie Power Query. Plik nie ma formularzy (UserForm). Porównanie z ustaleniami z [wywiadu](../wywiad-obieg-plikow.md).

Oznaczenia: **[zgodne z wywiadem]**, **[różnica względem wywiadu]**, **[nowe — nieomawiane]**, **[niepewne]** — kod ani arkusze tego nie rozstrzygają.

Najważniejsza obserwacja na początek: **w tym pliku prawie cała logika siedzi w formułach arkusza, a nie w makrach.** Makra robią tylko cztery rzeczy: liczą punkty BOM, przywracają formuły nadpisane ręcznie, odświeżają kurs walut i budują harmonogram/kredyt. Dlatego opis poniżej obejmuje także formuły.

Kilka pojęć, które będą się powtarzać:

- **Formuła wejściowa** — komórka, która domyślnie liczy się formułą, ale użytkownik może wpisać w nią własną wartość. Przyciski „Przywróć formuły wejściowe” / „Odśwież kosztorys” wpisują formułę z powrotem.
- **Słownik obudów** — ukryty arkusz z listą nazw footprintów (z wariantami pisowni) i liczbą wyprowadzeń w danej grupie.
- **Punkt (lutu)** — jedno wyprowadzenie elementu do przylutowania. Koszt montażu w pliku liczy się od elementów i od punktów.
- **CPH** — nominalna wydajność linii SMT (ile elementów na godzinę układa automat).
- **Power Query** — wbudowany w Excel mechanizm pobierania danych z zewnątrz (tu: tabela kursów walut ze strony NBP).

## 1. Przegląd modułów i arkuszy

### Arkusze

| Arkusz | Rola |
|---|---|
| `BOM` | Wklejony BOM (ilość, desygnatory, footprint) i kalkulacja punktów w grupach RC/TT/DL/QL/BGA/TP/NS + THT/preTHT. |
| `KOSZTORYS` | Główny arkusz: dane podstawowe, dane PCB, czasy i koszty wszystkich etapów, tabele parametrów (stawki, maszyny), podsumowanie kosztorysu. |
| `OFERTA` | Przeliczenie kosztorysu na ofertę (waluta, narzuty, warianty „proponowany” i „szczegółowy”), kredyt kupiecki, harmonogram dostaw, przewidywany zysk. |
| `ESTYMACJA CZASOWA` | Same czasy (bez kosztów) dla kolejnych etapów — to jest właściwie „qPart”. Wszystko liczone formułami z KOSZTORYS. |
| `FORMULARZ PCBA` | Formularz pakietu (montaż, stop, klasa IPC, usługi dodatkowe, „dane procesu” do opisania). Część pól liczona, większość zaznaczana ręcznie. |
| `WERYFIKACJA WYMAGAŃ` | Lista kontrolna przeglądu wymagań przed przyjęciem zlecenia (wymaganie systemu jakości) — cztery pytania TAK/NIE, data i osoba. |
| `NOWE OBUDOWY` (ukryty) | Miejsce na footprinty spoza słowników (zapełniane przez nieaktywne makro). |
| `SUMA_RC`, `SUMA_TT`, `SUMA_DR`, `SUMA_QR`, `SUMA_BGA`, `SUMA_TP`, `SUMA_NS` (ukryte) | Słowniki obudów: nazwa footprintu (wiele wariantów pisowni) → liczba wyprowadzeń w każdej grupie. Od kilkuset do ok. 47 tys. wierszy. |

### Moduły

| Moduł | Przycisk (arkusz) | Rola |
|---|---|---|
| `Module9` → `KalkulujBOM` | „Kalkuluj BOM” (BOM) | Szuka footprintu w słownikach SUMA_* i wpisuje liczbę wyprowadzeń w kolumnach grup. |
| `Module10` → `WyczyscKalkulacje`, `WyczyscBOM` | „Wyczyść Kalkulacje”, „Wyczyść Arkusz” (BOM) | Czyści kolumny kalkulacji albo cały wklejony BOM (z podwójnym potwierdzeniem). |
| `Module8` → `AktualizujDaneZBazyObudow` | „Aktualizuj bazę obudów” (BOM) | **Wyłączone** (`Exit Sub` na początku). Miało kopiować słowniki SUMA_* z osobnego skoroszytu „Baza Obudow”. |
| `Module11` → `PrzenoszenieDanych` | „Przepisz nowe obudowy” (BOM) | **Wyłączone** (`Exit Sub`). Miało przenosić ręcznie uzupełnione nowe footprinty do arkusza NOWE OBUDOWY. |
| `Module2` → `OdswiezKosztorys`, `OdswiezSMT`, `OdswiezTHT` | „Odśwież Kosztorys”, „Odśwież Kosztorys SMT/THT” (KOSZTORYS) | Przywracają formuły wejściowe, ustawiają strony montażu, cenę szablonu, odświeżają kurs. |
| `Module3` → `Etykietowanie`, `depanelizacja` | „Przywróć formuły wejściowe” ×2 (KOSZTORYS) | Przywracają formuły w sekcjach etykietowania i depanelizacji. |
| `Module5` → `Waluta` | „AKTUALIZUJ KURS WALUTY” (OFERTA) | Odświeża tabelę kursów (Power Query, NBP) i formułę kursu w OFERTA. |
| `Module1` → `ofertaeksport` | „Odśwież dane z kosztorysu” (OFERTA) | Wpisuje na nowo formuły wariantu szczegółowego oferty (z przesunięciem wierszy po harmonogramie). `oferta()` — cała zakomentowana. |
| `Module12` → `FiltrujKopiujDane`, `FiltrujKopiujDaneProponowany`, `ZabezpieczArkusz` | „Kopiuj Wariant Szczegółowy/Proponowany” (OFERTA) | Filtruje niezerowe pozycje oferty, kopiuje je do schowka (do wklejenia w dokument oferty), po 10 s blokuje arkusz. |
| `Module4` → `Harmonogram` | „WYZNACZ HARMONOGRAM” (OFERTA) | Buduje listę miesięcy dostaw i dzieli ilość płytek na transze. |
| `Module6` → `Kredyt` | „OBLICZ ODSETKI” (OFERTA) | Liczy odsetki od kosztu materiałów i usług wg transz. |
| `Module14` → `MONTAZSMTB…E`, `MONTAZTHTB…E` | brak przycisku | Kopiują komórkę do C43/C44 — [niepewne] pozostałość po starszym układzie (te adresy w obecnych arkuszach nie mają takiej roli). |
| `Module7` → `Makro2` | brak przycisku | Nagrane makro (blokada arkusza bez hasła) — pozostałość. |
| `Ten_skoroszyt`, `Arkusz1…16` | — | Puste. **Brak automatów** (nic nie dzieje się przy otwarciu ani zapisie). |

## 2. Mechanizmy

### 2.1. Wejście: wklejenie BOM

- **Wejście:** BOM Klienta wklejany ręcznie do arkusza BOM od wiersza 6 (do ok. 400 pozycji): kolumna D — ilość (Qty), E — desygnatory, F–J — dowolne kolumny Klienta (nagłówki w wierszu 5 kasuje „Wyczyść Arkusz”), K — **footprint**. Kolumny S (wyprowadzenia THT) i T (preTHT — przygotowanie elementów THT, np. docinanie nóżek) wpisuje się ręcznie.
- **Reguła:** nie ma importu z pliku ani z iPart — tylko wklejenie. Kluczem do dalszych obliczeń jest **nazwa footprintu**, a nie MPN.
- **Wynik:** dane w BOM!D:K.
- [zgodne z wywiadem] — wycena pracy powstaje na surowym BOM Klienta, bez comparatora i iPart.

### 2.2. „Kalkuluj BOM” — klasyfikacja obudów i punkty

- **Wejście:** BOM!E (desygnator), BOM!K (footprint), słowniki SUMA_*.
- **Reguła** (`KalkulujBOM`), dla każdego wiersza z białym tłem kolumny K:
  1. Jeśli **pierwsza litera desygnatora to C albo R**, szuka footprintu w SUMA_RC → kolumna L (RC).
  2. Jeśli nie znaleziono (wynik 0), szuka kolejno w SUMA_TT → M, SUMA_DR → N (DL), SUMA_QR → O (QL), SUMA_BGA → P. Zatrzymuje się na pierwszym trafieniu.
  3. **Niezależnie** od powyższego szuka w SUMA_TP → Q (liczba pól termicznych) i SUMA_NS → R (znacznik elementu niestandardowego).
  4. Jeśli suma L:R = 0, wiersz L:T dostaje jasnopomarańczowe tło („obudowa nieznana — uzupełnij ręcznie”).
- **Wynik:** liczby wyprowadzeń w kolumnach L:R. Formuły w kolumnach A:C i wierszach 1–3 liczą: liczbę pozycji, liczbę elementów (× Qty) i liczbę punktów (Qty × wyprowadzenia) — osobno RC, SMT (TT…NS) i THT (S+T).
- Słowniki SUMA_* zawierają **warianty pisowni** tej samej obudowy (np. „X 123”, „X-123”, „X_123”), bo dopasowanie jest dokładne.
- Grupy RC, TT, DL, QL, BGA, TP, NS, THT, preTHT odpowiadają kolumnom PADS w dataPart (tam: RC, TT, DL, QL, BGA, ThP, NonS, THT, preTHT, pressFIT, Mech). W słownikach DL/QL nazywają się DR/QR. Różnica: **dataPart trzyma PADS po MPN, kosztorysant po nazwie footprintu**.
- [różnica względem wywiadu] — w wywiadzie ustalono, że PADS w dataPart zasilają estymację czasu. W kodzie kosztorysant **nie czyta dataPart**: ma własne słowniki footprintów. Związek jest koncepcyjny (te same grupy), nie techniczny.

### 2.3. Kosztorys — dane podstawowe i PCB (ręcznie)

- **Wejście ręczne w KOSZTORYS:** osoba wyceniająca (lista wyboru), numer POE, numer ZO, Klient, projekt, waluta, technologia (stop SAC lub SnPb), klasa IPC (2/3), **status Klienta** (nowy / sporadyczny / stały), **status projektu** (nowy pakiet / nowa dokumentacja / zmiany / aktualna), ilość płytek, panel dwurzędowy, liczba PCB w panelu, wymiary płytki i panelu.
- **Liczone:** liczba paneli; strony montażu SMT i THT (TOP / BOT / TOP+BOT / brak) — `OdswiezKosztorys` ustawia **TOP+BOT, jeśli jest choć jedna pozycja**, inaczej „—”; liczby pozycji, elementów i punktów pobierane z arkusza BOM.

### 2.4. Wdrożenie nowego produktu i uruchomienie montażu

- **Reguła:** czas weryfikacji dokumentacji i przygotowania programów = stała zależna od statusu projektu + stała zależna od stron montażu + czas na każdą pozycję SMT / AOI / THT (sekundy na pozycję z tabeli statusów) — razy stawka godzinowa. Status „zmiany” i „aktualna” zeruje koszt wdrożenia.
- Uruchomienie montażu (obsługa projektu, przyjęcie komponentów, SMT+AOI, THT) — podobnie: stała + sekundy na pozycję i element, mnożnik dla statusu Klienta (nowy > sporadyczny > stały); przyjęcie komponentów rośnie z pierwiastkiem 20. stopnia z ilości płytek. Dla stopu SnPb dolicza się stała kwota.
- **Wynik:** jednorazowe koszty „Wdrożenie nowego produktu” / „Aktualizacja rewizji” i „Uruchomienie montażu”.

### 2.5. Szablon (sito do pasty)

- **Wejście:** punkty TOP i BOT — liczone z płytki albo z panelu (przełącznik); `OdswiezKosztorys` wpisuje tu komunikat „uzupełnij ilość punktów”, jeśli montaż jest dwustronny.
- **Reguła:** koszt = kwota bazowa do progu punktów + dopłata za każdy punkt ponad próg (osobno koszt i cena).
- [nowe — nieomawiane] — **podział punktów na TOP/BOT jest wpisywany ręcznie**, choć mógłby wynikać z P&P (kolumna Layer).

### 2.6. Montaż SMT — trzy sposoby wyceny

1. **Cena z punktu standard:** elementy RC × cena + punkty pozostałe × cena; ceny w trzech progach ilości (<100, 100–999, ≥1000 szt.), z dopłatą procentową dla klasy IPC 3.
2. **Cena z punktów ważonych:** dla każdej grupy RC/TT/DL/QL/BGA/TP/NS: elementy × cena położenia + (punkty − elementy) × cena punktu grupy.
3. **Kosztorys z czasów pracy maszyn** — wybierany jednym z trzech „rodzajów wyceny”:
   - **Model matematyczny:** przygotowanie produkcji [h] = (pozycje/10 + elementy/1200) × ilość^(1/20); przygotowanie linii [h] = (pozycje/30 + elementy/3600) × ilość^(1/60) (× 5/8, gdy montaż jednostronny); liczba pracowników z czasu przygotowania linii; **czas montażu na panel** = Σ (elementy grupy na stronie × waga × czas na element), gdzie wagi to 1,11 (RC, TT), 1,25 (DL, QL, BGA), 1,43 (NS), a czas na element = 1 / (80% × CPH linii); elementy dzielone na TOP/BOT proporcjonalnie do punktów szablonu; czas nie krótszy niż minimalny czas cyklu linii.
   - **Prognoza produkcyjna:** ręcznie podany czas [h] + procentowy zapas.
   - **Zestawienie ze zrealizowanych:** ręcznie podany czas ostatniego zlecenia.
- **Parametry maszyn** (tabela w KOSZTORYS): 5 linii SMT — nominalne CPH (dla 3 linii), minimalny czas cyklu, koszt godziny linii w klasie 2 i 3. Wybór linii ręcznie.
- **Kontrola AOI:** kontrola pierwszej płytki (stała zależna od stron + elementy), procent kontroli śródprocesowej, czas kontroli; koszt godziny zależny od stopu i klasy IPC.
- **Wynik:** koszt panelu i płytki dla SMT i AOI.

### 2.7. Montaż THT

- Analogicznie: cena z punktu standard (punkty THT, próg ilości, dopłata za montaż dwustronny), cena z punktów ważonych (THT + preTHT) i kosztorys z czasów maszyn: trzy maszyny do lutowania na fali + montaż ręczny + silikonowanie. Czas fali na panel = czas cyklu + 0,5 × (elementy × 5 s + punkty × 0,5 s) (dla fali dwurzędowej połowa). Montaż ręczny i silikonowanie liczone z elementów i punktów.
- Kontrola THT: pierwsza płytka + procent kontroli + czas kontroli.

### 2.8. Usługi i logistyka

- **Logistyka (ręcznie):** PCB tooling, koszt 1 PCB, **koszt kompletu komponentów na płytkę**, wartość komponentów powierzonych (opis w arkuszu: „nie wliczamy do oferty, wliczamy do kredytowania — tymczasowo funkcja nie działa”).
- **Etykietowanie** (standardowe / niestandardowe / oba; liczba etykiet liczona z paneli), **depanelizacja** (V-cut / mostki / oba / brak — liczba cięć liczona z wymiarów płytki i panelu), **mycie** (liczba płytek w ramie myjki z wymiarów, cykle), **pakowanie** (rodzaj torebek, wysokie elementy, liczba torebek i kartonów z wymiarów, czas pakowania).
- Pozostałe usługi (RTG, tryb przyspieszony, wysyłka, podklejanie anten, testy, lakierowanie, zalewanie, materiały, montaż mechaniczny, obudowa, montaż finalny) — **cena wpisywana ręcznie**.
- **Wynik:** tabela „Podsumowanie kosztorysu” (KOSZTORYS, wiersze 171–199).

### 2.9. Estymacja czasowa (właściwy „qPart”)

- Arkusz ESTYMACJA CZASOWA pokazuje **tylko czasy** w h/min: weryfikacja dokumentacji i programy (SMT, AOI, THT), przygotowanie techniczne i technologiczne do partii (obsługa projektu, przyjęcie komponentów, SMT, AOI, THT), dział SMT (przygotowanie produkcji, linii, liczba pracowników, linia, czas montażu, kontrola pierwszej płytki, % kontroli, czas kontroli), dział THT (analogicznie + montaż ręczny i silikonowanie).
- Wszystkie wartości to formuły z KOSZTORYS. Część formuł jest **skopiowana, a nie odwołuje się** do KOSZTORYS — patrz ryzyka (5).
- [zgodne z wywiadem] — etapy pokrywają się z „wymarzonym qPart” z wywiadu, poza kompletacją, operacjami dodatkowymi i pakowaniem, które mają tu tylko koszt, nie czas (pakowanie liczy czas wewnętrznie).

### 2.10. Oferta

- **Wejście:** wynik KOSZTORYS, waluta i kurs, decyzje w kolumnie „Czy dodać narzut?” / „Decyzja/narzut” (np. szablon „NIE” = nie doliczaj, PCB tooling „w cenie PCB” / „w cenie wdrożenia”, etykietowanie „w cenie montażu SMT/THT” albo „oddzielnie”, kredyt „w kosztach materiału” / „oddzielnie” / „nie naliczaj”).
- **Reguła:** ceny przeliczone na walutę oferty (zaokrąglenia do 1, 0,01 albo 50); marża przez procentowe narzuty na pozycje kosztowe.
- **Wynik:** dwa warianty — **proponowany** (pakiet jako jedna cena + koszty jednorazowe) i **szczegółowy** (każda pozycja osobno); „Przewidywany zysk” SMT/THT/usługi. „Kopiuj Wariant…” kopiuje niezerowe wiersze do schowka.

### 2.11. Kurs walut

- `Waluta` odświeża zapytanie Power Query, które **pobiera tabelę średnich kursów NBP ze strony internetowej** i zostawia USD i EUR (PLN = 1). Wymaga internetu. Kurs trafia do KOSZTORYS i OFERTA.
- [nowe — nieomawiane] — jedyne zewnętrzne źródło danych pliku.

### 2.12. Harmonogram i kredyt

- `Harmonogram`: z liczby miesięcy i daty startu tworzy listę miesięcy; ilość płytek dzieli równo na transze, **pierwszy miesiąc = 0 szt.**; wstawia/usuwa wiersze w arkuszu OFERTA (przesuwa wszystko poniżej); pokazuje „Przypisaną ilość” na czerwono, gdy suma ≠ ilość.
- `Kredyt`: dla każdego miesiąca odsetki od niespłaconej części kosztu materiałów (oprocentowanie kredytu + magazynowania) i usług (magazynowanie) / 12. Suma odsetek → „Koszt kredytu” w kosztorysie.
- [nowe — nieomawiane].

### 2.13. Formularz PCBA i weryfikacja wymagań

- FORMULARZ PCBA: pola montażu SMT/AOI/THT (TOP/BOT), stop, klasa IPC są **liczone** z KOSZTORYS (pola wyboru zaznaczają się same); usługi dodatkowe (AQAP, certyfikat zgodności, traceability, azot, RTG, kleje, programowanie, testy, mycie, lakierowanie, zalewanie, depanelizacja, obudowa, etykiety, pakowanie) — **ręcznie**; „Dane procesu” — 10 pól opisu z wyborem obszaru (komponenty, PCB, szablon, SMT, THT, jakość, usługi).
- WERYFIKACJA WYMAGAŃ: cztery pytania TAK/NIE (wymagania Klienta, prawne, zasoby, komponenty), data i osoba z listy — formalny ślad przeglądu przed przyjęciem zlecenia.
- [nowe — nieomawiane].

## 3. Reguły biznesowe zaszyte w kodzie i formułach

1. Klasyfikacja obudów po **nazwie footprintu** w słownikach z wariantami pisowni; dopasowanie dokładne.
2. RC tylko dla desygnatorów zaczynających się na **C lub R**; RC → TT → DL → QL → BGA — pierwsze trafienie wygrywa; TP i NS zawsze sprawdzane dodatkowo.
3. Liczba punktów = Qty × wyprowadzenia; pozycje / elementy / punkty liczone osobno dla RC, SMT i THT.
4. Ceny jednostkowe w trzech progach ilości (<100, 100–999, ≥1000) i z dopłatą dla klasy IPC 3.
5. Model czasu SMT: przygotowanie zależne od pozycji i elementów, skalowane ilością^(1/20) lub ^(1/60); montaż z wag grup (1,11 / 1,25 / 1,43) i 80% CPH linii; minimalny czas cyklu.
6. Trzy rodzaje wyceny czasu: model matematyczny, prognoza produkcyjna (ręcznie + zapas), zestawienie ze zrealizowanych (ręcznie).
7. Mnożniki statusu Klienta (nowy / sporadyczny / stały) i statusu projektu (nowy pakiet / nowa dokumentacja / zmiany / aktualna) — wpływają na wdrożenie i uruchomienie.
8. Stop SnPb — dopłata procentowa do AOI i stała dopłata do uruchomienia montażu (opisane w arkuszu jako zalecenie).
9. Wymiary PCB < 52×52 mm i grubość ≤ 1,25 mm mają opisane dopłaty czasu (+50% SMT; +30% SMT, +60% THT) — **[niepewne]** opis jest w legendzie, nie znalazłem formuły, która je stosuje.
10. Waluta: średni kurs NBP; zaokrąglenia ofertowe do 1 zł / 0,01 / 50.

## 4. Odpowiedzi na otwarte pytania z wywiadu

| Pytanie | Co mówi plik |
|---|---|
| Kto wypełniałby qPart? | Plik pokazuje, **co** trzeba wypełnić — patrz tabela niżej. Większość danych technicznych dałoby się pobrać automatycznie z iPart/dataPart; ręcznie zostają dane handlowe oferty i parametry maszyn. |
| Czy PADS w dataPart zasilają estymację? | Koncepcyjnie tak (te same grupy), technicznie nie — kosztorysant ma własne słowniki po footprincie i nie czyta dataPart. |

**Co mogłoby pochodzić automatycznie, a co wymaga ręcznego wprowadzenia** (propozycja dla aplikacji):

| Dana w kosztorysancie | Dziś | Mogłaby pochodzić z | Kto wprowadza, jeśli ręcznie |
|---|---|---|---|
| Ilość, desygnatory pozycji | Wklejenie BOM | iPart (rdzeń BOM, także w trybie uproszczonym dla ofert) | — |
| Grupy RC/TT/DL/QL/BGA/TP/NS na pozycję | Słowniki footprintów | dataPart (PADS po MPN) / Case z iPart | Opiekun Projektu przy nowym komponencie (raz, do bazy) |
| Wyprowadzenia THT, preTHT | Ręcznie | dataPart (kolumny THT, preTHT) | j.w. |
| Strony montażu, podział punktów TOP/BOT | Ręcznie / domyślnie TOP+BOT | P&P (Layer) w iPart | — |
| Status projektu (nowy / zmiany / aktualna) | Ręcznie | Historia rewizji pakietu w iPart | — |
| Status Klienta | Ręcznie | Karta Klienta (liczba wcześniejszych ZK) | — |
| Koszt kompletu komponentów | Ręcznie | Wycena zakupowca (sekcja Wycena sPart) | Zakupowiec |
| Ilość płytek, waluta, harmonogram, kredyt | Ręcznie | — | Opiekun Klienta |
| Panel, wymiary, grubość PCB | Ręcznie | — (dokumentacja PCB) | Opiekun Klienta / Technolog |
| Klasa IPC, stop, usługi dodatkowe | Ręcznie | Wymagania pakietu (raz na pakiet) | Opiekun Klienta |
| Parametry maszyn, stawki, wagi modelu | W tabelach arkusza | Konfiguracja centralna | Kierownictwo / Technolog (raz) |
| Czas „ze zrealizowanych” | Ręcznie | Rzeczywiste czasy produkcji (dziś nie ma takich danych) | — |

## 5. Ryzyka i słabe punkty

1. **Prawdopodobny błąd w OFERTA (koszt silikonowania):** formuła kosztu silikonowania odwołuje się do komórki `KOSZTORYS!Q1177` (poza tabelą — pusta) zamiast do stawki silikonowania (prawdopodobnie Q117/Q118). Silikonowanie pokazuje się w ofercie z kosztem 0 i nie trafia do ceny montażu THT w ofercie (choć KOSZTORYS je liczy).
2. **Prawdopodobny błąd w KOSZTORYS (mycie):** w podsumowaniu kosztorysu pozycja „Mycie” bierze koszt z komórki etykietowania niestandardowego (`E143`), a nie z sekcji mycia (`E157`). OFERTA liczy mycie poprawnie — rozjazd między kosztorysem a ofertą.
3. **Makro „Odśwież dane z kosztorysu” zmienia logikę kredytu:** obecne formuły doliczają kredyt do PCB i komponentów, gdy wybrano „w kosztach materiału”; makro wpisuje formuły z warunkiem „oddzielnie w ofercie”. Po kliknięciu kredyt przy opcji „oddzielnie” jest liczony podwójnie (w materiale i osobnym wierszu), a przy „w kosztach materiału” wcale.
4. **Warunek `C20=D20=P39`** (silikonowanie TOP/BOT) — Excel porównuje najpierw C20 z D20 (PRAWDA/FAŁSZ), a potem wynik z tekstem, więc warunek jest zawsze fałszywy. [niepewne] jaki był zamiar.
5. **Kopie formuł zamiast odwołań:** ESTYMACJA CZASOWA liczy czasy własnymi formułami skopiowanymi z KOSZTORYS. Przyjęcie komponentów ma inne współczynniki (1/10, 1/1200) niż w kosztorysie (1/7, 1/720); czasy SMT i AOI w przygotowaniu do partii mają identyczną formułę. Czas pokazany w estymacji ≠ czas, z którego policzono koszt.
6. **Formuły nadpisywane ręcznie** i przywracane przyciskami — nie widać, które komórki są wpisane ręcznie, a które liczone; po „Odśwież Kosztorys” ręczne wpisy giną bez ostrzeżenia.
7. **Twarde adresy komórek i przesuwanie wierszy:** makra odwołują się do konkretnych komórek (np. D74, C72); „Harmonogram” wstawia wiersze, więc makra oferty liczą przesunięcie („dystans”) — każda zmiana układu arkusza psuje makra. `Module14` wygląda na pozostałość po starszym układzie.
8. **„Kalkuluj BOM” nie czyści starych wyników:** gdy footprint przestanie być znaleziony, poprzednia wartość w kolumnie zostaje. RC rozpoznawane po pierwszej literze komórki desygnatorów (lista „C1, C2…” działa, ale np. „RN1” trafi do RC).
9. **Słowniki obudów** (do 47 tys. wierszy wariantów pisowni) są utrzymywane poza dataPart — dwa źródła tej samej wiedzy; aktualizacja z „Baza Obudow” jest wyłączona.
10. Brak automatów i brak obsługi błędów — pusty arkusz daje wiele `#DIV/0!`; kurs walut wymaga internetu przy każdym „Odśwież Kosztorys”.
11. Hasło do arkuszy jest wpisane w kod (stała w makrach) — znane każdemu, kto otworzy edytor VBA.
12. Komponenty powierzone — w arkuszu oznaczone jako „tymczasowo funkcja nie działa”.

## 6. Pytania do potwierdzenia przez autora

| Pytanie | Moduł / miejsce |
|---|---|
| Czy TS_KOSZTORYSANT jest dziś używany przez Opiekunów Klienta do każdej oferty, czy tylko okazjonalnie? (w wywiadzie: „wycena pracy w łopatologiczny i przestarzały sposób”) | cały plik |
| Co oznaczają numery POE i ZO? Czy pochodzą z Nexo lub ProduMan? | KOSZTORYS, dane podstawowe |
| Gdzie jest i kto utrzymuje skoroszyt „Baza Obudow”? Czy słowniki SUMA_* są nadal aktualizowane? | `Module8`, SUMA_* |
| Czy słowniki footprintów mogą zostać zastąpione PADS z dataPart (po MPN)? Co z ofertami, gdzie Klient podaje tylko footprint? | `KalkulujBOM` |
| Który z trzech sposobów wyceny SMT (punkt standard / ważony / czasy maszyn) jest dziś wiodący w ofercie? | KOSZTORYS, OFERTA |
| Czy wagi 1,11 / 1,25 / 1,43 i 80% CPH zostały zmierzone, czy przyjęte szacunkowo? | KOSZTORYS, model SMT |
| Czy dopłaty za małe (<52×52 mm) i cienkie (≤1,25 mm) płytki powinny działać? Nie znalazłem formuły, która je stosuje. | KOSZTORYS, legenda |
| Silikonowanie w OFERTA (`Q1177`) — literówka? Jaka powinna być komórka? | OFERTA, montaż THT |
| Mycie w podsumowaniu KOSZTORYS (`E143`) — literówka zamiast `E157`? | KOSZTORYS, podsumowanie |
| Który warunek kredytu jest właściwy — w formułach arkusza czy w makrze „Odśwież dane z kosztorysu”? | `Module1.ofertaeksport` |
| Dlaczego pierwszy miesiąc harmonogramu ma zawsze 0 szt.? | `Module4.Harmonogram` |
| Czy „Zestawienie ze zrealizowanych” ma kiedyś pobierać rzeczywiste czasy z produkcji? Skąd? | KOSZTORYS, rodzaj wyceny |
| Kto powinien utrzymywać parametry linii i stawki — Technolog, kierownik produkcji, zarząd? | KOSZTORYS, tabele P:S |
| Czy Formularz PCBA i Weryfikacja Wymagań są wypełniane i drukowane do dokumentacji jakościowej? | FORMULARZ PCBA, WERYFIKACJA WYMAGAŃ |

## 7. Współzależności z innymi plikami

| Kierunek | Plik / źródło | Arkusz / zakres | Kiedy | Moduł / procedura | Co przenosi |
|---|---|---|---|---|---|
| wejście (ręczne) | BOM Klienta | BOM!D:K | przy wycenie | — (wklejenie) | ilość, desygnatory, footprint — [zgodne z wywiadem: wycena na surowym BOM] |
| wejście (internet) | strona NBP — tabela średnich kursów | KOSZTORYS, tabela kursów (USD, EUR) | „AKTUALIZUJ KURS WALUTY”, „Odśwież Kosztorys” | `Waluta` + Power Query | kursy walut — [nowe] |
| wejście (wyłączone) | skoroszyt „Baza Obudow” (musi być otwarty) | SUMA_RC…SUMA_NS | „Aktualizuj bazę obudów” — dziś nic nie robi | `AktualizujDaneZBazyObudow` | słowniki footprintów — [nowe] [niepewne, czy plik istnieje] |
| wyjście (schowek) | dokument oferty (poza Excelem) | OFERTA, warianty proponowany / szczegółowy | „Kopiuj Wariant…” | `FiltrujKopiujDane*` | niezerowe pozycje oferty — [niepewne, dokąd wklejane] |
| wejście (ręczne) | wycena zakupowca | KOSZTORYS, logistyka (koszt komponentów na płytkę, PCB) | przy wycenie | — | koszt materiału — [niepewne, czy z pliku wyceny] |
| brak | iPart, dataPart, sPart, fusePart, cPart, dPart, entPart, scrapPart | — | — | — | **Kosztorysant nie czyta ani nie zapisuje żadnego pliku *Part.** — [różnica względem wywiadu: PADS z dataPart nie są dziś używane technicznie] |
