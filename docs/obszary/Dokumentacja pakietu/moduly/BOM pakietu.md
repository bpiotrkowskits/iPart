---
status: propozycja
zakres: moduł BOM pakietu (obszar Dokumentacja pakietu)
przegląd: 2026-11-26
kod: src/iPart.Core/Packages, src/iPart.Infrastructure/Packages, src/iPart.App/ViewModels (BomImportViewModel, PackagesViewModel)
---

# BOM pakietu

## Cel

Moduł zamienia BOM i P&P Klienta na zatwierdzoną wersję BOM pakietu, z której korzystają wszystkie działy. Odpowiada na pytania: co montujemy w każdej linii, jakie MPN są dopuszczone, a jakie zakazane, i która wersja BOM obowiązuje dla danego ZK.

## Odpowiedzialność

| Posiada | Nie posiada (i kto posiada) |
|---|---|
| pakiet, rewizje importu, kopie plików BOM i P&P | parametry, Case, Footprint, sposób montażu komponentu → Baza komponentów |
| mapowanie kolumn, rozbicie desygnatorów, porównanie BOM–P&P | reguły zapasu, ilości do zamówienia → Zaopatrzenie |
| linie pakietu, podlinie, linie wydzielone, potwierdzenie linii | partie, opakowania, wydania → Kompletacja |
| sekcja Oryginał i sekcja TS, Part TS, sposób wydania linii | Klient, użytkownicy, ustawienia firmy i Klienta → Usługi wspólne |
| MPN zatwierdzone i niedozwolone linii | listy „Do obsłużenia”, komunikaty, blokady → Usługi wspólne |
| wersje BOM, zatwierdzenie P&P, zatwierdzenie BOM | |
| odnośnik do archiwum Excel pakietu | |

## Pojęcia

Ze [Słownika](../../../system/03%20Słownik.md): BOM Klienta, P&P, rewizja importu, desygnator, linia BOM, podlinia, linia wydzielona, rozbieżność BOM–P&P, Wykaz Różnic Dokumentacyjnych, potwierdzenie linii, sekcja Oryginał, sekcja TS, Part TS, DNP, zatwierdzenie P&P, zatwierdzenie BOM, wersja BOM, sesja edycji, MPN zatwierdzone, MPN niedozwolone, zamiennik, zamiennik jednorazowy, sposób montażu, sposób wydania.

Pojęcia lokalne:
- **Linia pakietu** — tożsamość linii niezależna od wersji: stały numer, stan (aktywna, scalona z inną, wydzielona, nieaktywna).
- **Linia w wersji** — dane linii w konkretnej wersji BOM.
- **Wersja BOM dla ZK** — wersja ze zmianami tymczasowymi dla wskazanych ZK (dawny „Jednorazowo Dopuszczony”).

## Model

```
Pakiet (GUID; Klient; nazwa; odnośnik do archiwum; EssegiId)
 ├─ Rewizja importu (numer; kopia plików BOM i P&P; mapowanie; wiersze Klienta = sekcja Oryginał; rozbieżności z P&P) — niezmienna
 ├─ Linia pakietu (GUID; stały numer; stan)
 └─ Wersja BOM (numer; robocza / zatwierdzona / zarchiwizowana; bazowa albo dla wskazanych ZK)
     └─ Linia w wersji (sekcja TS; Part TS; sposób wydania; potwierdzenie z podstawą; odnośnik do wiersza Klienta)
         ├─ MPN zatwierdzone (podstawa, kto, kiedy, stan: aktywny / wstrzymany)
         └─ MPN niedozwolone (powód, kto, kiedy)
```

- Wersja BOM to **pełna kopia linii**, nie lista różnic. Jedna wersja może obsługiwać kilka ZK; pakiet może mieć kilka aktywnych wersji.
- MPN zatwierdzone i zakazy przechodzą do nowej wersji automatycznie.
- Zatwierdzenie P&P zapisuje się **per technologia** (dziś SMT), tak by można było dodać zatwierdzenie przez Technologa THT.
- Tabele w schemacie `documentation` ([ADR-0005](../../../decyzje/ADR-0005%20Schemat%20bazy%20na%20obszar.md)). Komponent wskazywany po GUID z Bazy komponentów.

## Reguły

### Import i porównanie

1. **Normalizacja tylko do porównania.** Dane Klienta zostają w oryginalnym brzmieniu, z pełnymi znakami (Unicode). Przy porównaniu `µ` = `μ`; `u` nie oznacza mikro.
2. **Rozbicie desygnatorów** według reguł z 0.1.0. Każde rozbicie wymaga decyzji („rozwiń” / „zostaw jako tekst”; jest „Akceptuj wszystkie”). Domyślnie „rozwiń” tylko dla pełnych zakresów; skrócone i niejednoznaczne — ręcznie. Bez decyzji zapis jest zablokowany.
3. **Porównanie BOM–P&P** (desygnator, Footprint, opis, Part TS): puste po obu stronach to ostrzeżenie „brak danych”, nie rozbieżność. Quantity różne od liczby desygnatorów — ostrzeżenie.
4. **Podlinie:** akceptacja jednym kliknięciem; zaakceptowana zostaje w linii. Nierozwiązana staje się linią wydzieloną z odnośnikiem do źródłowej, a potem: „scal z linią N” (numer nie jest używany ponownie, linia zostaje w historii jako scalona) albo „osobny komponent”. Linie BOM Klienta zachowują kolejność; linie wydzielone mogą być na końcu.
5. **Potwierdzenie linii wymaga podstawy:** odpowiedź Klienta, wcześniejsza realizacja albo decyzja Opiekuna z uzasadnieniem; kto i kiedy. Opiekun może potwierdzić linię wydzieloną bez Klienta.
6. **DNP** tylko jako osobne słowo (DNP, DNM, DNI, NM, NI), do zatwierdzenia w podglądzie.
7. **PCB** to zwykła linia ze sposobem montażu PCB, automatycznie na końcu.
8. **Kilka MPN w komórce:** separator główny i wyjątek per komórka; oryginał zostaje. MPN trafiają na listę zatwierdzonych z podstawą „z BOM Klienta”; pierwszy jest preferowany.

### Dane linii i Baza komponentów

9. **Rozpoznany MPN uzupełnia od razu jak najwięcej danych z Bazy komponentów.** Rozpoznanie po MPN i producencie z BOM; przy kilku kandydatach wybiera Opiekun.
10. **Sekcja TS jest wiodąca, sekcja Oryginał niezmienna.** Niezgodność Value z bazą i zapisem Klienta to rozbieżność do decyzji (porównanie liczb po przeliczeniu jednostek, tylko parametry podane przez Klienta). Nierozpoznany zapis Klienta blokuje linię do „sprawdziłem”.
11. **Nowe MPN spoza bazy** trafiają na listę: dodaj bez zatwierdzenia / dodaj z zatwierdzeniem / pomiń. Nic nie jest dopisywane do bazy automatycznie.
12. **Sposób montażu** z bazy po MPN. Bez MPN w bazie — po Footprint: co najmniej 6 wystąpień i zawsze ten sam sposób → automatycznie (oznaczone „z Footprint”); 1–5 albo różne → podpowiedź do zatwierdzenia; brak → puste.
13. **Typ komponentu** z bazy; litery desygnatora są tylko podpowiedzią.
14. **Footprint dedykowany** komponentu pokazuje się, gdy pakiet należy do Klienta, którym oznaczono wariant footprintu. Pusty Footprint w bazie → lokalny wpis w linii jako kopia Case.

### Part TS

15. **Value zawsze pełny** (z PPM). Part TS to konfigurowalny widok: dla rezystorów, drabinek, kondensatorów, tantali i elektrolitów z parametrów, dla pozostałych typów — MPN.
16. **PPM w Part TS** domyślnie, gdy tolerancja < 1% albo PPM ≤ 50. Ustawienia działają kolejno: firma → komponent → Klient → pakiet → linia. Zmiana ustawień w istniejących pakietach daje listę „było → będzie” do zatwierdzenia.
17. **Parametry nieukrywalne:** rezystor i drabinka — rezystancja, tolerancja, moc; kondensator — pojemność, napięcie, dielektryk, tolerancja; tantal i elektrolit — pojemność, napięcie, tolerancja.

### MPN zatwierdzone, niedozwolone i zamienniki

18. **Zamiennik po liczbach:** takie same lub lepsze parametry → akceptacja automatyczna. Liczą się tylko parametry widoczne w Part TS; Case zawsze równy; AEC wymagany, jeśli linia go ma. Dielektryk lepszy dozwolony: C0G → X8R → X7R → X5R → X7S → X6S → X7T → Y5V.
19. **Zamiennik od zakupowca** o innych parametrach zatwierdza zakupowiec z podstawą (także „zatwierdzony przez Klienta”). Zakres: BOM pakietu (na stałe) albo wersja BOM dla ZK. Opiekun Projektu dostaje informację.
20. **MPN z odesłanej listy towaru powierzonego** jest zatwierdzony przez Klienta automatycznie, z podstawą „lista towaru powierzonego od Klienta”, na stałe w BOM pakietu.
21. **Po zmianie linii:** MPN niedozwolone zostają (usuwane tylko ręcznie, z powodem); MPN „z parametrów” są przeliczane; MPN od Klienta, TS i z listy BOM — „odrzuć / zachowaj / wstrzymaj decyzję”. Wstrzymany MPN nie jest widoczny dla zakupów i magazynu.

### Zatwierdzenia i wersje

22. **Zatwierdzenie P&P** przez Technologa SMT dotyczy tylko linii SMT. Pokazuje listę zmian Footprint „było → będzie” z wyborem „w bazie / tylko w projekcie”. Footprint raz ustalony obowiązuje wszędzie.
23. **Zatwierdzenie BOM** wymaga parametrów, Part TS i zamienników dla potwierdzonych linii. Udostępnia wersję zakupom i magazynowi; nie jest cofane.
24. **Nowa wersja BOM** powstaje: dla ZK (zmiany tymczasowe) albo z nowej rewizji importu — „nanieś wszystkie” / część automatycznie i reszta ręcznie / „od początku” z wyborem starego albo nowego P&P. Poprawki w sekcji TS to ta sama rewizja importu. Poprzednią wersję można zarchiwizować.
25. **Tożsamość linii przy nowym BOM Klienta — dopasowanie po desygnatorach** (zob. decyzje robocze).
26. **Przejście ZK w toku na nową wersję** proponuje Opiekun Projektu, zatwierdza zakupowiec.
27. **Zmiana po zatwierdzeniu** wymaga komentarza i trafia „Do obsłużenia” do zakupowca ZK i — gdy kompletacja ruszyła — do magazynu. Zmiana Footprintu trafia do Technologa SMT.

### Edycja

28. **Sesja edycji:** wersja robocza i jeden zapis „Zapisz zmiany” = jeden wpis historii, jedno powiadomienie. W czasie edycji inni widzą ostatni zapis.
29. **Blokada całego BOM** na czas sesji; wygasa po 1 godzinie bez aktywności, szkic zostaje ([Usługi wspólne](../../Usługi%20wspólne/00%20Mapa%20obszaru.md)).

### Pakiety z okresu Excel

30. Pakiet ma **odnośnik do archiwum** (folder albo plik), dodawany przez Opiekuna przy pierwszym imporcie. Import BOM pakietu z odnośnikiem pokazuje komunikat „pakiet realizowany w Excelu — sprawdź MPN niedozwolone i odpowiedzi Klienta w pliku archiwalnym” i wymaga potwierdzenia Opiekuna (zapis w historii).

### Kolory

31. Linie: niepotwierdzona F4B183, DNP C9C9C9, montaż THT / Press-Fit / MECH / Plugs BFFF9F, bezpośrednio do Klienta F7F5A3. Kolejność przy kilku znaczeniach: DNP → niepotwierdzona → montaż inny niż SMT → sposób wydania. Pełna informacja w podpowiedzi po najechaniu. Paleta: `src/iPart.App/Resources/DataColors.xaml`.

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| komenda | Importuj rewizję | pliki BOM i P&P, mapowanie kolumn, decyzje rozbicia |
| komenda | Potwierdź linię | z podstawą |
| komenda | Zapisz zmiany | zamyka sesję edycji wersji roboczej |
| komenda | Zatwierdź P&P | Technolog SMT; per technologia |
| komenda | Zatwierdź BOM | tworzy zatwierdzoną wersję |
| komenda | Dodaj zamiennik | od Zaopatrzenia: MPN, podstawa, zakres (BOM pakietu / ZK) |
| komenda | Dodaj MPN od Klienta | z odesłanej listy towaru powierzonego |
| zapytanie | Wersja BOM dla ZK | linie, Part TS, sposób montażu i wydania, MPN zatwierdzone i niedozwolone |
| zapytanie | Stan zatwierdzenia P&P | per technologia — dla Kompletacji |
| zdarzenie publikowane | Wersja BOM zatwierdzona | dla Zaopatrzenia i Kompletacji |
| zdarzenie publikowane | Linia zmieniona po zatwierdzeniu | „było → będzie” — do „Do obsłużenia” |
| zdarzenie publikowane | Footprint zatwierdzony | zgłoszenie do Bazy komponentów |
| zdarzenie konsumowane | Dane komponentu zmienione | od Bazy komponentów |

## Zależności

- **Baza komponentów** — dane komponentu, rozpoznanie MPN, footprinty dedykowane; przyjmuje zgłoszenia nowych MPN i Footprintów.
- **Usługi wspólne** — Klient, użytkownicy i dostępy, ustawienia, historia, blokady, „Do obsłużenia”, komunikaty.
- **Zaopatrzenie** — zgłoszenia zamienników, zatwierdzenie przejścia ZK na nową wersję.
- **Dokumentacja pakietów (DokuMan)** — powiązanie pakietu z `EssegiId` (do ustalenia).

## Legacy

| Legacy | Co przejmuje moduł | Co znika |
|---|---|---|
| comparator (Power Apps) | porównanie BOM–P&P, rozbicie desygnatorów, podlinie, potwierdzenie | plik BOM_P&P, eksport CSV, zmiana wielkości liter, obejścia znaków |
| iPart (plik) | linie, sekcja TS, Part TS, MPN zatwierdzone i niedozwolone, zatwierdzenia, historia | aktualizacja z MASTER, import iPart v1/v2, Klient i projekt z nazw folderów, komórki START |

Okres przejściowy: nowe pakiety całe w aplikacji; pakiety w opracowaniu zaczynamy od nowa z BOM Klienta, a odpowiedzi Klienta i decyzje Opiekun przenosi ręcznie. **Bez importera starych plików iPart.** Alfa = pilot na wybranych pakietach. Szczegóły: [Decyzje VBA](../../../robocze/Decyzje%20VBA.md), 9.4–9.6.

## Stan

2026-11-26: wersja 0.1.0 ma import BOM z mapowaniem i rozbiciem desygnatorów, pakiety z rewizjami importu i liniami, podgląd linii z kolorem linii niepotwierdzonej. Brak: porównania z P&P, wersji BOM, sekcji TS, MPN zatwierdzonych i niedozwolonych, zatwierdzeń, kopii plików w bazie. Nic nie działa produkcyjnie.

## Decyzje robocze (do akceptacji)

1. **Dopasowanie linii po desygnatorach** przy nowym BOM Klienta: na poziomie pojedynczego desygnatora; ta sama grupa → ta sama linia (zmiany jako „było → będzie”); podział → jedna część zachowuje numer, reszta jako linie wydzielone; scalenie → linia z większością desygnatorów zachowuje numer, pozostałe „scalona z N”; nowe desygnatory → nowe linie, usunięte → linia nieaktywna; wynik jako propozycja do zatwierdzenia przez Opiekuna ([Projekt bazy danych](../../../robocze/Projekt%20bazy%20danych.md), 10.4).

## Otwarte pytania

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Czy Pin-In-Paste podlega zatwierdzeniu P&P przez Technologa SMT? | Technolog SMT |
| Format Value dla drabinki rezystorowej | Kierownik działu Obsługi Projektu |
| Pełna tabela reguł zamienników (dielektryki i pozostałe typy) | Kierownik działu Obsługi Projektu |
| Lista parametrów nieukrywalnych dla pozostałych typów | Kierownik działu Obsługi Projektu, Opiekunowie Projektu |
| Forma „uczenia się” zapisów Value Klienta; 30–50 przykładów od różnych Klientów | Opiekunowie Projektu |
| Footprinty dedykowane: gdy komponent ma kilka wariantów dla jednego Klienta — który obowiązuje? | Kierownik działu Obsługi Projektu, Technolog SMT |

## Decyzje

- [ADR-0003](../../../decyzje/ADR-0003%20Comparator%20i%20iPart%20jako%20jeden%20moduł.md) Comparator i iPart jako jeden moduł
- [ADR-0006](../../../decyzje/ADR-0006%20Stałe%20identyfikatory%20i%20powiązania%20zewnętrzne.md) Stałe identyfikatory
- [ADR-0007](../../../decyzje/ADR-0007%20Jeden%20mechanizm%20historii%20zmian.md) Jeden mechanizm historii zmian
- [ADR-0008](../../../decyzje/ADR-0008%20Pliki%20źródłowe%20w%20bazie%20danych.md) Pliki źródłowe w bazie danych
