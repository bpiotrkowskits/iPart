---
status: roboczy
zakres: obieg dokumentacji Pakietu Elektronicznego — moduły comparator, iPart, fusePart, sPart, cPart, dPart, entPart, scrapPart, dataPart, qPart
przegląd: 2026-11-23
---

# Obieg plików Part — notatki z wywiadu

Źródło: wywiad z Kierownikiem działu Obsługi Projektu (autor systemu plików), 2026-11-23. Uzupełniony o przegląd struktury plików Excel z folderu `zrodla/` (nagłówki arkuszy, bez danych Klientów). Materiał niewiążący.

Oznaczenia: **[dziś]** — jak jest teraz, **[potrzeba]** — jak powinno być, **[niepewne]** — do potwierdzenia.

## Cel i odbiorcy

- **Cel:** opisać, jak działają dziś pliki Excel obsługujące Pakiety Elektroniczne (PCBA), kto z nich korzysta, jak przepływają dane i czego brakuje — jako podstawę do przeniesienia systemu do aplikacji.
- **Odbiorcy:** Kierownik działu Obsługi Projektu (właściciel systemu), osoba budująca aplikację, a w dalszej kolejności kierownicy działów, których dotyczą zmiany (zakupy, logistyka, magazyn, SMT).
- **Poza zakresem tego etapu (świadomie odłożone):** rezerwacje magazynowe i uwzględnianie innych ZD/kompletacji, integracja z TS Kompletor, Nexo i TS ProduMan, szczegóły formuł i makr (osobny etap analizy technicznej).

## Pojęcia

| Pojęcie | Znaczenie w naszej firmie |
|---|---|
| Pakiet Elektroniczny (PCBA) | Wyrób produkowany na podstawie dokumentacji Klienta; rozpoznawany po rewizji Klienta (np. „PCBA1 rev C”). |
| BOM | Lista komponentów od Klienta; zwykle Excel, układ kolumn różny u każdego Klienta, a nawet między projektami jednego Klienta. |
| P&P | Lista położeń elementów na płytce (współrzędne X/Y, strona, obrót); zwykle `.txt`. |
| Plik BOM_P&P | Pośredni plik o stałych kolumnach, do którego przekleja się dane Klienta przed comparatorem. |
| Rdzeń BOM | Wspólny zestaw kolumn w iPart/fusePart/sPart/cPart/dPart: ID, Confirmed, Designator, Quantity, Part TS, Case, Footprint, Description, Value, Part Number, EntPart, Mounting, Notes. |
| Part TS | Wiodąca dla wszystkich działów informacja, co według dokumentacji musi być zamontowane. Zwykle Part Number dla aktywnych, parametry dla RC. Pojawia się w programach działowych i na oklejeniu opakowań wydawanych na produkcję. Ustalana automatycznie po synchronizacji z dataPart; człowiek ją weryfikuje. |
| Case | Obudowa zgodna z dokumentacją producenta, czasem w zapisie uproszczonym. Decyduje Opiekun Projektu. |
| Footprint | Obraz elementu na płytce. Zwykle zgodny z Case, ale nie zawsze (wymogi obrazowania w programie SMT). Decyduje Technolog SMT. |
| Value | Wartość komponentu w autorskim formacie firmy. |
| Confirmed | Prawda/fałsz dla linii rdzenia BOM — dane linii potwierdzone (zgodność BOM/P&P lub wyjaśnienie z Klientem). Można cofnąć („ANULOWANO POTWIERDZENIE”). |
| MPN zatwierdzone | Lista dopuszczonych numerów producenta (zamienników) dla linii. |
| MPN niedozwolone | Lista MPN zakazanych dla linii — zabezpieczenie po błędach lub zmianach dokumentacji. |
| DNP | „Nie montować”. Linii się nie usuwa — zmienia się desygnator (np. R1 → R1_DNP) i Mounting na DNP. |
| ZK | Zamówienie od Klienta na montaż (numer w Nexo). |
| ZD | Zamówienie do dostawcy na komponenty (numer w Nexo). Dla dostaw od Klienta też powstaje ZD, z ceną 0 zł. |
| PP | Partia produkcyjna — część ZK realizowana osobno. |
| LT | Lead time — czas dostawy komponentu. |
| Rewizja Importu | Numer kolejnego BOM otrzymanego dla tej samej rewizji Klienta. Używana też przy przenoszeniu iPart ze starszych wersji pliku. |
| Powierzenie (EntPart) | Komponent dostarczany przez Klienta. Może dotyczyć całego BOM, pojedynczych pozycji lub części ilości. |
| Zamiennik jednorazowy | Status komponentu w sPart — zamiennik tylko dla danego zamówienia, bez zmiany iPart. |
| Desygnacja | Przypisanie opakowania do konkretnej pozycji BOM (dPart). |
| Numer pojemnika | Numer pojemnika, w którym komponent jest wydany na produkcję (komponenty mogą być w kilku pojemnikach). |
| Sposób wydania | Cel wydania: SMT, THT, serwis (montaż mechaniczny), bezpośrednio do Klienta itp. Kolumna obecnie nieużywana. |
| Kompresorownia | Dawne pomieszczenie z komponentami spoza gospodarki magazynowej. Zaszłość, na razie zostaje. |
| Nadwyżka | [niepewne] Liczba komponentów zamówiona ponad ilość z BOM (sekcja kierownika logistyki w sPart). |
| Nadmiar ze zmiany | Komponenty już zamówione/dostarczone, które przestały być potrzebne po zmianie BOM. Pojęcie inne niż nadwyżka. |
| PADS | Liczniki wyprowadzeń wg grup obudów (RC, TT, DL, QL, BGA, TP, NS, THT…). W dataPart zasilają estymację czasu (qPart). |
| supplyPart / entPart (arkusze) | Arkusze skoroszytu scrapPart z regułami zapasu wg Case — dla zakupów i dla komponentów powierzanych. Nazwa „entPart” oznacza też osobny plik. |

**Role** (bez nazwisk):

| Rola | Zakres |
|---|---|
| Opiekun Klienta | Jedyny kanał kontaktu z Klientem: dokumentacja, zmiany, odpowiedzi na pytania. Wycenia pracę przy ofercie. |
| Operator danych BOM | Jeden z dwóch wyznaczonych pracowników. Przekleja dane do BOM_P&P, obsługuje comparator („Zaimportował” w iPart). |
| Opiekun Projektu | Opracowuje iPart, synchronizuje z dataPart, dopisuje odpowiedzi Klienta i zamienniki. Osoba kontaktowa dla innych działów. Najczęściej ta sama osoba co operator danych BOM. Nazwa to zaszłość — [niepewne] prawdopodobnie zostanie zmieniona. |
| Kierownik działu Obsługi Projektu | Ustala reguły, rozwiązuje nietypowe problemy, łączy BOM-y w fusePart, współustala reguły zapasów. |
| Technolog SMT | Zatwierdza P&P (zgodność z programem SMT), decyduje o Footprint, zatwierdza Footprint w dataPart. |
| Zakupowiec | Wycena komponentów, zamówienia (sPart), zamienniki, generowanie cPart. |
| Kierownik Logistyki | Część działu zakupów. Podsumowanie kosztów i nadwyżek projektu, współustala reguły zapasów. |
| Magazyn | Kompletacja (cPart), generowanie dPart, informowanie produkcji o nowym dPart. |
| Przyjmujący na produkcji | Przyjmują opakowania wg dPart i rozdzielają na stanowiska (splice, przekładanie z tacek, zakładanie na podajniki). |

## Moduły

| Moduł | Zadanie | Wejście: skąd | Co robi sam | Wyjście: dokąd | Kto obsługuje |
|---|---|---|---|---|---|
| comparator (Power BI) | Porównanie BOM z P&P | Plik BOM_P&P (ręcznie przeklejone dane Klienta) | Rozwija zakresy i listy desygnatorów, porównuje dane dla każdego desygnatora | Rdzeń BOM z Confirmed do iPart; lista pytań do Wykazu Różnic Dokumentacyjnych | Operator danych BOM |
| iPart | Integracja BOM i P&P, główne źródło danych pakietu | comparator, dataPart, odpowiedzi Klienta | Synchronizacja z dataPart, Part TS, MPN zatwierdzone/niedozwolone z logiem, historia zmian, zatwierdzenie P&P | sPart, fusePart, cPart (przy otwarciu), entPart; dopisuje komponenty do dataPart | Opiekun Projektu; Technolog SMT (P&P) |
| fusePart | Łączenie kilku BOM do wspólnych zakupów i kompletacji | Kilka iPart | Łączy linie po kluczu Part TS + Case, pozostałe rozbieżności w kolumnie „Różnice” | sPart | Kierownik lub Opiekun Projektu |
| sPart | Zakupy | iPart lub fusePart, stan magazynu (xlsx/csv), wycena, supplyPart | Ilości z BOM, zapas, brakująca ilość, zamówienia, koszty | cPart (przy zamknięciu sPart); zamienniki do iPart | Zakupowiec, Kierownik Logistyki |
| cPart | Kompletacja | iPart (przy otwarciu), sPart (przy zamknięciu sPart) | Wydane opakowania, liczba płytek możliwych do zmontowania, podział na PP | dPart (eksport) | Magazyn |
| dPart | Desygnacja opakowań przy wydaniu na produkcję | cPart + pliki z TS Kompletor | Przypisanie opakowań i pojemników do pozycji BOM | Produkcja | Magazyn generuje, produkcja korzysta |
| entPart (plik) | Komponenty powierzane przez Klienta | iPart (kolumna EntPart) | — | Klient odsyła MPN i ilości | Opiekun Projektu lub Opiekun Klienta |
| scrapPart | Reguły zapasów wg obudowy | — | Arkusze supplyPart (zakupy) i entPart (powierzane): te same zasady, inne wartości | sPart, plik entPart | Kierownik Logistyki, Kierownik działu Obsługi Projektu |
| dataPart | Baza komponentów z parametrami (ok. 40 tys.) | Dopisywanie z iPart | Value, Case, Footprint, montaż, PADS, parametry RC/kondensatorów, zatwierdzenia | iPart, qPart | Opiekunowie Projektu, Technolodzy |
| qPart | Estymator czasu | — | Niezrealizowany (najnowsza wersja: TS_KOSZTORYSANT) | — | Nieustalone |

### comparator

- [dziś] Dane Klienta są ręcznie przeklejane do pliku BOM_P&P o stałych kolumnach (BOM: Designator, Part TS, Footprint, Description, Quantity, Value, Part Number, Notes + Column1…15; P&P: Designator, Part TS, Footprint, Description, X, Y, Layer, Rotation).
- [dziś] Braki w P&P (poza współrzędnymi) uzupełnia się z BOM. Braki w BOM — pytanie do Klienta albo łączenie kolumn (aktywne: Part Number, pasywne: wartość).
- [dziś] Rozbieżność zaakceptowana przez operatora → obowiązuje wartość z BOM, linia Confirmed. Niezaakceptowana → linia trafia do iPart jako niezweryfikowana, poprawną wartość wpisuje się w iPart.
- [dziś] SMT w BOM, brak w P&P → brak zatwierdzenia, pytanie do Klienta; po potwierdzeniu montażu czasem sami dopisujemy współrzędne; w iPart linia na końcu P&P bez współrzędnych.
- [dziś] THT/mechaniczny w BOM, brak w P&P → montujemy bez pytania Klienta.
- [dziś] W P&P, brak w BOM → zwykle DNP, czasem pytanie do Klienta (decyzja indywidualna).
- [dziś] Pytania trafiają do „Wykazu Różnic Dokumentacyjnych” (pary BOM/P&P dla Designator, Part TS, Footprint, Description + PYTANIE/ODPOWIEDŹ), wysyłanego przez Opiekuna Klienta.
- [dziś] Comparator umie porównać dwa BOM-y (lista zmian). Miał opcję „cały BOM powierzony”.
- [potrzeba] Przenieść opcję „cały BOM powierzony” do iPart.
- [potrzeba] Import BOM z ręcznym mapowaniem kolumn zamiast przeklejania: użytkownik wskazuje, która kolumna trafia do którego pola; dwie kolumny mogą trafić do jednego pola z określeniem, która ma pierwszeństwo; kolumny nieoznaczone przechodzą po kolei do Column1…15. Bez automatycznego zgadywania na podstawie zachowań Klienta (zbyt duże ryzyko).

### iPart

- [dziś] Synchronizacja z dataPart po kolumnie Part Number, przyciskami i makrami: (1) Rozkoduj Part Number — Value z dataPart, rozdzielenie kilku MPN w jednej komórce; (2) Przypisz Parametry — formularz dla MPN spoza bazy; (3) Standaryzuj Obudowy — Case i Footprint z dataPart lub dopisanie komponentu przez formularz, porównanie sposobu montażu.
- [dziś] iPart zasila dataPart: „dopisz z zatwierdzeniem” (parametry potwierdzone) lub „dopisz bez zatwierdzenia” (dane niepełne).
- [dziś] Part TS, logi i historia zmian są zautomatyzowane.
- [dziś] MPN zatwierdzone pochodzą z: kilku MPN w Part Number, dopasowania parametrów Value/Case do dataPart, informacji od Klienta, zamienników wskazanych przez zakupowca w sPart. Arkusze `_log` zapisują kto, kiedy i na jakiej podstawie.
- [dziś] MPN niedozwolone dopisuje się ręcznie po błędach lub zmianach dokumentacji.
- [dziś] Confirmed ustawiane w comparatorze albo w iPart po wyjaśnieniu z Klientem; potwierdzenie można cofnąć. Pilnuje Opiekun Projektu.
- [dziś] Technolog SMT zatwierdza P&P; bez tego kompletacji w cPart nie można zakończyć i wyeksportować do dPart. Samo sprawdzenie zgodności z programem SMT odbywa się poza systemem.
- [dziś] Reguła: nie zmienia się kolejności linii pierwotnego BOM; nowe linie na końcu; linie usuwane oznacza się jako DNP.
- [dziś] Limit 15 MPN na linię nie ma uzasadnienia. [potrzeba] Dowolna liczba MPN.

### fusePart

- [dziś] Klucz łączenia: Part TS + Case — żeby nie połączyć różnych komponentów (np. 100k/1% w różnych obudowach), a jednocześnie połączyć linie różniące się zapisem lub kompletnością danych historycznych.
- [dziś] Różnice (np. różne MPN zatwierdzone) w kolumnie „Różnice”; przegląda je osoba łącząca BOM-y.
- [dziś] Zakupy pracują na fusePart; decyzje przechodzą do sPart → cPart → dPart.
- [dziś] Brak synchronizacji wstecznej do iPart — tylko ręczne przenoszenie. [potrzeba] Automatyczna synchronizacja wsteczna.
- [dziś] Suma MPN niedozwolonych z łączonych pakietów nie obowiązuje. [potrzeba] Powinna obowiązywać.
- [dziś] Istnieje osobny wariant dPart dla połączonych BOM-ów (dPart_v3.f).

### sPart

- [dziś] Importuje iPart (lub fusePart); przy każdym otwarciu widzi aktualny BOM.
- [dziś] Stan magazynu kopiowany/importowany z xlsx lub csv; ilość przypisywana po PN.
- [dziś] Ilość z BOM = ilość pakietów × Quantity. Zapas = reguły wg Case z arkusza supplyPart (automatycznie). Brakująca ilość = ilość z BOM + zapas − ilość na magazynie.
- [dziś] Nie uwzględnia rezerwacji, innych kompletacji ani ZD w drodze. [potrzeba] Docelowo tak — poza obecnym etapem.
- [dziś] Zakupowiec pomija zakup linii niepotwierdzonych (Confirmed).
- [dziś] Wycena z etapu oferty jest przenoszona jeden do jednego dzięki stałej kolejności linii.
- [dziś] Powierzenie obsługiwane tu ręcznie: dostawca „Klient”, przy częściowym powierzeniu dwa wiersze dla pozycji, ZD z ceną 0 zł.
- [dziś] Tryby: podglądu (bez aktualizacji rdzenia i działań w tle), zmian, [niepewne] podsumowania dla Kierownika Logistyki (tylko sekcja podsumowania). Tryb podsumowania istnieje — działanie do potwierdzenia w kodzie.
- [dziś] cPart generuje zakupowiec, bez warunków — nawet z „pustego” sPart, do pracy równoległej.

### cPart

- [dziś] Dane z iPart pobierane przy każdym otwarciu cPart; dane z sPart wysyłane przy każdym zamknięciu sPart.
- [dziś] Magazyn wpisuje wydane opakowania (numer, ilość, lokalizacja); plik liczy, ile płytek można zmontować.
- [dziś] Wpisy magazynu zostają mimo zmian w iPart. Linia nie znika (reguła DNP); zmiana jest widoczna przez podświetlenie.
- [dziś] Możliwy podział kompletacji na partie produkcyjne.
- [dziś] Kolumna „Sposób wydania” nieużywana. [potrzeba] Warto ją przynajmniej częściowo zautomatyzować.
- [potrzeba] „Wycofanie z kompletacji” opakowania ze śladem w historii kompletacji.

### dPart

- [dziś] Eksport z cPart w momencie fizycznego wydania na produkcję; dołączane pliki z TS Kompletor.
- [dziś] Gdy SMT i THT wydawane osobno — zwykle drugi dPart. Po dowydaniu lub zmianie BOM magazyn generuje nowy dPart i informuje przyjmujących.
- [dziś] Sekcja „Designators” (wariant łączony): w nagłówkach lista projektów z fusePart, dla każdej linii desygnatory z danego projektu.

### entPart (plik)

- [dziś] Generowany z iPart na podstawie kolumny EntPart przez Opiekuna Projektu lub Opiekuna Klienta, wysyłany do Klienta, który odsyła MPN i ilości.
- [dziś] Klienci informują o powierzeniu różnie: w BOM albo mailem. Bywa powierzenie całego BOM, części pozycji, części ilości; dla tego samego pakietu różnie w różnych ZK.
- [potrzeba] Import odesłanego entPart do sPart i cPart.

### dataPart

- [dziś] Arkusz partsData: Part Number, Mounting, Case, Footprint, Value, Notes, Critical, altCase, PADS, flagi i Tagi. Arkusze pomocnicze obudów (railmilCase, thtCase, altCase, stdCase, qtyCase) i parametrów (resValue, capValue, ctceValue).
- [dziś] Kolumny flagCase/flagFootprint/flagValue — kto i kiedy zatwierdził parametr. Tag1–Tag15 — historia zmian.
- [dziś] Technolog zatwierdza Footprint, Opiekun Projektu — Case i Value.
- [dziś] Zmiana zatwierdzonego parametru nie kasuje zatwierdzenia automatycznie — robi się to ręcznie. [potrzeba] Zmiana powinna kasować zatwierdzenie.
- [potrzeba] Pełna historia zmian (kto, kiedy, co) zamiast Tagów.

### qPart

- [dziś] PADS w dataPart istnieją po to, żeby zasilać estymację czasu (potwierdzone). Najnowsza wersja: TS_KOSZTORYSANT (oferta, kosztorys, formularz PCBA, weryfikacja wymagań, estymacja czasowa, słowniki obudów wg grup PADS).
- [potrzeba] Szacowanie czasu przygotowania technicznego i technologicznego dokumentacji, kompletacji, przygotowania produkcji, produkcji, kontroli, operacji dodatkowych, pakowania — na podstawie BOM, P&P, PADS i parametrów maszyn.
- [potrzeba] Potrzebny przy ofertowaniu (Opiekun Klienta), wstępnej ścieżce procesowej, planowaniu produkcji i podsumowaniu opłacalności (wąskie gardła, nieoptymalne prace).
- [dziś] Projekt upadł, bo nie wiadomo, kto miałby go wypełniać.

## Przepływ

- [dziś] Pakiet pojedynczy: **comparator → iPart → sPart → cPart → dPart**.
- [dziś] Pakiety łączone: **comparator → iPart → fusePart → sPart → cPart → dPart**.
- [dziś] Odgałęzienia: iPart → entPart → Klient; scrapPart (supplyPart) → sPart; iPart ↔ dataPart (odczyt i dopisywanie).
- [dziś] Przepływy wsteczne: zamienniki z sPart → iPart (MPN zatwierdzone); decyzje z fusePart → iPart (tylko ręcznie).
- [dziś] Kierunek sPart → cPart (sPart wysyła, cPart nie pobiera) jest zabezpieczeniem: cPart nie ma dostępu do dysku z cenami.

## Procesy i powroty

| Krok | Kto | Skąd wie, że ma działać | Warunek | Wynik | Kiedy wraca się do poprawek |
|---|---|---|---|---|---|
| Zapytanie ofertowe — wycena | Zakupowiec (komponenty), Opiekun Klienta (praca) | Zapytanie od Klienta | — | Wycena na surowym BOM Klienta | — |
| Przygotowanie BOM_P&P i comparator | Operator danych BOM | ZK w TS ProduMan (zespół śledzi listę na bieżąco) | Dokumentacja Klienta | Rdzeń BOM, pytania do Klienta | Nowy BOM od Klienta (S1, S3) |
| Opracowanie iPart | Opiekun Projektu | j.w. | — | iPart gotowy zwykle w ciągu 2 dni roboczych; mogą zostać linie niepotwierdzone | Odpowiedzi Klienta, lista zmian (S2), cofnięcie potwierdzenia |
| Zatwierdzenie P&P | Technolog SMT | [niepewne] nie ustalono | — | Odblokowanie zakończenia kompletacji | [niepewne] |
| Zakupy | Zakupowiec | Sam sprawdza, czy jest iPart; jeśli nie — prosi o przygotowanie | iPart (może być częściowo niepotwierdzony) | ZD; linie niepotwierdzone pomijane | Zmiana BOM (podświetlenie w sPart) |
| Generowanie cPart | Zakupowiec | Prośba magazynu | Brak warunków | cPart (także z „pustego” sPart) | — |
| Kompletacja | Magazyn | Sam sprawdza, czy jest cPart; jeśli nie — prosi o przygotowanie | — | Wydane opakowania, liczba płytek | Zmiana BOM (podświetlenie w cPart) |
| Wydanie na produkcję (dPart) | Magazyn | Fizyczne wydanie | Zatwierdzenie P&P przez Technologa | dPart dla przyjmujących | Dowydanie, zmiana BOM, osobne wydanie SMT/THT → nowy dPart |
| Podsumowanie projektu | Kierownik Logistyki | Własna decyzja, zwykle pod koniec lub po projekcie | — | Nadwyżki, koszty | — |

Typowe powroty:

- **S1 — nowy BOM po rozpoczęciu zakupów** [dziś]: dowiaduje się Opiekun Klienta, informuje przez powiadomienia w ProduMan. Przeklejenie i comparator od nowa; rozbieżności rozstrzygane indywidualnie (często prośba do Klienta o potwierdzenie wcześniejszych ustaleń); pozostałe dane przenoszone ze starego iPart. Porównanie dwóch BOM-ów nie wystarcza: za dużo rozbieżności, linie się przetasowują (zmieniają się zakresy desygnatorów, komponenty przechodzą do innych linii lub tworzą nowe). Ręczna zmiana zakresów w iPart jest karkołomna. Główny problem: inna forma zapisu danych niż w poprzedniej wersji.
- **S2 — lista zmian od Klienta** [dziś]: najprostszy przypadek, zmiany nanoszone 1:1 w obecnym iPart.
- **S3 — BOM wstępny, finalny zapowiedziany** [dziś]: Klient prosi o rozpoczęcie zakupów; oba BOM-y procesowane w całości przez wszystkie etapy. [potrzeba] Uproszczona procedura dla BOM wstępnego.

## Cykl życia Pakietu

| Zdarzenie | Kto wywołuje | Co musi być spełnione | Skutek |
|---|---|---|---|
| Zapytanie ofertowe | Klient | — | Wycena na surowym BOM (bez comparatora i iPart) |
| ZK | Klient; wprowadzenie do TS ProduMan | — | Start comparatora i iPart |
| Nowy BOM dla tej samej rewizji Klienta | Klient (przez Opiekuna Klienta) | — | Nowa „Rewizja Importu”, nowy iPart pod tą samą nazwą, stary do archiwum |
| Kolejne ZK na ten sam pakiet | Klient | — | Ten sam iPart (sytuacja wymarzona: bez poprawek) |
| Zmiana tylko dla jednego ZK | Klient / ustalenie | — | Kopia iPart z numerem ZK w nazwie pliku |
| Zamiennik tymczasowy | Zakupowiec | — | Status „zamiennik jednorazowy” w sPart |
| Podział ZK na partie | Planowanie / magazyn | — | Podział kompletacji w cPart; zakupy dalej na całe ZK |
| Wydanie na produkcję | Magazyn | Zatwierdzenie P&P | dPart (może być kilka na ZK) |

- [dziś] sPart i cPart przy otwarciu zawsze widzą aktualny iPart. To raczej zamierzone (reguła stałej kolejności linii), ale może zaburzyć przypisanie kupionego lub skompletowanego komponentu do nowej, innej pozycji.
- [dziś] Kopia iPart dla ZK odrywa się od głównego — zmiany stałe trzeba nanosić ręcznie w obu plikach.
- [dziś] Zmiany w trakcie partii — ręcznie; projekty dzielone na partie są zwykle stabilne.
- [potrzeba] Tryb uproszczony iPart dla oferty i BOM wstępnego (np. bez dopisywania do bazy komponentów spoza niej).
- [potrzeba] Wielokrotne użycie tego samego iPart dla kolejnych ZK bez poprawek.

## Historia zmian i komunikacja

| Rodzaj zmiany | Gdzie zapisana | Kogo informujemy i jak | Dziś / potrzeba |
|---|---|---|---|
| Zmiana danych w iPart | Arkusz Historia Zmian (ID, pozycja, kolumna, poprzednia/bieżąca wartość, kto, kiedy, kopia BOM); kolumna Historia Zmian z numerami grupowanymi wg zapisów `[#, #, #] [#, #]` | sPart/cPart: podświetlenie gradientem wg daty (nowsze — ciemniejsze), [niepewne] pogrubienie komórki z różnicą | [dziś] widać tylko ostatnią zmianę, brak potwierdzenia obsłużenia. [potrzeba] Pokazać, która linia i która dana się zmieniła względem poprzedniej wersji; oznaczanie zmiany jako obsłużonej w sPart/cPart. |
| Nowa dokumentacja / zmiany od Klienta | — | Opiekun Klienta przez powiadomienia w TS ProduMan | [dziś] |
| Cofnięcie potwierdzenia linii | Historia Zmian: „ANULOWANO POTWIERDZENIE” | [niepewne] | [dziś] |
| Dopisanie MPN zatwierdzonego / niedozwolonego | Arkusze `_log` (kto, kiedy, na jakiej podstawie) | — | [dziś] |
| Zmiana komponentu w dataPart | Tag1–Tag15, flagi zatwierdzeń | — | [potrzeba] pełna historia: kto, kiedy, co |
| Nowy dPart | — | Magazyn informuje przyjmujących ręcznie | [potrzeba] powiadomienia w aplikacji |
| Gotowość etapu (iPart, cPart) | — | Brak — następny etap sam sprawdza i prosi | [potrzeba] [niepewne] powiadomienia w aplikacji |
| Wycofanie opakowania z kompletacji | — | — | [potrzeba] ślad w historii kompletacji |

## Nadmiary i zapasy

- [dziś] Zapas liczony automatycznie wg Case z reguł scrapPart (supplyPart dla zakupów, entPart dla powierzanych). Reguły ustalają Kierownik Logistyki i Kierownik działu Obsługi Projektu.
- [dziś] Nadmiar ze zmiany BOM zwykle zostaje na magazynie; jeśli zdążymy i opakowanie jest nienaruszone — próba zwrotu do dostawcy; obciążenie Klienta zależy od inicjatywy Opiekuna Klienta.
- [dziś] **Nadmiaru nie zapisuje się nigdzie.** [potrzeba] Ewidencja w aplikacji (co, ile, z jakiej zmiany, decyzja).
- [niepewne] „Nadwyżka” w sPart to prawdopodobnie ilość zamówiona ponad ilość z BOM.
- [dziś] Przed obecnym systemem zakupowiec zamawiał elementy powierzane przez Klienta — firma ponosiła stratę.

## Uprawnienia i widoczność

| Moduł / dana | Kto czyta | Kto zmienia | Kto zatwierdza |
|---|---|---|---|
| Rdzeń BOM (desygnatory, ilości, Description) | Wszyscy | Operator danych BOM, Opiekun Projektu | — |
| Confirmed | Wszyscy | Operator danych BOM (comparator), Opiekun Projektu (iPart) | — |
| Part TS, Case, Value | Wszyscy | Opiekun Projektu | Opiekun Projektu |
| Footprint, P&P | Wszyscy | Technolog SMT | Technolog SMT |
| MPN zatwierdzone | Wszyscy | Opiekun Projektu, zakupowiec (zamiennik) | Bez akceptacji (na obecnym etapie) |
| MPN niedozwolone | Wszyscy | Opiekun Projektu | Bez akceptacji (na obecnym etapie) |
| Powierzenie (EntPart) | Wszyscy | Opiekun Projektu, Opiekun Klienta | Bez akceptacji (na obecnym etapie) |
| Wycena, zamówienia, ZD, ceny, koszty | **Tylko dział zakupów (w tym Kierownik Logistyki) i Kierownik działu Obsługi Projektu** | Zakupowiec, Kierownik Logistyki | — |
| Wydania, opakowania, pojemniki | Magazyn, produkcja | Magazyn | — |
| Reguły zapasów (scrapPart) | [niepewne] | Kierownik Logistyki, Kierownik działu Obsługi Projektu | — |
| Parametry w dataPart | Wszyscy | Opiekunowie Projektu, Technolodzy | Technolog (Footprint), Opiekun Projektu (Case, Value) |

- [dziś] Arkusze zablokowane hasłem, które znają wszyscy; umowa: odblokowanie tylko pod nieobecność Kierownika działu Obsługi Projektu i zgłoszenie mu tego.
- [dziś] Tryb podglądu i tryb zmian dostępne dla wszystkich.
- [dziś] Ochrona cen opiera się na dostępie do dysku z plikami sPart.
- [potrzeba] Ceny i koszty nie mogą wychodzić poza uprawnione osoby.

## Braki i usprawnienia

| Etap | Problem dziś | Potrzeba | Ostrzeżenie czy blokada |
|---|---|---|---|
| Import BOM | Ręczne przeklejanie do BOM_P&P | Mapowanie kolumn przez użytkownika, łączenie kolumn z priorytetem, nieoznaczone do Column1…15 | — |
| Oferta | BOM nie przechodzi przez comparator/iPart (zbyt żmudne) | Tryb uproszczony iPart | — |
| BOM Klienta | Brak wykrywania niezgodności Value z MPN | Mechanizm wykrywania | Do ustalenia |
| Nowa wersja BOM (S1, S3) | Praca od początku, ręczne nanoszenie zakresów | Nowa wersja dziedziczy pracę z poprzedniej | Do ustalenia |
| fusePart | Brak synchronizacji wstecznej; MPN niedozwolone nie sumują się | Synchronizacja wsteczna; suma zakazów | Do ustalenia |
| Powierzenie | Ręcznie w sPart; nie da się ustawić na stałe w iPart | „Cały BOM powierzony” w iPart; import odesłanego entPart do sPart/cPart | Do ustalenia |
| Zmiany w sPart/cPart | Widać tylko ostatnią zmianę, brak potwierdzenia obsłużenia | Zmiana per linia i per dana; oznaczanie jako obsłużona | Do ustalenia |
| Gotowość etapów, nowy dPart | Brak powiadomień | Powiadomienia w aplikacji | Ostrzeżenie [niepewne] |
| Kompletacja | Brak śladu wycofania opakowania; „sposób wydania” nieużywany | Wycofanie z kompletacji z historią; częściowa automatyzacja sposobu wydania | — |
| dataPart | Historia w Tagach; zatwierdzenie nie kasuje się przy zmianie | Pełna historia; automatyczne kasowanie zatwierdzenia | — |
| Nadmiary | Nieewidencjonowane | Ewidencja i decyzja | — |
| Zakończenie kompletacji | — | — | [dziś] **Blokada:** bez zatwierdzenia P&P przez Technologa |
| Zakupy | — | — | [dziś] Linie niepotwierdzone pomijane przez zakupowca (umowa, nie blokada techniczna — [niepewne]) |
| MPN na linię | Limit 15 kolumn | Dowolna liczba | — |
| Odłożone świadomie | Rezerwacje, inne ZD i kompletacje, TS Kompletor, Nexo | Docelowo | — |

## Przykłady z praktyki

- **Powierzone komponenty zamówione przez zakupy** (przed obecnym systemem): zakupowiec zamówił elementy, które dostarczał Klient — firma poniosła stratę. Dziś powierzenie zaznacza się ręcznie w sPart.
- **Zmiana makr w pliku użytkownika:** osoba z zespołu znająca Excel i VBA zmieniła kod sPart w pliku użytkownika zamiast w szablonie głównym. Wystąpiły błędy przy zapisie i eksporcie cPart; Kierownik działu Obsługi Projektu ręcznie poprawiał kod makr w kilkunastu plikach sPart.
- Przykłady kłopotów przy przeklejaniu BOM — rozmówca nie zajmuje się już tym na co dzień; znane problemy (zakresy desygnatorów, kilka MPN w komórce) są rozwiązane w comparatorze i iPart. Przesunięte nagłówki i scalone komórki nie są znanym problemem.

## Dane wrażliwe i granice

- Ceny, koszty i różnice względem wyceny — tylko dział zakupów (z Kierownikiem Logistyki) i Kierownik działu Obsługi Projektu.
- W notatkach nie zapisano imion i nazwisk pracowników, nazw Klientów ani projektów. Pliki źródłowe zawierają takie dane (m.in. nagłówek kolumny z imieniem pracownika w sPart, lista osób w arkuszu weryfikacji wymagań w TS_KOSZTORYSANT) — pliki są w folderze `zrodla/`, wyłączonym z repozytorium.
- Wywiad dotyczył zasad, nie konkretnych BOM-ów.

## Sprzeczności i otwarte pytania

Wyjaśnione w trakcie wywiadu:

- Kontakt z Klientem prowadzi Opiekun Klienta, nie operator danych BOM.
- Opiekun Projektu ≠ Opiekun Klienta.
- S1 i S3 — przy uproszczeniu procedury chodziło o S3 (zapowiedziany finalny BOM).
- Kierownik Logistyki ma pełny dostęp do sPart — traktowany jako część działu zakupów.
- Powierzenie jest zarówno stałą cechą współpracy z Klientem, jak i cechą konkretnego ZK — potrzebne oba poziomy.
- Normalizacja przez dataPart nie rozwiąże porównywania wersji BOM, bo Value w BOM bywa niezgodne z MPN.

Otwarte:

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Jaka będzie nowa nazwa roli „Opiekun Projektu”? | Kierownik działu Obsługi Projektu |
| Które pola porównuje comparator i jak dokładnie? | Kod comparatora (analiza techniczna) |
| Kto wypełniałby qPart? | Kierownictwo (do ustalenia) |
| Co dokładnie oznacza „Nadwyżka” w sekcji kierownika logistyki? | Kierownik Logistyki |
| Jak dokładnie wyróżniane są zmiany w sPart i cPart (kolumny, pogrubienie)? | Kod VBA (analiza techniczna) |
| Jak działa tryb podsumowania w sPart? | Kod VBA (analiza techniczna) |
| Skąd Technolog SMT wie, że ma zatwierdzić P&P? | Technolog SMT |
| Czy pomijanie linii niepotwierdzonych w zakupach jest blokadą techniczną, czy umową? | Zakupowiec, kod VBA |
| Kto widzi reguły zapasów w scrapPart? | Kierownik Logistyki |

## Wnioski dla aplikacji

*Propozycje prowadzącego wywiad, nie ustalenia rozmówcy — do dyskusji.*

1. ⭐ **Jedno wspólne źródło danych pakietu** (rozmówca wskazał jako najważniejsze ustalenie). Decyzje zapadają na różnych etapach — zamienniki w sPart, decyzje w fusePart, potwierdzenia w comparatorze i iPart — i powinny trafiać do jednego miejsca widocznego dla wszystkich etapów, zamiast żyć w osobnych plikach i być przenoszone ręcznie.
2. **Jeden program zamiast makr w każdym pliku.** Dziś każdy plik ma własną kopię makr — poprawka lub błąd trzeba przenosić plik po pliku. W aplikacji zmiana programu działa od razu dla wszystkich projektów.
3. **Stały identyfikator linii BOM.** Dziś pozycja w pliku pełni rolę identyfikatora (stąd reguła niezmiennej kolejności i DNP zamiast usuwania). Niezmienny numer linii zachowa tę regułę, ale przestanie ona być kruchą umową.
4. **Porównywanie wersji BOM po desygnatorze**, nie po linii — przetasowania linii przestaną przeszkadzać. Problem innej formy zapisu i niezgodności Value/MPN pozostaje do rozwiązania osobno.
5. **Wersje i poziomy:** pakiet (rewizja Klienta) → wersja BOM (Rewizja Importu) → ZK → partia. Zamówienie w toku powinno pamiętać, na której wersji BOM kupiono i skompletowano komponenty; zmiana wersji pokazuje różnice do decyzji zamiast podmieniać dane po cichu. Wyjątki dla ZK jako „nakładka” na wspólny iPart, nie kopia pliku. Nowa wersja BOM dziedziczy decyzje z poprzedniej.
6. **Zaopatrzenie pozycji z kilku źródeł:** zakup, Klient (powierzenie), magazyn — każde z własną ilością; powierzenie domyślne (Klient/pakiet) z wyjątkami dla ZK.
7. **Role i uprawnienia zamiast podziału na dyski** — ceny i koszty widoczne tylko dla zakupów i kierownictwa; to najważniejsze wymaganie bezpieczeństwa.
8. **Zmiany jako zadania:** każda zmiana linii widoczna per dana, z oznaczeniem „obsłużona” przez zakupy i magazyn; powiadomienia o gotowości etapów i nowym dPart.
9. **qPart z danych, które już powstają** — BOM, P&P i PADS w jednym źródle mogą zmniejszyć ręczne wypełnianie, co częściowo odpowiada na problem „kto będzie wypełniał”.
10. **Rozdzielenie nazw:** „entPart” (plik vs arkusz w scrapPart), „Rewizja Importu” (wersja danych vs wersja szablonu pliku), „nadwyżka” vs „nadmiar ze zmiany”.
11. Pomysł „aplikacja wczytuje Excel i zapisuje go z powrotem” pozostaje punktem wyjścia; wnioski 1–2 i 7 wskazują, że docelowo dane powinny być w bazie z uprawnieniami, a Excel służyć do importu i eksportu.

**Dalsze kroki:**

- Analiza techniczna: eksport modułów VBA do `zrodla/vba/<plik>/` oraz kod comparatora (`.txt`); opis każdego mechanizmu (wejście, reguła, wynik) do potwierdzenia przez rozmówcę.
- Odpowiedzi na otwarte pytania od wskazanych osób.

## Ocena facylitatora

**Tak — warsztat Event Storming jest wskazany**, ale zawężony do jednego tematu: **zmiana dokumentacji po rozpoczęciu realizacji** (S1, S3, zmiany tymczasowe dla ZK).

Uzasadnienie na podstawie ustaleń:

- W tym procesie uczestniczy wiele ról z różnych działów: Opiekun Klienta, operator danych BOM, Opiekun Projektu, Technolog SMT, zakupowiec, Kierownik Logistyki, magazyn, produkcja.
- Są zatwierdzenia i ich cofanie: Confirmed (z „ANULOWANO POTWIERDZENIE”), zatwierdzenie P&P blokujące zakończenie kompletacji, zatwierdzenia parametrów w dataPart.
- Są częste powroty: nowy BOM oznacza pracę od początku, a zmiany przechodzą przez wszystkie pliki z ryzykiem błędnego przypisania kupionych i wydanych komponentów.
- Informacja o zmianach i gotowości płynie poza systemem (ProduMan, ustnie), bez potwierdzenia obsłużenia.

Główny przepływ (comparator → … → dPart) jest dobrze opisany przez rozmówcę i nie wymaga warsztatu. Warsztat powinien pokazać, kto i w jakiej kolejności reaguje na zmianę, co dzieje się z już kupionymi i wydanymi komponentami (nadmiary) oraz gdzie powinny powstać powiadomienia. Proponowani uczestnicy: przedstawiciele Opiekunów Klienta, Opiekunów Projektu, zakupów, magazynu i Technolog SMT.
