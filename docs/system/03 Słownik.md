---
status: propozycja
zakres: słownik systemu iPart
przegląd: 2026-11-26
---

# Słownik

Jedno źródło znaczeń pojęć iPart. Obowiązuje w całej dokumentacji i w rozmowach o systemie.

**Pojęcia wspólne z ProduMan nie są tu definiowane ponownie.** Ich znaczenie jest w Słowniku ProduMan (`system/03 Słownik.md` w dokumentacji ProduMan). Gdy to samo słowo znaczy w iPart co innego, zmieniamy nazwę po stronie iPart ([Zasady dokumentacji](../zasady/Zasady%20dokumentacji.md), rozdział 9).

## Pojęcia wspólne z ProduMan

| Pojęcie | Gdzie w ProduMan | Uwagi dla iPart |
|---|---|---|
| **ZK** | Słownik → Zamówienia i zlecenia | iPart używa ZK do zakupów, kompletacji i wyboru wersji BOM |
| **Pakiet** (pakiet elektroniczny) | Słownik → Wyrób i technologia | w iPart pakiet rozpoznajemy po kodzie i rewizji Klienta (np. „PCBA1 rev C”); nowa rewizja Klienta to nowy pakiet, zgodnie z ADR-0006 ProduMan |
| **Opiekun klienta** | Kontekst i cel systemu → Użytkownicy | jedyny kanał kontaktu z Klientem |
| **Zakupowiec** | Kontekst i cel systemu → Użytkownicy | — |
| **Towar powierzony** | Słownik → Przepływ produkcji | w plikach Excel: „powierzenie”, „EntPart”. Może dotyczyć całego BOM, pojedynczych linii albo części ilości |
| **Kompletacja** | Słownik → Przepływ produkcji | — |
| **Kompletor** | Słownik → Przepływ produkcji | mechanizm magazynowy, nie osoba. W plikach Excel: „TS Kompletor” |
| **Zgłoszenie braku** | Słownik → Przepływ produkcji | — |
| **Estymacja** | Słownik → Wyrób i technologia | przewidywany czas; dotyczy obszaru Estymacja (dawny qPart) |
| **Nadwyżka** | Słownik → Przepływ produkcji | **dobre wyroby** ponad cel. Komponenty zamówione ponad BOM to w iPart **nadwyżka zakupowa** |
| **Obszar, Moduł, Kontrakt zewnętrzny, Komenda, Zdarzenie** | Słownik → Architektura i dokumentacja | — |

## Dokumentacja Klienta i BOM

| Pojęcie | Znaczenie |
|---|---|
| **BOM Klienta** | Lista komponentów od Klienta, zwykle w Excelu. Układ kolumn bywa różny u każdego Klienta, a nawet między projektami jednego Klienta. Dane Klienta zachowujemy bez zmian. |
| **P&P** | Lista położeń elementów na płytce: desygnator, współrzędne X/Y, strona, obrót. Zwykle plik `.txt`. |
| **Rewizja importu** | Kolejny BOM Klienta otrzymany dla tej samej rewizji Klienta. Numer nadaje iPart. Nie jest rewizją Klienta. |
| **Desygnator** | Oznaczenie miejsca elementu na płytce (np. `R1`, `C2A`). Zakresy i listy desygnatorów w BOM Klienta iPart rozbija na pojedyncze desygnatory. |
| **Linia BOM** | Pozycja BOM pakietu: komponent z listą desygnatorów i ilością. Ma stały numer w obrębie pakietu. Linie BOM Klienta zachowują jego kolejność. |
| **Podlinia** | Część linii BOM z desygnatorami o tym samym wyniku porównania z P&P (w comparatorze: „wycinek”). Rozbieżna podlinia może zostać wydzielona. |
| **Linia wydzielona** | Linia powstała z nierozwiązanej podlinii, z odnośnikiem do linii źródłowej. Później zostaje scalona z inną linią albo staje się osobnym komponentem. Numer linii scalonej nie jest używany ponownie. |
| **Rozbieżność BOM–P&P** | Niezgodność desygnatora, Footprint, opisu albo Part TS między BOM a P&P (w plikach Excel: kod L/F/D/N). Brak danych po obu stronach to ostrzeżenie, nie rozbieżność. |
| **Wykaz Różnic Dokumentacyjnych** | Lista pytań do Klienta wynikających z rozbieżności, z odpowiedziami. |
| **Potwierdzenie linii** | Stwierdzenie, że dane linii są zgodne albo wyjaśnione. Wymaga podstawy: odpowiedź Klienta, wcześniejsza realizacja albo decyzja Opiekuna Projektu z uzasadnieniem. W plikach Excel: „Confirmed”. |
| **Sekcja Oryginał** | Dane linii w brzmieniu Klienta. Nie są zmieniane. |
| **Sekcja TS** | Dane linii ustalone przez firmę: wiodące dla wszystkich działów. Niezgodność z sekcją Oryginał to rozbieżność do decyzji. |
| **Part TS** | Wiodąca informacja, co musi być zamontowane. Dla rezystorów, drabinek rezystorowych, kondensatorów, tantali i elektrolitów — zapis z parametrów; dla pozostałych typów — MPN. Pojawia się w programach działowych i na oklejeniu opakowań. Sposób wyświetlania ustawia się na kilku poziomach (ustawienia firmy, komponentu, Klienta, pakietu, linii). |
| **DNP** | „Nie montować”. Linia zostaje w BOM, zmienia się sposób montażu. Rozpoznawany tylko jako osobne słowo (DNP, DNM, DNI, NM, NI). |
| **Zatwierdzenie P&P** | Potwierdzenie przez Technologa SMT, że P&P jest zgodne z programem SMT. Ustala Footprint linii SMT. |
| **Zatwierdzenie BOM** | Udostępnienie wersji BOM zakupom i magazynowi po ustaleniu parametrów, Part TS i zamienników dla potwierdzonych linii. Nie jest cofane — zmiana wymaga nowej wersji BOM. |
| **Wersja BOM** | Zatwierdzony stan BOM pakietu. Nowa wersja powstaje dla ZK (zmiany tymczasowe) albo z nowej rewizji importu. Pakiet może mieć kilka aktywnych wersji; poprzednią można zarchiwizować. |
| **Sesja edycji** | Praca nad wersją roboczą BOM zakończona jednym zapisem zmian („Zapisz zmiany”): jeden wpis historii, jedno powiadomienie. W czasie sesji inni widzą ostatni zapis. |

## Komponenty

| Pojęcie | Znaczenie |
|---|---|
| **Komponent** | Rekord bazy komponentów: MPN z parametrami, Case, Footprint i sposobem montażu. Rozpoznawany po MPN i producencie. Klient nie wyróżnia komponentu — własność fizyczną towaru opisuje symbol magazynowy. |
| **Symbol magazynowy** | Oznaczenie komponentu w systemie magazynowym i Nexo. Komponent może mieć kilka symboli: własny firmy i powierzony przez Klienta. |
| **MPN** | Numer części producenta (Manufacturer Part Number). W plikach Excel: „Part Number”. |
| **Case** | Obudowa zgodna z dokumentacją producenta, czasem w zapisie uproszczonym. Decyduje Opiekun Projektu. |
| **Footprint** | Obraz elementu na płytce. Zwykle zgodny z Case, ale nie zawsze (wymogi programu SMT). Decyduje Technolog SMT. Raz ustalony obowiązuje wszędzie. **Footprint dedykowany** to wariant footprintu komponentu oznaczony Klientem — pokazywany, gdy BOM i P&P należą do tego Klienta. |
| **Sposób montażu** | Technologia montażu elementu: SMT, THT, Press-Fit, Pin-In-Paste, MECH, Plugs (wtyki, domyślnie wydawane na THT), PCB albo DNP. W plikach Excel: „Mounting”. Nie mówi, dokąd element jest wydawany (patrz sposób wydania). |
| **Value** | Wartość komponentu w formacie firmy, zawsze pełna (np. z PPM). |
| **Parametr nieukrywalny** | Parametr, który zawsze widać w Part TS danego typu komponentu (np. rezystancja, tolerancja i moc rezystora). |
| **MPN zatwierdzone** | Lista MPN dopuszczonych dla linii, z podstawą (z BOM Klienta, z parametrów, decyzja). Pierwszy MPN z BOM Klienta jest preferowany. |
| **MPN niedozwolone** | Lista MPN zakazanych dla linii — zabezpieczenie po błędach lub zmianach dokumentacji. Usuwane tylko ręcznie, z powodem. |
| **Zamiennik** | MPN inny niż w BOM Klienta, dopuszczony dla linii. Taki sam lub lepszy w parametrach widocznych w Part TS, z tym samym Case. |
| **Zamiennik jednorazowy** | Zamiennik dopuszczony tylko dla jednego ZK, bez zmiany BOM pakietu. |
| **PADS** | Liczniki wyprowadzeń według grup obudów (RC, TT, DL, QL, BGA, TP, NS, THT…). Podstawa estymacji. |
| **Ustawienia Klienta** | Reguły jednego Klienta, np. PPM zawsze w Part TS, MPN zawsze (także RC), język listy komponentów powierzanych. |

## Zaopatrzenie i kompletacja

| Pojęcie | Znaczenie |
|---|---|
| **ZD** | Zamówienie do dostawcy na komponenty (Nexo). Dla dostaw od Klienta też powstaje ZD, z ceną 0 zł. |
| **PP** | Partia produkcyjna — część ZK realizowana osobno. Relacja do ZP ProduMan — otwarte pytanie w [Mapie obszarów](02%20Mapa%20obszarów.md). |
| **LT** | Czas dostawy komponentu (lead time). |
| **Reguła zapasu** | Ilość zamawiana ponad BOM zależnie od Case, osobno dla zakupów i dla towaru powierzonego. W plikach Excel: scrapPart, arkusze supplyPart i entPart. |
| **Nadwyżka zakupowa** | Komponenty zamówione ponad ilość z BOM. Nie mylić z nadwyżką w ProduMan (dobre wyroby ponad cel). |
| **Zestawienie zakupowe** | Jeden albo kilka ZK jednego Klienta kupowanych i kompletowanych razem: ZK z wersjami BOM i ilościami, pozycje zakupu, partie. Zestawienie z kilkoma ZK ma numer `FUSEnnn/rrrr` i opcjonalną nazwę ([ADR-0009](../decyzje/ADR-0009%20Zestawienie%20zakupowe%20zamiast%20osobnej%20ścieżki%20łączenia.md)). |
| **Pozycja zakupu** | Wiersz zestawienia zakupowego: jedna albo kilka połączonych linii BOM kupowanych jako jeden komponent, z wybranym MPN, ilościami, dostawcą, LT, ceną i odnośnikami do ZD. |
| **Łączenie BOM-ów** | Zestawienie BOM-ów kilku ZK jednego Klienta do wspólnych zakupów i kompletacji. Tworzy je zakupowiec, Opiekun Projektu albo Kierownik działu Obsługi Projektu. Nie ma własnych decyzji o BOM: przechowuje ZK, wersje BOM, ilości i decyzje „połącz / nie łącz” pozycji. W plikach Excel: fusePart. |
| **Nadmiar ze zmiany** | Komponenty zamówione lub dostarczone, które przestały być potrzebne po zmianie BOM. |
| **Desygnacja** | Przypisanie opakowania komponentu do konkretnej linii BOM przy wydaniu na produkcję. W plikach Excel: dPart. |
| **Numer pojemnika** | Numer pojemnika, w którym komponent jest wydany na produkcję. Komponent może być w kilku pojemnikach. |
| **Sposób wydania** | Dokąd magazyn wydaje komponent: produkcja SMT, produkcja THT, serwis, bezpośrednio do Klienta. Domyślnie wynika ze sposobu montażu; wyjątki ustala Opiekun Projektu w BOM. |
| **Partia (PP)** | Część ilości ZK kompletowana i wydawana osobno, z własną kompletacją. Wszystkie partie ZK korzystają z tej samej wersji BOM dla ZK. Tworzy ją magazyn. |

## Praca w aplikacji

| Pojęcie | Znaczenie |
|---|---|
| **Do obsłużenia** | Lista pozycji przypisanych osobie: zmiana do obsłużenia, zadanie (np. zatwierdzenie P&P), komunikat do zapoznania. Pozycja ma stan: nowa, obsłużona (kto, kiedy, jak) albo odrzucona z powodem. |
| **Operator tematu** | Osoba, która zajmuje się pakietem, ZK, łączeniem albo partią — podjęła temat (utworzyła, zaimportowała, przejęła). Do niej trafiają pozycje „Do obsłużenia” dotyczące tematu. Nie ma przypisań do Klientów. |
| **Komunikat** | Wiadomość do pakietu, wersji BOM, ZK albo linii BOM, z autorem i adresatami (dział albo osoba). Wymaga potwierdzenia przeczytania. Nie mylić z komunikatem do zlecenia w ProduMan. |
| **Zastępstwo** | Wskazanie osoby, do której w okresie nieobecności trafiają pozycje „Do obsłużenia” (kopia zostaje u nieobecnego). |
| **Administrator grupy** | Osoba w grupie (dział, etap) z dodatkowym prawem wpisywania zastępstw i zwalniania blokad edycji. Wyznacza go Kierownik działu Obsługi Projektu. Cecha osoby, nie rola. |
| **Blokada edycji** | Zajęcie całego BOM pakietu na czas sesji edycji. Wygasa po godzinie bez aktywności; niezapisane zmiany zostają jako szkic. |

## Legacy i odpowiedniki

W dokumentacji iPart **legacy** oznacza system plików Excel/VBA i comparator w Power Apps. W ProduMan to samo słowo oznacza aplikacje WinForms i TaskMan.

| Legacy | Znaczenie w legacy | Odpowiednik w iPart |
|---|---|---|
| comparator | porównanie BOM z P&P (Power Apps + Power Automate) | obszar Dokumentacja pakietu, razem z iPart ([ADR-0003](../decyzje/ADR-0003%20Comparator%20i%20iPart%20jako%20jeden%20moduł.md)) |
| plik BOM_P&P | pośredni plik o stałych kolumnach, do którego przekleja się dane Klienta | import BOM i P&P z mapowaniem kolumn |
| iPart (plik) | BOM pakietu, synchronizacja z dataPart, Part TS, MPN, historia | obszar Dokumentacja pakietu |
| Rdzeń BOM | wspólne kolumny plików iPart, fusePart, sPart, cPart i dPart | linia BOM |
| fusePart | łączenie BOM-ów po kluczu Part TS + Case | łączenie BOM-ów (obszar Zaopatrzenie) |
| sPart | zakupy | obszar Zaopatrzenie |
| entPart (plik) | lista komponentów powierzanych wysyłana Klientowi | obszar Zaopatrzenie (towar powierzony) |
| scrapPart, supplyPart | reguły zapasu według Case | reguła zapasu |
| cPart | kompletacja | obszar Kompletacja |
| dPart | desygnacja opakowań | desygnacja |
| dataPart | baza komponentów (ok. 40 tys.) | obszar Baza komponentów |
| qPart, TS_KOSZTORYSANT | estymator czasu (niedokończony) | obszar Estymacja |
| Kompresorownia | dawne pomieszczenie z komponentami spoza gospodarki magazynowej | do ustalenia |
