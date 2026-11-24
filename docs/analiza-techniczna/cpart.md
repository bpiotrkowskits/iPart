---
status: roboczy
zakres: analiza techniczna kodu VBA pliku cPart (rev3.3)
przegląd: 2026-11-23
---

# cPart — analiza techniczna kodu VBA

Źródło: kod VBA wyciągnięty (tylko do odczytu) z `cPart_v3.xlsm` — 21 modułów (ok. 5 900 linii), 6 formularzy, układ przycisków na arkuszu START, formuły i formatowanie warunkowe szablonu. Dla ustalenia kierunków wymiany danych sprawdzono też wybrane procedury sPart, które zapisują do cPart i czytają z niego (opis szczegółowy — w analizie sPart). Dokument porównuje kod z ustaleniami z wywiadu ([wywiad-obieg-plikow.md](../wywiad-obieg-plikow.md)) i z analizą iPart ([ipart.md](ipart.md)).

Oznaczenia: **[zgodne z wywiadem]**, **[różnica względem wywiadu]**, **[nowe — nieomawiane]**, **[niepewne]** — kod tego nie rozstrzyga.

Pojęcia z analizy iPart (makro, zdarzenie, słownik, normalizacja, twarda ścieżka) obowiązują także tutaj. Dodatkowo:

- **Arkusz ZK / arkusz PP** — w cPart jeden skoroszyt zawiera wiele arkuszy roboczych. Każdy arkusz to jedna partia produkcyjna jednego ZK, nazwany `ZK_<numer(y)>_<rok>_PP<n>`. Arkusz START jest ukrytym szablonem, z którego powstają kolejne arkusze.
- **Tryb arkusza** — tekst w komórce F1 każdego arkusza: „Tryb Podglądu”, „Tryb Pracy”, „Zarchwizowano”, „Tryb Aktualizacji”. Makra działają tylko w „Tryb Pracy”.
- **ID linii** — numer w kolumnie A (1…999), równy numerowi pozycji w BOM iPart. To on łączy linię cPart z linią iPart, sPart i dPart.

## 1. Przegląd modułów

### Arkusze i tryby

| Moduł | Rola |
|---|---|
| `Ten_skoroszyt` | Zdarzenia: przy otwarciu — okno wyboru arkusza i trybu, aktualizacja szablonu, pobranie BOM z iPart; przy zamknięciu — wszystkie arkusze w Tryb Podglądu i pełna blokada; przy zapisie — blokada folderu MASTER i wydanie nowego szablonu. |
| `cS_p1_NoweZK` | Przycisk „Dodaj Nowe ZK”: kopiuje START jako nowy arkusz `ZK_…_PP1`. |
| `Public_Const` | Nazwy trybów, nazwa szablonu, hasło do arkuszy (stała w kodzie). |
| `Public_Function` | Blokady arkuszy (trzy poziomy), walidacja nazwy ZK, zmiana nazwy pliku, gradient dat aktualizacji, przeliczenie ilości dla ZK połączonych, nazwy arkuszy PP. |

### Dane z iPart

| Moduł | Rola |
|---|---|
| `cZK_p1_Import_iPart` | Przycisk „Pobierz/Aktualizuj BOM” i automat przy otwarciu: pobiera BOM (albo fuseBOM) z iPart/fusePart do wybranego arkusza. |
| `cZK_p4_OtworzPart` | Przycisk „Otwórz plik iPart” (ścieżka z A2); procedura otwierania dPart (nieużywana, patrz ryzyka). |

### Partie produkcyjne i ZK połączone

| Moduł | Rola |
|---|---|
| `cZK_p2_DodajPP` | Przycisk „Dodaj Partię Produkcyjną”: dzieli ilość arkusza na dwie partie (wariant zwykły). |
| `cZK_p2_DodajPPfuse` | To samo dla ZK połączonych w fusePart — ilości dzielone osobno dla każdego ZK. |
| `cZK_p3_ZmianaIlosciFuse` | Przycisk „Zmień ilości w ZK”: nowe ilości poszczególnych ZK w arkuszu połączonym. |

### Kompletacja i eksport do dPart

| Moduł | Rola |
|---|---|
| `cZK_p5_Kompletacja` | Przycisk „Importuj Numery Opakowań”: kolejno import opakowań i eksport dPart. |
| `cZK_p5_ImportKompletacja` | Wczytuje pliki z opakowaniami (eksport z systemu magazynowego), rozstrzyga konflikty, przypisuje opakowania do linii po MPN. |
| `cZK_p5_ExportKompletacja` | Tworzy nowy plik dPart z szablonu, kopiuje dane, rozdziela opakowania na wiersze, porównuje z poprzednim dPart. |

### Wersje szablonu

| Moduł | Rola |
|---|---|
| `A_ForwardUpdate` | Przy otwarciu: jeśli w folderze MASTER jest nowszy szablon — przenosi arkusze ZK do nowego szablonu. |
| `A_MasterUpdate` | Wydanie nowej wersji szablonu (rev i znacznik czasu) — tylko w folderze MASTER i tylko dla autora; kopie do trzech folderów. |

### Formularze

| Formularz | Kiedy się pojawia | Co użytkownik wybiera |
|---|---|---|
| `frmWybierzArkusz` | Przy każdym otwarciu pliku (jeśli istnieje choć jeden arkusz ZK). Tytuł: „Wybierz arkusz do aktualizacji BOM”. | Lista rozwijana: „[Tryb Podglądu]” (domyślnie) albo „[Tryb Pracy] <nazwa arkusza>” dla każdego arkusza ZK; przycisk OK. Zamknięcie krzyżykiem pyta o zamknięcie całego pliku („Nie można włączyć … bez aktualizacji BOM”). |
| `frmImp` | Podczas importu opakowań, gdy to samo opakowanie jest już w cPart, ale z innym MPN, ilością lub lokalizacją. | Dwa opisy obok siebie: „Nowe Dane” (z importu) i „Dotychczasowe Dane” (z cPart) — numer opakowania, MPN, ilość, lokalizacja. Cztery przyciski: weź nowe / zostaw dotychczasowe — dla tego opakowania albo dla wszystkich kolejnych konfliktów. Krzyżyk = anulowanie całego importu. |
| `frmMain` | Podczas importu, gdy opakowanie jest w cPart, a nie ma go w importowanych plikach. Tytuł: „Brak Opakowania w Importowanej Kompletacji”. | Opis opakowania i cztery przyciski: usuń / pozostaw — dla tego opakowania albo dla wszystkich („Pozostaw Dla Wszystkich”). Krzyżyk = anulowanie importu. |
| `frmPP` | Przycisk „Dodaj Partię Produkcyjną” (arkusz zwykły). | Pole 1: ile sztuk zostaje w bieżącej partii (domyślnie ilość − 1, zakres 1…ilość − 1, poza zakresem pole żółknie); pole 2: ilość nowej partii — liczona sama, zablokowana. Przycisk OK. |
| `frmFusePP` | „Dodaj Partię Produkcyjną” w arkuszu połączonym. Nagłówek „Ilości w PP<n>”. | 15 par: etykieta = numer ZK (z wiersza 1, kolumny M…AA), pole = ile sztuk tego ZK zostaje w bieżącej partii (0…ilość bazowa; wartość spoza zakresu jest przycinana i podświetlana na czerwono). Pola bez numeru ZK są nieaktywne. OK / Anuluj. |
| `frmFuseZK` | Przycisk „Zmień ilości w ZK”. | Te same 15 par (numer ZK + ilość), ale bez górnego limitu — wartość większa niż dotychczasowa podświetla się na żółto. OK / Anuluj. |

Formularze `frmImp` i `frmMain` mają w projektancie tylko część podpisów czytelnych po wyciągnięciu (np. „Nowe Dane”, „Dotychczasowe Dane”, „Pozostaw”, „Pozostaw Dla Wszystkich”); znaczenie przycisków ustalono z kodu.

### Przyciski na arkuszu (szablon START, kopiowany do każdego arkusza ZK)

| Napis na przycisku | Makro |
|---|---|
| Pobierz/Aktualizuj BOM | `AktualizujDaneZZewnetrznegoBOM` |
| Otwórz plik iPart | `OtworzInterPart` |
| Dodaj Nowe ZK | `UtworzNowyArkuszZK` (usuwany z nowych arkuszy) |
| Dodaj Partię Produkcyjną | `NowaPP` |
| Importuj Numery Opakowań | `ImportEksport` (import + eksport dPart) |
| Zmień ilości w ZK (Tylko Tryb Podglądu) | `ZmienIlosciZK` |
| Usuń Arkusz Podglądowy | `ZmienIlosciZK` — [niepewne] napis nie pasuje do makra |

## 2. Mechanizmy

### 2.1. Otwarcie pliku — wybór arkusza, trybu i pobranie BOM

- **Wejście:** lista arkuszy skoroszytu; START!D1–E1 (rev i znacznik czasu); folder MASTER na dysku sieciowym; ścieżka iPart w A2 wybranego arkusza.
- **Reguła:**
  1. START zawsze dostaje pełną blokadę.
  2. Jeśli w pliku nie ma żadnego arkusza ZK (świeży szablon) — koniec.
  3. Okno `frmWybierzArkusz`. „[Tryb Podglądu]” → nic się nie dzieje, plik otwarty do oglądania.
  4. Wybrany arkusz ZK → kontrola wersji szablonu (2.9) → F1 = „Tryb Pracy” → **automatyczne pobranie BOM z iPart** (2.2) → blokada struktury skoroszytu.
- **Wynik:** jeden arkusz w Trybie Pracy z aktualnym BOM; pozostałe w Trybie Podglądu.
- **Moduł:** `Ten_skoroszyt.Workbook_Open`, `frmWybierzArkusz`.
- **[zgodne z wywiadem]** „Dane z iPart są aktualizowane przy każdym włączeniu cPart” — ale **tylko dla arkusza wybranego do pracy**, nie dla wszystkich ZK/PP w pliku i nie w Trybie Podglądu.
- **[zgodne z wywiadem]** Tryb podglądu pozwala obejrzeć plik bez aktualizacji rdzenia BOM.

**Zamknięcie pliku:** wszystkie arkusze poza START dostają F1 = „Tryb Podglądu” i pełną blokadę; blokada struktury skoroszytu; usunięcie tymczasowej kopii szablonu (`Ten_skoroszyt.Workbook_BeforeClose`). cPart przy zamknięciu **nie wysyła** nigdzie danych.

### 2.2. Pobranie / aktualizacja BOM z iPart (lub fusePart)

- **Wejście:** plik wskazany w A2 arkusza (iPart albo fusePart); arkusz „BOM” albo „fuseBOM”; arkusz „MPN zatwierdzone”; START!J16:J26 pliku źródłowego (komentarze Opiekuna Projektu).
- **Reguła:**
  1. Działa tylko w Trybie Pracy; blokada dla START i arkuszy zarchiwizowanych.
  2. Brak lub zła ścieżka w A2 → okno wyboru pliku, startujące w folderze Klienta na dysku dokumentacji (Klient odczytywany z nazwy folderu cPart — folder za „Kompletacje”). Wybrana ścieżka zapisywana w A2.
  3. **Kontrola zatwierdzenia iPart:** komórki N1:N4 arkusza BOM (data zatwierdzenia, zatwierdzający, Klient, projekt) — w starszych plikach L1:L4. Wszystkie puste → komunikat „nie został zatwierdzony do działań logistycznych” i koniec. Część pusta → „nie ma pełnej informacji zatwierdzającej” i koniec. Komunikat podaje adres e-mail działu.
  4. Arkusz fuseBOM → nagłówki zmieniane na FuseID, Różnice, FuseDes, SumaQty; H4 = Klient z fusePart, G5/H5 = „Nazwa fuse”, G6/H6 = „Komplet” = 1 (kolor fioletowy).
  5. **Dopasowanie linii po pozycji:** linia źródłowa nr *k* (licząc od pierwszego wiersza danych) trafia do wiersza cPart, który ma w kolumnie A numer *k*. Nie ma porównania po desygnatorze ani MPN.
  6. **Kolumny po nagłówkach** (wiersz 10 cPart ↔ wiersz nagłówków źródła): każda kolumna cPart od C do AZ o tym samym nagłówku co w źródle jest nadpisywana, jeśli wartość się różni. Reguła Case/Footprint: gdy źródło ma Case i Footprint — Footprint pomijany; gdy ma tylko Footprint — traktowany jako Case. Kolumna EntPart jest wyłączona z mapowania (patrz punkt 8).
  7. **Ochrona Mounting:** Mounting w cPart **nie jest nadpisywany**, jeśli zawiera ręczny wpis „jednorazowo … DNP”, „zamiennik … SMT” lub „zamiennik … THT”, albo jeśli obecna wartość nie jest na liście SMT, THT, PRESSFIT, PIN-IN-PASTE, MECH.
  8. **EntPart → Dostawca:**
     - EntPart „?” albo w Notes „komponent powierzany (do potwierdzenia)” → EntPart „?”, dostawca bez zmian;
     - EntPart ☑ albo w Notes „komponent powierzany” → EntPart ☑, Dostawca = „Klient” (pusty) albo „? Klient | <poprzedni dostawca>” (gdy był inny);
     - inny tekst w EntPart → jak wyżej, a dodatkowo tekst trafia do kolumny „Nazwa”, jeśli była pusta.
  9. **MPN zatwierdzone:** lista MPN z arkusza „MPN zatwierdzone” (od kolumny O, bez powtórzeń) łączona przecinkami do jednej komórki.
  10. **Oznaczanie zmian:** przed aktualizacją zdejmowane są wszystkie pogrubienia w B:O. Każda zmieniona komórka w B:O jest pogrubiana (tylko gdy plik był już kiedyś aktualizowany — C1 niepuste). Linia z jakąkolwiek zmianą dostaje w B „Data Aktualizacji” = teraz.
  11. **Gradient:** kolumna B wszystkich linii z datą jest kolorowana od białego (najstarsza aktualizacja) do zielonego (najnowsza). Daty różniące się o mniej niż minutę liczą się jako jedna aktualizacja. Kolor zależy od **kolejności** aktualizacji, nie od upływu dni.
  12. „Opis z Pozycji” = nazwa arkusza (np. `ZK_…_PP1`); w arkuszu połączonym — początek FuseDes i fragment numeru ZK.
  13. Komentarze z START!J16:J26 iPart wyświetlane w okienku („Komunikaty (START)”).
  14. Uzupełnienie pustych: H4 = Klient, H5 = projekt (z iPart N4), J7 = login osoby aktualizującej.
  15. Arkusz zwykły (nie fuse) → usunięcie bloku ilości ZK (L1:AB3) i przycisków dla ZK połączonych.
  16. **Zmiana nazwy pliku** na `<ZK>_<projekt>_cPart.xlsm` (ZK = nazwa arkusza do „_PP”); C1 = data aktualizacji; zapis.
- **Wynik:** rdzeń BOM, MPN zatwierdzone, EntPart/Dostawca, Historia Zmian (kolumna C — z iPart po nagłówku) aktualne; wpisy magazynu (opakowania, wydano, uwagi) nietknięte, bo ich nagłówków nie ma w iPart.
- **Moduł:** `cZK_p1_Import_iPart.AktualizujDaneZZewnetrznegoBOM`.
- **[zgodne z wywiadem]** Wpisy magazynu zostają mimo zmian w iPart; zmiany są pogrubiane i podświetlane gradientem w kolumnie „Data Aktualizacji”.
- **[różnica względem wywiadu]** Gradient to kolejność aktualizacji (od najstarszej do najnowszej w tym arkuszu), a nie wiek zmiany. Pogrubienie pokazuje tylko zmiany z **ostatniej** aktualizacji.
- **[nowe — nieomawiane]** cPart odmawia pobrania BOM, dopóki iPart nie jest zatwierdzony „do działań logistycznych” (pełne N1:N4). To jedyna blokada zatwierdzenia sprawdzana w cPart.
- **[nowe — nieomawiane]** Ręczne wpisy w Mounting w cPart („jednorazowo DNP”, „zamiennik SMT/THT”) działają jak **wyjątki dla jednego ZK/partii** — nie są nadpisywane przez iPart i mają własne formatowanie (szare tło dla „jednorazowo DNP”). To dzisiejszy odpowiednik „nakładki ZK” z wywiadu.
- **[nowe — nieomawiane]** Komentarze Opiekuna Projektu z iPart trafiają do magazynu jako okienko przy każdej aktualizacji.

### 2.3. Dane z sPart (zakupy) — kod w sPart

cPart nie ma kodu odbierającego dane z sPart. Robi to sPart (`sZK_p3_Eksport.UtworzLubAktualizujKompletacje`):

- **Kiedy:** przy zamknięciu sPart, dla każdego arkusza ZK w Trybie Pracy z wpisaną ścieżką cPart (AP2 w sPart), albo przyciskiem w sPart.
- **Co:** kolumny o tych samych nagłówkach (wiersz 10), w tym Ilość, Dostawca, Nazwa, Status MPN, Tracking/data dostawy, ZD; „Uwagi” z sPart → „Zakupy” w cPart; nagłówek A2, C1, H1:H5, J1:J6, H6 (tylko gdy puste). Data w B — zostaje nowsza. Wyjątek: przy statusie „Generic” nie nadpisuje „Nazwa” wpisanej w cPart.
- **Dokąd:** **wyłącznie do arkusza `<ZK>_PP1`**. Gdy go brak — tworzy go z START. Gdy brak pliku cPart — tworzy go z szablonu w folderze kompletacji Klienta.
- **Dodatkowe wiersze:** jeśli numer linii (kolumna AH „RowID”) nie pasuje, sPart **wstawia wiersz** w cPart (np. przy podzieleniu pozycji na dwa źródła — zakup i Klient) i kopiuje formuły „Różnica” i „Ile płytek?”.
- **[zgodne z wywiadem]** Dane z sPart przychodzą przy zamknięciu sPart.
- **[różnica względem wywiadu]** Aktualizowany jest tylko arkusz PP1. Partie utworzone później (PP2, PP3…) nie dostają aktualizacji zakupów.
- **[nowe — nieomawiane]** sPart otwiera cPart do zapisu. Jeśli magazyn ma cPart otwarty, aktualizacja przy zamknięciu sPart jest pomijana bez komunikatu (błąd wyciszony w zdarzeniu zamknięcia).

W drugą stronę sPart czyta z cPart (`sZK_p6_Import_cPart.DaneZCPart`, przy pobieraniu iPart w sPart): kolumny „Wydano” i „Magazyn” (uwagi magazynu) ze wszystkich arkuszy PP danego ZK → „Wydano z Magazynu” i „Uwagi Magazyn” w sPart.

### 2.4. Liczenie: zapotrzebowanie, różnica, „Ile płytek?”, „Max płytek”

Te wartości liczą **formuły** w arkuszu, nie makra:

| Kolumna | Formuła (opis) |
|---|---|
| W „Zapotrzebowanie” | Quantity × ilość w partii (H6). |
| X „Wydano” | Wpisywane ręcznie — żadne makro cPart go nie wypełnia. [niepewne] Kto i jak wpisuje? |
| Y „Różnica” | Suma „Wydano” ze wszystkich wierszy o tym samym ID (kolumna RowID, część przed „\|”) − Zapotrzebowanie. Dzięki temu pozycja rozbita na kilka wierszy (np. dwa źródła) liczy się razem. Wiersze dodatkowe pokazują „jw.”. |
| AG „Ile płytek?” | DNP → puste; „Wydano” wpisane tekstem → Quantity × H6 (pozycja traktowana jako kompletna); w pozostałych przypadkach (Zapotrzebowanie + Różnica) / Quantity, czyli wydane sztuki ÷ sztuki na płytkę. |
| Y8 „Max płytek” | Najmniejsza wartość „Ile płytek?” w wierszach **12–259**, zaokrąglona w dół, nie więcej niż H6; wyświetlana jako „x/H6”. |

- **Formatowanie warunkowe:** linia z Wydano > 0 i Różnicą ≥ 0 — kolor „kompletna”; Różnica < 0 — kolor „brak”; THT — osobny kolor, a THT z nadwyżką wydania — wyróżnienie; Confirmed = false — wyróżnienie; DNP i „jednorazowo DNP” — wyszarzenie; Status MPN: „Generic”, „Do Weryfikacji”/„Niedopuszczony”, „Zatwierdzony przez TS”/„Zatwierdzony przez Klienta”/„Dopuszczony Jednorazowy” — trzy kolory.
- **[zgodne z wywiadem]** Plik liczy, ile płytek można zmontować z tego, co wydano.
- **[niepewne]** „Max płytek” pomija pierwszą linię BOM (wiersz 11, ID 1) i linie powyżej ID 249. Pierwsza linia to często PCB — możliwe, że celowo.

### 2.5. Import opakowań (kompletacja)

- **Wejście:** jeden lub kilka plików Excel wybranych przez użytkownika (pierwszy arkusz, nagłówki w wierszu 1: „Numer Opakowania”, „MPN”, „Nazwa PL (MPN)”, „Ilość w kompletacji”, „Lokalizacja krótka”) — [niepewne] eksport z systemu magazynowego (TS Kompletor); w cPart kolumny „Nazwa”, „Numer Opakowania”, „Ilość w opakowaniu”, „Lokalizacja”.
- **Reguła:**
  1. Tylko Tryb Pracy; pliki źródłowe otwierane tylko do odczytu. Ten sam numer opakowania w kilku plikach — wygrywa ostatni.
  2. Opakowania już wpisane w cPart (z wieloliniowych komórek) są porównywane z importem:
     - nowe w imporcie → dodawane;
     - w obu, różne MPN, ilość lub lokalizacja → okno `frmImp` (MPN uznany za zgodny, gdy pasuje MPN albo „Nazwa PL (MPN)” z importu);
     - w cPart, brak w imporcie → okno `frmMain` (usuń albo pozostaw).
  3. Dopiero po decyzjach czyszczone są kolumny opakowań w cPart i wszystko wpisywane od nowa.
  4. **Przypisanie do linii po MPN:** „Nazwa” każdej linii dzielona po „/”; porównanie po normalizacji (usunięcie `#`, `-`, `,`, `.` i znacznika „|POWIERZONY|”). Najpierw po MPN, potem po „Nazwa PL (MPN)” (takie opakowanie dostaje kolor czcionki jako sygnał). Opakowanie pasujące do kilku linii trafia do **wszystkich** tych linii.
  5. W komórce jednej linii opakowania są dopisywane w kolejnych wierszach komórki; ilości łączone znakiem „+” (np. `4000+4000`); lokalizacje w kolejnych wierszach.
  6. **Opakowania powtórzone:** opakowanie w kilku liniach → w kolumnie „Opakowania powtórzone” wpis „<numer> również w Designator: …”.
  7. **Nieprzypisane:** opakowania bez pasującej linii → pod BOM blok „Nieprzypisane Opakowania” / „Niewykryte MPN”.
- **Wynik:** kolumny opakowań w cPart; potem od razu eksport dPart (2.6).
- **Moduł:** `cZK_p5_ImportKompletacja.Import`, formularze `frmImp`, `frmMain`.
- **[różnica względem wywiadu]** Magazynier **nie wpisuje opakowań ręcznie** — importuje je z plików systemu magazynowego. Ręcznie wpisuje się (wg formuł) tylko „Wydano”. [niepewne]
- **[nowe — nieomawiane]** Usunięcie opakowania w oknie `frmMain` nie zostawia żadnego śladu. To miejsce, w którym potrzebne jest „wycofanie z kompletacji” z historią (wywiad).
- **[nowe — nieomawiane]** Dopasowanie po MPN z „Nazwa” (MPN zamówiony, z sPart), nie po Part TS ani desygnatorze.

### 2.6. Eksport do dPart

- **Wejście:** bieżący arkusz PP; szablon dPart z folderu wzorów na dysku magazynu (`dPart_v3.0.1` dla zwykłego, `dPart_v3.f` dla połączonego — wybór po nagłówku E10); plik iPart/fusePart z A2 (Footprint); poprzedni dPart tej partii.
- **Reguła:**
  1. Tylko Tryb Pracy; wymagana nazwa projektu w H5.
  2. Folder docelowy: folder przedprodukcji Klienta na dysku magazynu (Klient = nazwa folderu, w którym leży cPart). Istniejące eksporty tej partii przenoszone do podfolderu `z_NIEAKTUALNE`.
  3. Nazwa: `<ZK>_PP<n>_<projekt>_c<licznik>_dPart.xlsm`, licznik = największy dotychczasowy + 1.
  4. Kopia szablonu → arkusz START przemianowany na nazwę partii → kopiowane A2, C1, G1:J7; X1 = czas eksportu, V2 = pełna ścieżka cPart.
  5. W cPart: AB7 = czas eksportu, Z8 = ścieżka nowego dPart.
  6. Dane kopiowane **po nagłówkach** wiersza 10; **Footprint pobierany na nowo z iPart po ID** (aktualny Footprint Technologa); bloki „Nieprzypisane Opakowania” / „Niewykryte MPN” dopisywane na dole.
  7. Arkusz połączony: blok ilości ZK (L1:…3), przycisk zmiany ilości (podpięty pod makro dPart) i kolumny „ZK…” z fuseBOM (po ID).
  8. Porównanie z poprzednim dPart: „Data Aktualizacji” różna po ID → komórka podświetlona na pomarańczowo.
  9. **Rozdzielenie opakowań:** każde opakowanie dostaje własny wiersz; wiersze dodatkowe mają tylko Mounting, Notes, Nazwa, Status i opakowanie; „Wydano” sumowane do pierwszego wiersza grupy; różne MPN w jednej grupie → pogrubienie i kolor.
  10. **Numer pojemnika:** z „Lokalizacji” (tekst z „/”) wycinany przedostatni (albo ostatni) człon; nagłówek zmieniany na „Numer Pojemnika”.
  11. Opakowania powtórzone liczone od nowa jako „również dla ID: …”.
  12. Kolorowanie: SMT/PCB — paski niebieskie/białe wg parzystości ID, THT — paski zielone; pomijane linie Confirmed = false i DNP.
  13. Pełna blokada dPart i zapis.
- **Wynik:** nowy plik dPart; poprzednie w `z_NIEAKTUALNE`.
- **Moduł:** `cZK_p5_ExportKompletacja.Eksport`.
- **[zgodne z wywiadem]** dPart powstaje jako eksport z cPart; każdy eksport = nowy plik; dane z systemu magazynowego trafiają do dPart (przez import w tym samym kroku).
- **[różnica względem wywiadu]** **Blokady „bez zatwierdzenia P&P przez Technologa nie można zakończyć kompletacji i wyeksportować dPart” nie ma w kodzie cPart** ani w żadnym innym pliku poza iPart. Eksport sprawdza tylko tryb arkusza i nazwę projektu. [niepewne] Blokada działa dziś organizacyjnie?
- **[nowe — nieomawiane]** Import i eksport są jednym przyciskiem — każdy import opakowań od razu tworzy nowy dPart.
- **[nowe — nieomawiane]** Numer pojemnika nie jest osobną daną — jest wycinany z tekstu lokalizacji.

### 2.7. Partie produkcyjne (PP)

**Arkusz zwykły** (`NowaPPstandard` + `frmPP`):

1. Tylko Tryb Pracy, ilość w H6 > 1.
2. Użytkownik podaje, ile sztuk zostaje w bieżącej partii; reszta przechodzi do nowej.
3. Bieżący arkusz: H6 = ilość pozostająca. Kopia arkusza **przed** bieżącym, nazwa z kolejnym numerem (`…_PP2`), H6 = ilość nowej partii.
4. W nowym arkuszu czyszczone są: Z7, X8:Y8, V11:V1009, X11:Z1009, AB11:AB1009.

**Arkusz połączony** (`NowePPfuse` + `frmFusePP`): dla każdego ZK (M1:AA1) osobno — wpisana ilość zostaje w bieżącej partii, różnica przechodzi do nowej; przeliczenie SumaQty (kolumna F) z FuseDes; w nowym arkuszu O6:Q6 = kto i kiedy utworzył partię. Kolumny magazynu w kopii **nie są czyszczone**.

- **[zgodne z wywiadem]** cPart dzieli kompletację na partie produkcyjne; zakupy liczą całe ZK.
- **[nowe — nieomawiane]** Partia to kopia arkusza — dane BOM w każdej partii aktualizowane są osobno (przy wybraniu jej do pracy).

### 2.8. ZK połączone (fuse)

- **Rozpoznanie:** nagłówek E10 = „FuseDes” (po pobraniu z fuseBOM).
- **Ilości:** wiersz 1 kolumn M…AA = numery ZK, wiersz 2 = ilości. Kolumna E (FuseDes) zawiera linie w formacie `<ZK>: <liczba>: <desygnatory>`; SumaQty (F) = Σ ilość ZK × liczba desygnatorów.
- **Zmiana ilości** (`ZmienIlosciZK` + `frmFuseZK`): kopia arkusza przed bieżącym z dopiskiem daty `_DDMM`, nowe ilości, przeliczenie F, O6:Q6 = kto i o której zmienił. Stary arkusz zostaje bez zmian.
- **[zgodne z wywiadem]** Obsługa BOM-ów połączonych w fusePart; osobny wzór dPart dla nich.
- **[nowe — nieomawiane]** Zmiana ilości ZK tworzy nowy arkusz zamiast zmieniać istniejący — stary zostaje jako stan poprzedni.

### 2.9. Wersje szablonu (MASTER)

- **Otwarcie:** oczekiwana nazwa szablonu `cPart — <rev> — <znacznik>.xlsm` z START!D1–E1. Jeśli takiego pliku nie ma w folderze MASTER → po zgodzie: stary plik do `z_NIEAKTUALNE` (z datą w nazwie) → kopia najnowszego szablonu → **START z nowego szablonu, wszystkie arkusze ZK ze starego pliku** → poprawienie przypisań przycisków → nowa rev w D1/E1 każdego arkusza → zapis pod starą nazwą. Odmowa = zamknięcie pliku.
- **Zapis w folderze MASTER:** dozwolony tylko dla jednego loginu wpisanego w kod (autora); pytanie o nowy numer minor/major; zapis jako nowy szablon i kopie zapasowe w trzech folderach.
- **Moduł:** `A_ForwardUpdate`, `A_MasterUpdate`.
- **[nowe — nieomawiane]** Inaczej niż w iPart: aktualizuje się też START (szablon nowych arkuszy), ale istniejące arkusze ZK zachowują stary układ kolumn — dostają tylko nowe makra.

### 2.10. Blokady arkuszy

| Poziom | Kiedy | Co można edytować |
|---|---|---|
| Pełna (`ProtectALL`) | START, Tryb Podglądu, po eksporcie w dPart | Nic (tylko filtrowanie). |
| BOM (`ProtectBOM`) | Tryb Pracy, gdy H1 (zakupowiec) wypełnione | Kolumny od W w prawo (magazyn); „Nazwa” tylko w wierszach ze Statusem MPN „Generic”. |
| BOM-lite (`ProtectBOMLite`) | Tryb Pracy, gdy H1 puste (sPart jeszcze nie zapisał) | Cały obszar danych poza nagłówkami. |

- **[zgodne z wywiadem]** Blokady arkuszy z hasłem wpisanym w kod; tryb podglądu.
- **[nowe — nieomawiane]** Zanim sPart pierwszy raz zapisze do cPart, rdzeń BOM w cPart jest edytowalny ręcznie.

### 2.11. Kolumna „Sposób wydania”

- Kolumna AC istnieje w szablonie, ale **żadne makro, formuła ani lista wyboru jej nie używa**; nie jest też kopiowana do dPart (dPart nie ma takiego nagłówka).
- **[zgodne z wywiadem]** Kolumna nieużywana.
- **[nowe — nieomawiane]** W iPart (krok 3) Mounting może mieć wartości „Bezpośrednio do Klienta” i „Materiały Dodatkowe” — to częściowo ta sama informacja.

## 3. Współzależności z innymi plikami

| Kierunek | Plik | Arkusz / zakres / kolumny | Kiedy | Moduł / procedura | Co przenosi |
|---|---|---|---|---|---|
| czyta | iPart (lub fusePart) | BOM / fuseBOM: N1:N4 (L1:L4), wiersz nagłówków, kolumny o nagłówkach jak w cPart, EntPart, Notes | Otwarcie w Trybie Pracy (wybrany arkusz) albo przycisk „Pobierz/Aktualizuj BOM” | `cZK_p1_Import_iPart.AktualizujDaneZZewnetrznegoBOM` | Kontrola zatwierdzenia; rdzeń BOM po nagłówkach (z Historią Zmian); EntPart → Dostawca/Nazwa. Plik otwierany w trybie zapisu, zamykany bez zapisu. |
| czyta | iPart | MPN zatwierdzone: kolumny od O | j.w. | j.w. | Lista MPN zatwierdzonych → jedna komórka. |
| czyta | iPart | START!J16:J26 | j.w. | j.w. | Komentarze Opiekuna Projektu → okienko dla magazynu. |
| czyta | iPart (lub fusePart) | BOM / fuseBOM: ID + Footprint | Eksport dPart | `CopyFootprintFromMainByID` | Aktualny Footprint do dPart. |
| czyta | fusePart | fuseBOM: kolumny „ZK…” (wiersz = ID + 10) | Eksport dPart (arkusz połączony) | `AppendFusePartData` | Podział desygnatorów na ZK do dPart. |
| zapisuje (kod w sPart) | sPart → cPart | arkusz `<ZK>_PP1`: kolumny wg nagłówków (Ilość, Dostawca, Nazwa, Status MPN, Tracking, ZD, Zakupy ← Uwagi), A2, C1, H1:H5, J1:J6, H6; wstawianie wierszy wg RowID | Zamknięcie sPart (Tryb Pracy + ścieżka cPart w AP2) albo przycisk w sPart | `sZK_p3_Eksport.UtworzLubAktualizujKompletacje` (sPart) | Dane zakupowe; tworzy cPart z szablonu, jeśli nie istnieje. |
| czyta (kod w sPart) | cPart → sPart | wszystkie arkusze PP danego ZK: „Wydano”, „Magazyn” | Pobieranie iPart w sPart | `sZK_p6_Import_cPart.DaneZCPart` (sPart) | Ilości wydane i uwagi magazynu do sPart. |
| czyta | pliki z systemu magazynowego (TS Kompletor — [niepewne]) | pierwszy arkusz: Numer Opakowania, MPN, Nazwa PL (MPN), Ilość w kompletacji, Lokalizacja krótka | Przycisk „Importuj Numery Opakowań” | `cZK_p5_ImportKompletacja.Import` | Opakowania, ilości, lokalizacje. |
| tworzy | dPart | nowy plik `<ZK>_PP<n>_<projekt>_c<nn>_dPart.xlsm` z szablonu (zwykły / `.f`) w folderze przedprodukcji Klienta | Przycisk „Importuj Numery Opakowań” (po imporcie) | `cZK_p5_ExportKompletacja.Eksport` | Nagłówek, cała tabela po nagłówkach, opakowania rozdzielone na wiersze, pojemniki, nieprzypisane. |
| zapisuje (przenosi) | dPart (poprzednie) | pliki tej partii → `z_NIEAKTUALNE` | j.w. | `MoveExistingExportsToOld` | Archiwizacja poprzednich eksportów. |
| czyta | dPart (poprzedni) | „ID”, „Data Aktualizacji” | j.w. | `CompareUpdateDatesWithBackup` | Wykrycie linii zmienionych od poprzedniego eksportu. |
| zapisuje | cPart (własny) | AB7, Z8 | j.w. | `Eksport` | Czas eksportu i ścieżka ostatniego dPart. |
| czyta / tworzy | szablon cPart MASTER | folder MASTER na dysku magazynu | Otwarcie (kontrola wersji); zapis w MASTER | `A_ForwardUpdate`, `A_MasterUpdate` | Nowe makra i START; wydanie szablonu, 3 kopie zapasowe. |
| łącze zewnętrzne | stary plik zakupowy | nazwa zakresu „magazyn” → arkusz Magazyn | Przy przeliczaniu | (formuły) | [niepewne] Pozostałość po wcześniejszej wersji; nieużywane przez kod. |

## 4. Reguły biznesowe zaszyte w kodzie

1. **Jeden arkusz = jedna partia jednego ZK.** Nazwa `ZK_<numer(y)>_<rrrr>_PP<n>`; dozwolone tylko litery, cyfry i `_`; pierwszy arkusz ZK zawsze `_PP1`.
2. **Tryby w F1:** Tryb Pracy (makra działają), Tryb Podglądu (nic się nie aktualizuje), Zarchwizowano (`arch_…`), Tryb Aktualizacji (po zmianie szablonu). Przy zamknięciu wszystko wraca do podglądu.
3. **Warunek pobrania BOM:** iPart zatwierdzony do działań logistycznych — wszystkie cztery komórki N1:N4 wypełnione.
4. **Łączenie linii z iPart po numerze pozycji** (kolumna A cPart = pozycja w BOM iPart). Kolumny po nagłówkach; Footprint w cPart ignorowany, gdy źródło ma Case.
5. **Wyjątki Mounting dla ZK:** „jednorazowo … DNP”, „zamiennik … SMT”, „zamiennik … THT” — nie nadpisywane przez iPart.
6. **Powierzenie → dostawca „Klient”**; gdy dostawca był już inny — „? Klient | <dostawca>” do wyjaśnienia; „?” w EntPart nie zmienia dostawcy.
7. **Zapotrzebowanie** = Quantity × ilość w partii; **Różnica** = suma wydanego dla ID − zapotrzebowanie; **Ile płytek** = wydane ÷ Quantity; **Max płytek** = minimum z linii (bez pierwszej, do ID 249), nie więcej niż ilość partii.
8. **Opakowanie ↔ linia po MPN** z kolumny „Nazwa” (lista po „/”), z rezerwowym dopasowaniem po „Nazwa PL (MPN)”; normalizacja: usunięcie `# - , .` i „|POWIERZONY|” (inna niż w iPart).
9. **Ilości opakowań** w jednej komórce łączone „+”, opakowania i lokalizacje w kolejnych wierszach komórki.
10. **Numer pojemnika** = przedostatni człon lokalizacji rozdzielonej „/” (gdy członów ≥ 4), inaczej ostatni.
11. **Status MPN** (z sPart): Generic, Do Weryfikacji, Niedopuszczony, Zatwierdzony przez TS, Zatwierdzony przez Klienta, Dopuszczony Jednorazowy. Przy „Generic” magazyn może wpisać „Nazwa”.
12. **Nazwy plików:** cPart `<ZK>_<projekt>_cPart.xlsm`; dPart `<ZK>_PP<n>_<projekt>_c<nn>_dPart.xlsm`; poprzednie dPart w `z_NIEAKTUALNE`.
13. **Klient** odczytywany z nazwy folderu (folder za „Kompletacje” albo ostatni folder ścieżki cPart).
14. **Podział partii (fuse):** dla każdego ZK osobno; nowa partia = ilość bazowa − ilość pozostająca (nie mniej niż 0).

## 5. Odpowiedzi na otwarte pytania z wywiadu

| Pytanie z wywiadu | Co mówi kod |
|---|---|
| Jak wyróżniane są zmiany w cPart? | Pogrubienie zmienionych komórek B:O (tylko z ostatniej aktualizacji) + data w „Data Aktualizacji” + gradient tej kolumny od białego (najstarsza) do zielonego (najnowsza aktualizacja). W dPart dodatkowo pomarańczowa data, gdy zmieniła się od poprzedniego eksportu. |
| Co widzi magazynier, gdy pozycja z wydanym opakowaniem zmieni się w iPart? | Pogrubione zmienione komórki i nową datę aktualizacji. Opakowania i „Wydano” zostają bez zmian i bez ostrzeżenia, nawet jeśli zmienił się MPN/Part TS — dopiero kolejny import opakowań przypisze je od nowa po „Nazwa”. |
| Gdzie i jak sprawdzana jest blokada bez zatwierdzenia P&P przez Technologa? | Nigdzie w kodzie cPart (ani sPart, fusePart, dPart). cPart sprawdza tylko zatwierdzenie BOM (N1:N4). |
| Czy „Sposób wydania” jest używany? | Nie — brak kodu, formuł i listy wyboru. |
| Format `[#,#] [#]` w Historii Zmian? | cPart przepisuje kolumnę „Historia Zmian” z iPart bez zmian — nie tworzy własnego formatu. |
| Czy cPart pobiera iPart przy otwarciu? | Tak, ale tylko dla arkusza wybranego do Trybu Pracy. |
| Czy sPart aktualizuje cPart przy zamknięciu? | Tak (kod w sPart), ale tylko arkusz PP1 i tylko gdy cPart nie jest otwarty przez kogoś innego. |

## 6. Ryzyka i słabe punkty

1. **Brak blokady P&P w kodzie.** Zasada z wywiadu nie jest wymuszana — dPart można wyeksportować bez zatwierdzenia Technologa.
2. **Aktualizacja zakupów tylko w PP1.** Partie PP2+ nie dostają nowych danych z sPart (ilości, statusy MPN, ZD, tracking, zmiany „Nazwa”) — a import opakowań dopasowuje po „Nazwa”.
3. **Ciche pominięcie aktualizacji z sPart**, gdy cPart jest otwarty (np. przez magazyn) w chwili zamykania sPart.
4. **Lista dozwolonych Mounting porównuje wielkie litery z tekstem „Bezpośrednio do Klienta”** — warunek nigdy nie jest spełniony. Skutek szerszy: każda wartość spoza listy (DNP, PCB, „Bezpośrednio do Klienta”, „Materiały Dodatkowe”) **nie jest już nigdy aktualizowana z iPart**. Jeśli iPart zmieni linię z DNP z powrotem na SMT, cPart zostaje przy DNP.
5. **Czyszczenie w nowej partii (zwykłej) może nie odpowiadać obecnemu układowi kolumn.** Kasowane są m.in. Y (formuły „Różnica”) i X8:Y8 (etykieta i formuła „Max płytek”) oraz Z7 (etykieta „Link do dPart”), a zostaje AA („Ilość w opakowaniu”). [niepewne] Wygląda na przesunięcie o jedną kolumnę po zmianie układu.
6. **„Opakowania powtórzone” w cPart nie są czyszczone** przed importem — każdy kolejny import dopisuje te same wpisy ponownie. [niepewne] W dPart liczone są od nowa, więc dotyczy tylko cPart.
7. **Stary blok „Nieprzypisane Opakowania” może zostać** pod BOM, gdy nowy import ma mniej (albo zero) nieprzypisanych — i trafić do kolejnego dPart.
8. **Łączenie z iPart po pozycji wiersza** — wymaga nienaruszonej kolejności linii (reguła z wywiadu); linie dopisane w iPart ponad ID 999 nie trafią do cPart.
9. **Usunięcie opakowania przy imporcie bez śladu** (brak historii kompletacji).
10. **Opakowanie przypisywane do wszystkich linii z tym samym MPN** — ilość liczy się wtedy w kilku liniach jednocześnie (sygnalizowane tylko jako „opakowanie powtórzone”).
11. **Zmiana nazwy pliku po każdej aktualizacji BOM** wg nazwy bieżącego arkusza — przy kilku ZK w jednym pliku nazwa pliku zależy od tego, który arkusz aktualizowano ostatnio. [niepewne]
12. **iPart otwierany w trybie zapisu** przy pobieraniu BOM — w tym czasie Opiekun Projektu nie może zapisać iPart (plik zablokowany).
13. **„Max płytek” liczony tylko dla wierszy 12–259.**
14. **„Otwórz dPart” czyta ścieżkę z Y8**, gdzie jest formuła „Max płytek”; eksport zapisuje ścieżkę w Z8. Procedura nie ma przycisku, ale nie zadziała.
15. **Przycisk „Usuń Arkusz Podglądowy” uruchamia zmianę ilości ZK**; przycisk „Zmień ilości w ZK (Tylko Tryb Podglądu)” działa wyłącznie w Trybie Pracy.
16. **Aktualizacja szablonu nie przekazuje nazwy wybranego arkusza** — flaga „Tryb Aktualizacji” nie jest ustawiana, a komunikat końcowy pokazuje pustą nazwę arkusza.
17. **Arkusze `arch_` przy zamknięciu dostają Tryb Podglądu** (wbrew komentarzowi w kodzie, że mają być pomijane).
18. **Twarde ścieżki sieciowe** (szablony dPart i cPart, folder przedprodukcji, MASTER, kopie zapasowe), **login autora** i **adres e-mail działu** wpisane w kod.
19. **Klient z nazwy folderu** — przeniesienie cPart w inne miejsce zmienia folder docelowy dPart.
20. **Zewnętrzne łącze do starego pliku zakupowego** w nazwanym zakresie „magazyn”.
21. **Szerokie wyciszanie błędów** przy blokadach, otwieraniu plików i w zdarzeniach otwarcia/zamknięcia.

## 7. Pytania do potwierdzenia przez autora

| Pytanie | Moduł |
|---|---|
| Czy eksport dPart powinien być blokowany bez zatwierdzenia P&P (iPart P&P!N1)? Gdzie dziś pilnuje się tej zasady? | `cZK_p5_ExportKompletacja` |
| Kto i jak wypełnia kolumnę „Wydano”? Czy ma ją liczyć import z systemu magazynowego? | szablon, `cZK_p5_ImportKompletacja` |
| Czy pliki importowane przyciskiem „Importuj Numery Opakowań” to eksport z TS Kompletor? | `cZK_p5_ImportKompletacja` |
| Czy sPart powinien aktualizować wszystkie partie (PP1, PP2…), a nie tylko PP1? | (sPart) `sZK_p3_Eksport` |
| Czy Mounting DNP/PCB/„Bezpośrednio do Klienta” w cPart ma być aktualizowany z iPart? Lista dozwolonych wartości ma błąd wielkości liter. | `cZK_p1_Import_iPart` |
| Które kolumny powinny być czyszczone w nowej partii (zwykłej)? Czy kasowanie formuł „Różnica” i „Max płytek” jest zamierzone? | `cZK_p2_DodajPP` |
| Dlaczego „Max płytek” liczy od wiersza 12 (bez ID 1) i tylko do wiersza 259? | szablon |
| Czy import opakowań powinien być oddzielony od eksportu dPart (dwa przyciski)? | `cZK_p5_Kompletacja` |
| Czy usuwanie opakowania przy imporcie ma zostawiać ślad (wycofanie z kompletacji)? | `cZK_p5_ImportKompletacja`, `frmMain` |
| Jakie wartości ma przyjmować „Sposób wydania” i czy wiązać go z Mounting „Bezpośrednio do Klienta” / „Materiały Dodatkowe”? | szablon |
| Do czego miał służyć przycisk „Usuń Arkusz Podglądowy” i czy „Zmień ilości w ZK” ma działać w Trybie Podglądu? | przyciski START |
| Czy jeden plik cPart ma zawierać wiele ZK? Jak wtedy ma się nazywać plik? | `Public_Function.NowaNazwaPliku` |
| Czy stary arkusz po „Zmień ilości w ZK” ma zostawać jako historia, czy być archiwizowany (`arch_`)? | `cZK_p3_ZmianaIlosciFuse` |
| Czy wpisy „jednorazowo DNP” / „zamiennik SMT/THT” w Mounting to obowiązujący sposób robienia wyjątków dla ZK? Kto je wpisuje? | `cZK_p1_Import_iPart` |
