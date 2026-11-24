---
status: roboczy
zakres: analiza techniczna kodu VBA i formuł pliku sPart (rev3.4)
przegląd: 2026-11-23
---

# sPart — analiza techniczna kodu VBA

Źródło: kod VBA wyciągnięty (tylko do odczytu) z `sPart_v3.xlsm`: 19 modułów (ok. 4 200 linii) i jeden formularz. Do tego przyciski na arkuszu START, formuły w wierszu wzorcowym, formatowanie warunkowe, listy wyboru i łącza do innych plików. Dokument porównuje kod z ustaleniami z wywiadu ([wywiad-obieg-plikow.md](../wywiad-obieg-plikow.md)) i z analizą iPart ([ipart.md](ipart.md)).

Oznaczenia: **[zgodne z wywiadem]**, **[różnica względem wywiadu]**, **[nowe — nieomawiane]**, **[niepewne]** — kod tego nie rozstrzyga.

Pojęcia z [ipart.md](ipart.md) (makro, zdarzenie, słownik, normalizacja, twarda ścieżka) obowiązują także tutaj. Dodatkowo:

- **Łącze zewnętrzne** — formuła, która czyta komórki z innego pliku Excel (np. `[3]!obudowa_zapasv3`). Excel pamięta ścieżkę do tego pliku i odświeża wartości przy otwarciu.
- **Power Query** — wbudowane w Excel narzędzie do pobierania danych (np. tabeli ze strony internetowej) przyciskiem „Odśwież”.
- **Formatowanie warunkowe** — reguła, która sama koloruje komórkę, gdy spełniony jest warunek (np. status „Niedozwolony” na czerwono). To nie jest kod VBA.
- **Arkusz ZK** — w sPart jeden plik może mieć **kilka arkuszy**, po jednym na zamówienie (ZK). Arkusz START jest ukrytym wzorem, z którego powstaje każdy arkusz ZK.

## 1. Przegląd modułów

### Praca na arkuszu ZK (`sZK_…`, `sS_…`)

| Moduł | Przycisk na arkuszu | Rola |
|---|---|---|
| `sS_p1_NoweZK` | Utwórz nowy arkusz ZK | Kopiuje ukryty START jako nowy arkusz o nazwie `ZK_<numer(y)>_<rok>`. |
| `sZK_p1_Import_iPart` | Pobierz/Aktualizuj BOM | Importuje rdzeń BOM i MPN zatwierdzone z iPart lub fusePart, pobiera wydania z cPart, zmienia nazwę pliku. |
| `sZK_p2_StatusyMPN` | Porównaj Status MPN | Ustala status MPN wpisanego przez zakupowca na podstawie list z iPart; miał też dopisywać ręczne decyzje do iPart (patrz 2.6). |
| `sZK_p3_Eksport` | Utwórz/Aktualizuj cPart [plik kompletacyjny] | Tworzy plik cPart ze wzorca albo aktualizuje istniejący (dane zakupowe). |
| `sZK_p4_OtworzPart` | Otwórz iPart, Otwórz cPart | Otwiera pliki ze ścieżek A2 i AP2. |
| `sZK_p5_Archiwum` | Zablokuj wartości [wyłączenie formuł] | Zamienia formuły P:BL na wartości („zamrożenie” wyników). Procedura `Archiwizuj` jest pusta. |
| `sZK_p6_Import_cPart` | (wywoływany z importu) | Pobiera z cPart ilości wydane i uwagi magazynu dla każdej partii produkcyjnej. |

### Wycena i kursy (`K_…`)

| Moduł | Przycisk | Rola |
|---|---|---|
| `K_OtworzPlik.Wklej` | WKLEJ | Kopiuje wycenę z wybranego pliku Excel do sekcji Wycena (stałe zakresy). |
| `K_OtworzPlik.Odswiezanie` | ODŚWIEŻ | Odświeża wszystkie zapytania (kursy walut) i wpisuje datę do AM6. |
| `K_Wyczysc.Wyczyść` | WYCZYŚĆ | Zeruje pola pomocniczego przelicznika walut (AK3, AL4, AM5). |

### Mechanizmy automatyczne i wersje pliku (`A_…`, `Ten_skoroszyt`)

| Moduł | Rola |
|---|---|
| `Ten_skoroszyt` | Zdarzenia: przy otwarciu — wybór trybu (formularz) i import BOM; przy zamknięciu — statusy MPN, aktualizacja cPart, przełączenie na podgląd; przy zapisie — blokada folderu MASTER. |
| `A_ForwardUpdate` | Przy wejściu w tryb pracy: jeśli w folderze MASTER jest nowszy szablon — przenosi arkusze ZK do nowego szablonu. |
| `A_MasterUpdate` | Wydanie nowej wersji szablonu (rev + znacznik czasu) i kopie zapasowe — tylko w folderze MASTER i tylko dla autora. |

### Formularz i funkcje wspólne

| Moduł | Rola |
|---|---|
| `frmWybierzArkusz` | Okno wyboru trybu przy otwarciu pliku (opis w 2.1). |
| `Public_Const` | Hasło do arkuszy (stała w kodzie), adres komórki trybu (F1), nazwy trybów. |
| `Public_Function` | Tryby, ochrona arkuszy, walidacja nazwy ZK, zmiana nazwy pliku, gradient koloru aktualizacji, odczyt Klienta ze ścieżki. |
| `Arkusz1…4` | Moduły arkuszy — puste. |

## 2. Mechanizmy

### 2.1. Otwarcie pliku — wybór trybu

- **Wejście:** arkusze pliku; komórka F1 każdego arkusza ZK (tryb).
- **Reguła:**
  1. Plik w folderze `z_NIEAKTUALNE` (archiwum) — kod nic nie robi.
  2. START i arkusze `arch_…` są zabezpieczane.
  3. Jeśli nie ma żadnego arkusza ZK (nowy plik) — koniec, użytkownik zaczyna od „Utwórz nowy arkusz ZK”.
  4. Pojawia się okno **„Wybierz arkusz do aktualizacji BOM”** (formularz `frmWybierzArkusz`): jedna lista rozwijana i przycisk OK. Pozycje listy:
     - `[Tryb Podglądu]` (domyślnie),
     - `[Tryb Pracy] <nazwa arkusza ZK>` — po jednej pozycji na każdy arkusz ZK (bez START, Kursy walut, Magazyn, obudowy i `arch_…`),
     - `[Tryb Podsumowania]`.
     Zamknięcie okna krzyżykiem — pytanie, czy zamknąć plik bez zapisu („nie można włączyć bez aktualizacji ostatniego BOM”).
  5. **Tryb Podglądu** — nic się nie dzieje; makra sprawdzające F1 nie zadziałają.
  6. **Tryb Podsumowania** — wszystkie arkusze ZK dostają F1 = „Tryb Podsumowania” i ochronę `ProtectSUP` (intencja: edycja tylko kolumn BA:BL — sekcja kierownika logistyki i podsumowanie kosztów).
  7. **Tryb Pracy** dla wybranego arkusza — kolejno: kontrola wersji szablonu (2.10), F1 = „Tryb Pracy”, **automatyczny import BOM z iPart/fusePart** (2.3). Błędy importu przy otwarciu są wyciszane.
- **Wynik:** jeden arkusz ZK w trybie pracy, pozostałe w podglądzie.
- **Moduł:** `Ten_skoroszyt.Workbook_Open`, `frmWybierzArkusz`.
- **[zgodne z wywiadem]** Tryby podglądu, pracy i podsumowania; „sPart przy każdym otwarciu widzi aktualny BOM” — w trybie pracy import BOM uruchamia się sam.
- **[różnica względem wywiadu]** Ochrona `ProtectALL` (używana dla podglądu) najpierw **odblokowuje wszystkie komórki A1:CZ10009**, a dopiero potem włącza ochronę arkusza. W efekcie w trybie podglądu i podsumowania można ręcznie zmieniać komórki — ochrona blokuje tylko wstawianie/usuwanie wierszy i kolumn, formatowanie i obiekty. Tryb podglądu chroni więc przed **uruchomieniem makr**, ale nie przed ręczną edycją. [niepewne] Czy to zamierzone?

### 2.2. Nowy arkusz ZK

- **Wejście:** okienko z numerem ZK.
- **Reguła:** nazwa musi zaczynać się od `ZK_`, kończyć 4 cyframi (rok) i zawierać tylko litery, cyfry i `_` (format `ZK_numer(y)_rrrr`). Nie może się powtarzać. Ukryty START jest kopiowany jako nowy arkusz, F1 = „Tryb Pracy”, przycisk „Utwórz nowy arkusz ZK” jest usuwany z kopii.
- **Moduł:** `sS_p1_NoweZK.UtworzNowyArkuszZK`.
- **[nowe — nieomawiane]** Jeden plik sPart może prowadzić **kilka ZK** — każde na osobnym arkuszu. Dotąd mówiliśmy o „pliku sPart na ZK”.

### 2.3. Import BOM z iPart lub fusePart („Pobierz/Aktualizuj BOM”)

- **Wejście:** ścieżka w A2 arkusza ZK. Gdy pusta lub błędna — okno wyboru pliku otwierane w folderze dokumentacji Klienta na dysku sieciowym (Klient odczytany ze ścieżki sPart: folder po „Zamowienia”). W pliku źródłowym: arkusz **BOM** (iPart) albo **fuseBOM** (fusePart), arkusz „MPN zatwierdzone”, START (komunikaty).
- **Reguła:**
  1. **Migracja układu:** jeśli w AC10 jest jeszcze nagłówek „Kompresorownia”, wstawiane są kolumny „W Kompletacji” i „Ilość na ZD”; AX10 dostaje nagłówek „Wydano z Magazynu”.
  2. Plik źródłowy otwierany jest bez uruchamiania jego makr (zdarzenia wyłączone) i zamykany bez zapisu.
  3. **Blokada zatwierdzenia:** komórki N1:N4 arkusza BOM (data, zatwierdzający, Klient, projekt; w starych wersjach L1:L4) muszą być **wszystkie wypełnione**. Inaczej komunikat „BOM nie został zatwierdzony do działań logistycznych — skontaktuj się z Działem Obsługi Projektu” i przerwanie.
  4. Dla fuseBOM: nagłówki A10/C10/E10/F10 zmieniają się na FuseID / Różnice / FuseDes / SumaQty, H4 = Klient z fusePart, H5 = nazwa fuse, H6 („Komplet”) = 1 — bo ilości są już zsumowane.
  5. **Dopasowanie linii po pozycji:** wiersz źródłowy *i* ma numer `i − 10` (albo `i − 2` w starym układzie) i trafia do wiersza sPart, który ma ten numer w kolumnie A. Numer ID z pliku źródłowego nie jest czytany — liczy się kolejność wierszy.
  6. **Kopiowanie po nagłówkach** (wiersz 10, kolumny C…AZ): każda kolumna sPart, której nagłówek występuje w źródle, dostaje wartość źródła, jeśli się różni. Reguła Case/Footprint: gdy źródło ma obie kolumny, Footprint jest pomijany; gdy ma tylko Footprint (stary iPart), trafia do Case.
  7. **Ochrona Mounting:** wartość w sPart nie jest nadpisywana, gdy zawiera „jednorazowo” + „DNP” albo „zamiennik” + „SMT”/„THT”, albo gdy jest spoza listy SMT, THT, PressFit, Pin-In-Paste, MECH.
  8. **Powierzenie (EntPart):**
     - „?” albo Notes „komponent powierzany (do potwierdzenia)” → EntPart „?” (dostawca bez zmian),
     - ☑ albo Notes „komponent powierzany” → EntPart ☑ i **Dostawca = „Klient”**; jeśli był już inny dostawca → „? Klient | <dostawca>”,
     - inny tekst w EntPart → jak ☑, a dodatkowo tekst trafia do „Nazwa”, jeśli była pusta.
  9. **MPN zatwierdzone:** lista MPN z kolumn O… arkusza „MPN zatwierdzone” (bez powtórzeń) trafia jako tekst „MPN1, MPN2, …” do kolumny „MPN zatwierdzone”. **MPN niedozwolone nie są importowane.**
  10. **Wyróżnienie zmian:** przed importem kasowane jest pogrubienie w B:O; każda komórka zmieniona w tym imporcie jest **pogrubiana** (od drugiego importu). Wiersz z co najmniej jedną zmianą dostaje w B („Data Aktualizacji”) bieżącą datę i godzinę. Potem kolumna B jest kolorowana **gradientem**: wszystkie różne daty aktualizacji (różnica ≤ 59 s = ta sama grupa) są sortowane, najstarsza — biała, najnowsza — zielona, pośrednie proporcjonalnie.
  11. „Opis z Pozycji” = nazwa arkusza ZK (dla fuse — fragment FuseDes i numer ZK).
  12. **Komunikaty Opiekuna Projektu:** niepuste komórki START!J16:J26 z iPart (od najnowszej) są pokazywane w okienku.
  13. H1 = login zakupowca (jeśli pusty), H4 = Klient, H5 = projekt z iPart.
  14. **Zmiana nazwy pliku** na `<nazwa arkusza ZK>_<projekt>_sPart.xlsm`; poprzedni plik do podfolderu `z_Nieaktualne` (szablon `sPart.xlsm` / `sPart_v3.xlsm` jest usuwany).
  15. C1 = data importu; następnie **pobranie wydań z cPart** (2.5).
- **Wynik:** zaktualizowany rdzeń BOM, EntPart, dostawca „Klient”, MPN zatwierdzone, daty i wyróżnienia, dane z cPart, nowa nazwa pliku.
- **Moduł:** `sZK_p1_Import_iPart.AktualizujDaneZZewnetrznegoBOM`; gradient: `Public_Function.KolorujAktualizacjePoCzasie`; nazwa: `Public_Function.NowaNazwaPliku`.
- **[zgodne z wywiadem]** sPart importuje iPart lub fusePart; wyróżnianie zmian gradientem i pogrubieniem; zmiany Klienta, kolejność linii jako klucz.
- **[różnica względem wywiadu]** Gradient nie zależy od liczby dni, tylko od **kolejności** różnych dat aktualizacji (najnowsza zawsze najciemniejsza, nawet sprzed miesiąca). Pogrubienie pokazuje tylko zmiany z **ostatniego** importu. Format `[#, #, #] [#, #]` nie występuje — kolumna „Historia Zmian” kopiuje tekst z iPart (`ID_Zmian[…]`).
- **[różnica względem wywiadu]** Powierzenie nie jest w sPart w pełni ręczne: znacznik ☑ z iPart **automatycznie** ustawia dostawcę „Klient”. Ręczne pozostaje: podział wiersza przy częściowym powierzeniu i ZD z ceną 0 zł (brak kodu).
- **[nowe — nieomawiane]** Blokada importu niezatwierdzonego iPart (N1:N4) — to techniczna wersja zasady „zakupowiec sprawdza, czy iPart jest gotowy”. Komunikaty z pola komentarzy Opiekuna Projektu są pokazywane zakupowcowi przy każdym imporcie. Ochrona Mounting dla zmian jednorazowych.
- **[nowe — nieomawiane]** Zakupowiec **nie widzi w sPart listy MPN niedozwolonych** — dowiaduje się o zakazie dopiero przez status (2.6).

### 2.4. Ilości, zapas i magazyn (formuły)

Ta część nie jest kodem VBA — to formuły w wierszach arkusza ZK (wzór w wierszu 11).

| Kolumna | Nagłówek | Formuła (opis) |
|---|---|---|
| P | Obudowa | Case (H); „THT” gdy Mounting = THT; „PCB” gdy PCB; „obudowa” gdy Mounting puste |
| AA | PN (magazyn) | **wpisywany ręcznie** przez zakupowca — numer, po którym szukany jest stan |
| Z | Symbol | symbol magazynowy z arkusza Magazyn (po AA) |
| AB | Ilość magazyn | ilość dostępna z arkusza Magazyn **+ ilość z listy magazynu zablokowanego** (osobny plik, łącze zewnętrzne) |
| AC, AD | W Kompletacji, Ilość na ZD | z arkusza Magazyn — tylko do wglądu |
| AE | — | „tak – <ilość>”, gdy komponent jest na liście zablokowanych |
| AH | Ilość z BOM | Quantity (F) × ilość pakietów (H6) |
| AI | Zapas szt | patrz niżej |
| AF | Brakująca Ilość | AB − (AH + AI); wartość ujemna = brak |
| AJ | Ilość (do zamówienia) | |AF|, gdy AF < 0, inaczej 0 |
| AN, AO | Przeliczenie na PLN, Wartość PLN | cena PLN albo cena USD/EUR × kurs; wartość = cena × AJ |

**Zapas (AI):** zero dla „obudowa”, PCB, pustej ilości; w pozostałych przypadkach:
`zaokrąglenie w górę( zapas_stały + AH × mnożnik )`, gdzie wiersz tabeli wybierany jest po **obudowie (P)**, a para kolumn (zapas, mnożnik) po **największym progu ilości ≤ AH** (progi 0, 10, 50, 100, 250, 500, 1000). Brak obudowy w tabeli → „bł.” (podświetlane na czerwono), a brakująca ilość nie policzy się.

- **Wejście tabeli zapasu:** łącze zewnętrzne do pliku **„Obudowy.xlsx”** (nazwy `obudowav3`, `ilośćv3`, `obudowa_zapasv3`). Te same nazwy istnieją w **scrapPart.xlsx** w arkuszu `supplyScrap`.
- **Arkusz Magazyn:** wklejany ręcznie (kolumny: Symbol, Nazwa PL (MPN), Opis, Montaż, Ilość dostępna, W kompletacjach, Ilość ZD, Symbol). **Nie ma makra importu stanu magazynu** — przycisk „WKLEJ” dotyczy wyceny (2.8).
- **[zgodne z wywiadem]** Ilość z BOM = ilość pakietów × Quantity; brakująca = BOM + zapas − magazyn; brak rezerwacji (kolumny „W Kompletacji” i „Ilość na ZD” nie są odejmowane).
- **[różnica względem wywiadu]** Zapas liczony jest z pliku „Obudowy.xlsx”, a nie bezpośrednio z „scrapPart.xlsx”; arkusz w scrapPart nazywa się `supplyScrap`, nie `supplyPart`. [niepewne] Czy „Obudowy.xlsx” to dawna nazwa scrapPart (czy zmiany w scrapPart docierają do sPart)?
- **[nowe — nieomawiane]** Stan magazynu obejmuje opakowania **zablokowane** (osobna lista magazynowa w innym pliku). Dopasowanie do magazynu odbywa się po PN wpisanym ręcznie w AA, a nie automatycznie po Part Number/MPN z BOM.
- **[nowe — nieomawiane]** Linie niepotwierdzone (Confirmed = false) są tylko **podświetlane** (formatowanie warunkowe); formuły liczą dla nich ilość do zamówienia tak samo. Linie DNP / „jednorazowo DNP” są wyszarzane. Pomijanie niepotwierdzonych w zakupach to umowa, nie blokada.

### 2.5. Wymiana danych z cPart

**sPart → cPart („Utwórz/Aktualizuj cPart” oraz przy zamknięciu)**

- **Wejście:** arkusz ZK (wiersze od 11, nagłówki w wierszu 10), ścieżka AP2, H5 (nazwa projektu — wymagana).
- **Reguła:**
  1. Brak H5 → komunikat „uzupełnij nazwę projektu zgodnie z TS ProduMan” i przerwanie.
  2. **Gdzie zapisać:** AP2, jeśli plik istnieje. Inaczej domyślnie folder kompletacji na dysku sieciowym, podfolder Klienta (nazwa ostatniego folderu ścieżki sPart), nazwa `<nazwa sPart>_cPart.xlsm`. Gdy taki plik już istnieje — pytanie „zintegrować?”; „Nie” tworzy osobny plik `…_AutoGen(_01…).xlsm` z prośbą o kontakt z autorem. Gdy brak folderu Klienta — okno „Zapisz jako”.
  3. Nowy plik powstaje jako kopia **wzorca cPart_v3** z folderu wzorców kompletacji.
  4. W cPart arkusz `<nazwa arkusza ZK>_PP1` — tworzony z ukrytego START cPart, jeśli go nie ma. **Aktualizowany jest zawsze tylko arkusz PP1.**
  5. **Mapowanie po nagłówkach** wiersza 10 (sPart → cPart, ta sama nazwa nagłówka) plus „Uwagi” → „Zakupy”. Przy pierwszym eksporcie mapowane są wszystkie kolumny; gdy cPart ma już R8 (data aktualizacji), tylko kolumny cPart od **P** w prawo (sekcje Zakupy i dalej) — rdzeń BOM cPart pobiera sam z iPart.
  6. **Ceny nie przechodzą**, bo w cPart nie ma kolumn o nagłówkach cenowych.
  7. **Wyrównanie wierszy:** numer linii („Lp”) — z kolumny A, a dla wierszy dodatkowych bez numeru (np. drugi dostawca tej samej pozycji) `<numer>|<kolejny>`. Jeśli kolumna AH cPart (RowID) w tym wierszu ma inną wartość, w cPart **wstawiany jest nowy wiersz**.
  8. Data w B: zostaje nowsza z dwóch plików. „Generic”: gdy status MPN zawiera „generic”, a w cPart „Nazwa” jest już wpisana (przez magazyn) — nie jest nadpisywana.
  9. Nagłówek cPart: A2 (ścieżka iPart), C1, H1:H5, H6 (tylko gdy puste), J1:J6; cPart!R8 i sPart!AR1 = bieżąca data.
  10. cPart zapisywany i zamykany; AP2 = ścieżka cPart (poza AutoGen).
- **Kiedy:** przyciskiem; oraz **przy zamknięciu sPart** dla każdego arkusza ZK w trybie pracy, który ma wpisaną ścieżkę w AP2.
- **Moduł:** `sZK_p3_Eksport.UtworzLubAktualizujKompletacje`; `Ten_skoroszyt.Workbook_BeforeClose`.
- **[zgodne z wywiadem]** cPart generuje zakupowiec, bez warunków (także z „pustego” sPart); sPart wysyła dane do cPart przy zamknięciu; ceny nie trafiają do cPart.
- **[nowe — nieomawiane]** Automatyczna aktualizacja przy zamknięciu działa dopiero **po pierwszym** utworzeniu cPart przyciskiem (wymaga AP2). Aktualizowana jest tylko partia PP1.

**cPart → sPart (przy imporcie BOM)**

- **Reguła:** cPart otwierany tylko do odczytu. Dla arkuszy `<ZK>_PP<n>`: kolumna „Wydano” → sPart „Wydano z Magazynu” jako tekst `PP2: 120 | PP1: 300` (kolor z najwyższej partii), kolumna „Magazyn” → sPart „Uwagi Magazyn” jako `PP2: … | PP1: …`. Wiersze dopasowane po numerze wiersza.
- **Moduł:** `sZK_p6_Import_cPart.DaneZCPart`.
- **[nowe — nieomawiane]** Przepływ wsteczny cPart → sPart: zakupowiec widzi w sPart, ile wydano w każdej partii i uwagi magazynu.

### 2.6. Status MPN i powrót zamienników do iPart („Porównaj Status MPN” oraz przy zamknięciu)

- **Wejście:** kolumna „Nazwa” (MPN faktycznie zamawiany, AQ) i „Status MPN” (AR); plik źródłowy z A2 (iPart lub fusePart): arkusze „MPN zatwierdzone”, „MPN niedozwolone”, logi `MPNz_log`, `MPNn_log`.
- **Reguła (zamierzona):**
  1. Dla każdego wiersza z MPN szukany jest numer pozycji (kolumna A tego wiersza albo najbliższy powyżej) i odpowiedni wiersz w arkuszach MPN źródła (numer + 10).
  2. **Ręczny status** (wybrany z listy, bez formuły) → MPN dopisywany do „MPN zatwierdzone” albo „MPN niedozwolone” w iPart, z wpisem w logu (`customer | <login> [data]`, `parameters | …`, `<login> [data]` itd.); w AR zostaje formuła pilnująca, że status dotyczy tego MPN.
  3. **Status pusty / „Do Weryfikacji” / z formułą** → status ustalany automatycznie:
     - MPN = Part Number pozycji → „Zgodny z BOM”,
     - MPN na liście zatwierdzonych → status z logu: „BOM…” → Zgodny z BOM; „parameters | BOM…” → Zgodny z parametrami; „parameters | inne” → Generic; „customer | …” → Zatwierdzony: przez Klienta; pozostałe → Zatwierdzony przez TS,
     - MPN na liście niedozwolonych → „Niedozwolony”,
     - inaczej → „Do Weryfikacji”.
     Status jest formułą `JEŻELI(AQ = MPN; status; "Do Weryfikacji")` — zmiana MPN w AQ cofa status na „Do Weryfikacji”.
  4. Plik iPart/fusePart jest **zapisywany** i zamykany.
- **Lista wyboru statusu (AR):** Zgodny z BOM, Zgodny z parametrami, Generic, Zatwierdzony przez Klienta, Zatwierdzony przez TS, **Jednorazowo Dopuszczony**, Do Weryfikacji, Niedozwolony. Kolory ustawia formatowanie warunkowe.
- **Kiedy:** przyciskiem oraz **przy każdym zamknięciu** sPart dla arkuszy w trybie pracy.
- **Moduł:** `sZK_p2_StatusyMPN.AktualizujStatusyMPN`.
- **[różnica względem wywiadu] — prawdopodobny błąd: ręczne decyzje zakupowca nie wracają do iPart.** Status jest najpierw zamieniany na małe litery (`NormalizeStatus`), a potem porównywany z nazwami pisanymi wielką literą („Zgodny z BOM”, „Zatwierdzony przez Klienta”…). Porównanie w VBA rozróżnia wielkość liter, więc warunek „ręczny status” **nigdy nie jest spełniony**. Ręcznie wybrany status nie trafia ani do iPart, ani do logu; zostaje tylko w sPart (punkt 3 też go nie zmienia). Dodatkowo w tabeli przekierowań są niespójne etykiety („zZgodny z parametrami”, „zatwierdzony: Generic”), a „Jednorazowo Dopuszczony” nie ma żadnego przekierowania. Kod odpowiadający za zamienniki od zakupowca **istnieje, ale nie działa**.
- **[różnica względem wywiadu]** Zamiennik jednorazowy: na liście statusów jest „Jednorazowo Dopuszczony” (nie „zamiennik jednorazowy”). Komentarz w kodzie zapowiada prefiks logu `oneoff`, ale funkcja odczytu go nie obsługuje (wynik: „Zatwierdzony przez TS”).
- **[nowe — nieomawiane]** Etykiety statusów pisane przez kod („Zatwierdzony: przez Klienta”) różnią się od etykiet z listy i formatowania („Zatwierdzony przez Klienta”) — te wiersze nie dostają koloru.
- **[nowe — nieomawiane]** Przy łączonym BOM zamienniki trafiałyby do **fusePart**, nie do źródłowych iPart.

### 2.7. Zamknięcie pliku

Przy zamknięciu (`Workbook_BeforeClose`), dla każdego arkusza ZK w **trybie pracy**:

1. statusy MPN (2.6) — otwiera i **zapisuje** iPart/fusePart,
2. jeśli AP2 jest wypełnione — aktualizacja cPart (2.5), zapis cPart,
3. F1 = „Tryb Podglądu”.

Potem wszystkie arkusze ZK → podgląd, `arch_…` → „Zarchiwizowano”, START zabezpieczony, usuwana tymczasowa kopia mastera.

- **[zgodne z wywiadem]** „sPart wysyła dane do cPart przy zamknięciu”.
- **[nowe — nieomawiane]** Kroki 1–2 zapisują **inne pliki**, zanim użytkownik zdecyduje, czy zapisać sam sPart. Jeśli zamknie sPart bez zapisu, iPart i cPart zawierają już dane z niezapisanego stanu.

### 2.8. Wycena i kursy walut

- **WKLEJ:** użytkownik wybiera plik wyceny; kopiowane są wartości z **stałych zakresów** tego pliku: AK19:AN… → R11 (cena PLN/szt., wartość, dostawca, wyceniony PN), AP19:AQ… → V11 (LT, uwagi), AE19… → X11 (zapas z wyceny), AL3:AL7 → U4 (podsumowanie wyceny). Kopiowanie zaczyna się od pierwszego wiersza danych i opiera na kolejności linii.
- **ODŚWIEŻ:** `RefreshAll` (zapytanie Power Query „Kursy walut” pobierające tabelę kursów banku) i data w AM6. Kurs USD i EUR to kolumna „Sprzedaż” tej tabeli (AL8, AM8).
- **WYCZYŚĆ:** zeruje pomocniczy przelicznik walut (AK3, AL4, AM5).
- **Moduł:** `K_OtworzPlik`, `K_Wyczysc`.
- **[zgodne z wywiadem]** Wycena przenoszona 1:1 dzięki stałej kolejności linii.
- **[niepewne]** Z jakiego pliku pochodzi wycena (dane od wiersza 19, kolumny AE–AQ)? To nie jest układ TS_KOSZTORYSANT ani iPart.
- **[nowe — nieomawiane]** „ODŚWIEŻ” i „WYCZYŚĆ” sprawdzają zmienną `PodgladAktywny`, której nigdzie nie zdefiniowano. Moduły nie mają `Option Explicit`, więc VBA traktuje ją jako pustą zmienną — **blokada podglądu w tych przyciskach nigdy nie działa**. „WKLEJ” nie sprawdza trybu wcale.

### 2.9. Podsumowanie kosztów i nadwyżka (formuły)

| Kolumna | Nagłówek | Formuła (opis) |
|---|---|---|
| BA | Ilość z BOM | = AH |
| BB | Nadwyżka | gdy zamawiamy (AJ > 0): AJ + AB − AH; inaczej AB − AH. Czyli **(zamówione + magazyn) − ilość z BOM** — obejmuje zapas i nadmiar wynikający z opakowań/MOQ |
| BC | Koszt nadwyżki PLN | cena PLN × BB |
| BF, BG | Różnica cenowa wyceny z magazynem / zakupami | „:)” gdy cena z wyceny ≥ cena magazynowa / zakupowa, inaczej różnica |
| BH | Koszty z mag | cena z **cennika** (osobny plik, łącze zewnętrzne) po symbolu magazynowym |
| BI, BJ, BK, BL | Wartość mag, koszt z zapasem, koszty zakupu, suma | koszt komponentów z magazynu i z zakupu |
| BO…CN | rozliczenie projektów w fusePart | do 13 ZK: ilość dla danego ZK odczytywana z tekstu FuseDes, koszt linii dzielony proporcjonalnie; BO8 ostrzega, gdy suma się nie zgadza („czy linia BOM nie została rozbita na kilka dostaw”) |

Formatowanie warunkowe podświetla nadwyżkę, gdy jest większa niż zapas (zamówiono więcej, niż wymaga BOM z zapasem).

- **„Zablokuj wartości [wyłączenie formuł]”** — zamienia formuły P1:BL na wartości i wpisuje datę do BC1; drugi raz nie zadziała. Chroni podsumowanie przed zmianą, gdy później zmieni się magazyn, kursy lub cennik.
- **Moduł:** `sZK_p5_Archiwum.WylaczFormuly`.
- **[zgodne z wywiadem]** Sekcja kierownika logistyki; tryb podsumowania (BA:BL).
- **[nowe — nieomawiane]** Wycena stanu magazynowego z cennika; zamrażanie wyników; podział kosztów między ZK przy fusePart.
- Odpowiedź na pytanie otwarte z wywiadu: „Nadwyżka” = ilość ponad BOM (zamówione + magazyn − BOM), łącznie z zapasem.

### 2.10. Wersje szablonu (MASTER)

- **Kontrola przy wejściu w tryb pracy** (`A_ForwardUpdate`): z START!D1/E1 budowana jest nazwa „sPart — revX.Y — znacznik.xlsm”. Gdy nie ma jej w folderze MASTER, po zgodzie: stary plik zapisany do `z_NIEAKTUALNE`, z najnowszego mastera brany jest **tylko arkusz START**, a wszystkie pozostałe arkusze (ZK, Magazyn, Kursy walut…) kopiowane są ze starego pliku. Wybrany arkusz dostaje F1 = „Tryb Aktualizacji” i komunikat „Zaktualizuj BOM”. Odmowa zamyka plik bez zapisu.
- **Wydanie szablonu** (`A_MasterUpdate`, przy zapisie): tylko w folderze MASTER i tylko dla loginu autora; nowa rev i znacznik czasu, zapis pod nową nazwą, kopia jako `sPart_v3.xlsm` w trzech folderach (w tym testowym) — to jest plik startowy dla nowych sPart.
- **Blokada zapisu w MASTER** dla innych loginów.
- **[nowe — nieomawiane]** Inaczej niż w iPart: nowy szablon zmienia **START**, więc nowy układ dostają tylko **nowe** arkusze ZK. Istniejące arkusze ZK zachowują stary układ, a zmiany układu wprowadza kod importu (np. wstawienie kolumn „W Kompletacji” i „Ilość na ZD”). Stan „Tryb Aktualizacji” nie jest nigdzie dalej sprawdzany.

## 3. Współzależności z innymi plikami

| Kierunek | Plik | Arkusz / zakres / kolumny | Kiedy | Moduł / procedura | Co przenosi |
|---|---|---|---|---|---|
| czyta | iPart | BOM: N1:N4 (zatwierdzenie), wiersz 10 (nagłówki), wiersze danych; „MPN zatwierdzone” O…; START!J16:J26 | otwarcie w trybie pracy; przycisk „Pobierz/Aktualizuj BOM” | `sZK_p1_Import_iPart` | rdzeń BOM po nagłówkach, EntPart/Notes → EntPart i dostawca „Klient”, lista MPN zatwierdzonych, Klient/projekt, komunikaty Opiekuna Projektu |
| czyta | fusePart | fuseBOM (jak wyżej, N3 Klient, N4 nazwa fuse), „MPN zatwierdzone” | jak wyżej (gdy A2 wskazuje fusePart) | `sZK_p1_Import_iPart` | połączony BOM, FuseID/FuseDes/SumaQty, MPN zatwierdzone |
| zapisuje | iPart / fusePart | „MPN zatwierdzone”, „MPN niedozwolone”, `MPNz_log`, `MPNn_log` (wiersz = nr pozycji + 10) | przycisk „Porównaj Status MPN”; **każde zamknięcie** w trybie pracy | `sZK_p2_StatusyMPN` | zamierzenie: MPN wybrany ręcznie przez zakupowca + log. **Faktycznie: nic nie dopisuje (błąd porównania), ale zapisuje plik** |
| czyta | iPart / fusePart | „MPN zatwierdzone”, „MPN niedozwolone”, logi | jak wyżej | `sZK_p2_StatusyMPN` | status MPN (Zgodny z BOM / z parametrami / Generic / przez Klienta / TS / Niedozwolony) |
| tworzy | cPart | kopia wzorca cPart_v3 → folder kompletacji Klienta | przycisk „Utwórz/Aktualizuj cPart” (gdy brak pliku) | `sZK_p3_Eksport` | nowy plik cPart, arkusz `<ZK>_PP1` |
| zapisuje | cPart | arkusz `<ZK>_PP1`: kolumny o tych samych nagłówkach (od P po pierwszym eksporcie), „Uwagi” → „Zakupy”; A2, C1, H1:H6, J1:J6, R8 | przycisk; **każde zamknięcie** sPart w trybie pracy (gdy AP2 wypełnione) | `sZK_p3_Eksport` | dane zakupowe (ilość, dostawca, nazwa/MPN, status MPN, tracking, ZD, opis), daty aktualizacji, nagłówek ZK, ścieżka iPart; bez cen |
| czyta | cPart | arkusze `<ZK>_PP<n>`: „Wydano”, „Magazyn” | przy imporcie BOM | `sZK_p6_Import_cPart` | ilości wydane i uwagi magazynu w podziale na partie |
| czyta (łącze) | plik zapasów „Obudowy.xlsx” (odpowiednik scrapPart/supplyScrap) | nazwy `obudowav3`, `ilośćv3`, `obudowa_zapasv3` | przy przeliczeniu formuł | formuła AI | zapas stały i mnożnik wg obudowy i progu ilości |
| czyta (ręcznie wklejony) | eksport stanu magazynu (xlsx/csv) | arkusz Magazyn A:H | ręczne wklejenie | — (formuły Z, AB:AD, AX) | ilość dostępna, w kompletacjach, na ZD, symbol |
| czyta (łącze) | lista magazynowa opakowań zablokowanych | nazwa `MAGAZYN_ZABLOKOWANY` | przy przeliczeniu formuł | formuły AB, AE | ilość zablokowana (dodawana do stanu) |
| czyta (łącze) | cennik magazynowy | Sheet1!B:R | przy przeliczeniu formuł | formuła BH | cena jednostkowa komponentu z magazynu |
| czyta | plik wyceny (Excel) | AK19:AN…, AP19:AQ…, AE19…, AL3:AL7 | przycisk „WKLEJ” | `K_OtworzPlik.Wklej` | ceny, dostawca, wyceniony PN, LT, uwagi, zapas z wyceny, podsumowanie |
| czyta (Power Query) | strona kursów walut banku | arkusz „Kursy walut” | przycisk „ODŚWIEŻ” | `K_OtworzPlik.Odswiezanie` | kursy kupna/sprzedaży |
| czyta / zapisuje | szablon sPart w folderze MASTER | START | wejście w tryb pracy; zapis w MASTER | `A_ForwardUpdate`, `A_MasterUpdate` | nowe makra i nowy START; wydanie szablonu i kopie `sPart_v3.xlsm` |
| brak | dataPart, dPart, entPart (plik), scrapPart.xlsx (bezpośrednio) | — | — | — | sPart nie odwołuje się do nich w kodzie |

## 4. Reguły biznesowe zaszyte w kodzie i formułach

1. **Nazwa arkusza ZK:** `ZK_<numer(y)>_<rok 4 cyfry>`, tylko litery, cyfry, `_`.
2. **Nazwa pliku:** `<arkusz ZK>_<projekt>_sPart.xlsm`; projekt z nazwy pliku iPart (bez numeru ZK i „_iPart”).
3. **Warunek importu:** iPart/fusePart zatwierdzony — N1:N4 (data, zatwierdzający, Klient, projekt) wszystkie wypełnione.
4. **Dopasowanie linii:** po pozycji wiersza (numer = wiersz − 10), nie po identyfikatorze.
5. **Mapowanie kolumn:** po identycznym nagłówku w wierszu 10; Footprint → Case dla starych iPart.
6. **Ochrona Mounting przed nadpisaniem:** „jednorazowo DNP”, „zamiennik SMT”, „zamiennik THT” i wartości spoza listy SMT/THT/PressFit/Pin-In-Paste/MECH.
7. **Powierzenie:** ☑ lub Notes „komponent powierzany” → Dostawca „Klient” (konflikt: „? Klient | <dostawca>”); „?” → do potwierdzenia, bez zmiany dostawcy.
8. **Ilość z BOM** = Quantity × ilość pakietów (H6); dla fuse H6 = 1.
9. **Zapas** = ⌈zapas_stały + ilość × mnożnik⌉ wg obudowy i progu (0/10/50/100/250/500/1000); 0 dla PCB i pustej obudowy; THT jako osobna „obudowa”.
10. **Stan** = dostępne + zablokowane; **do zamówienia** = max(0, BOM + zapas − stan); brak rezerwacji.
11. **Nadwyżka** = (zamówione + stan) − BOM.
12. **Ceny:** PLN, albo USD/EUR × kurs sprzedaży banku; cena magazynowa z cennika.
13. **Status MPN:** priorytet Zgodny z BOM → lista zatwierdzonych (rodzaj z logu) → lista niedozwolonych → „Do Weryfikacji”. Status przypięty formułą do konkretnego MPN.
14. **Wiersze dodatkowe pozycji** (np. część od Klienta, część kupowana) — wiersz bez numeru w kolumnie A należy do najbliższej pozycji powyżej (`<numer>|<kolejny>` w cPart).
15. **Wyróżnianie zmian:** pogrubienie = zmiana w ostatnim imporcie; kolor B = ranking daty aktualizacji (biały → zielony).
16. **Do cPart trafiają tylko kolumny o nagłówkach występujących w cPart** — tak chronione są ceny.
17. **Rozliczenie fuse:** koszt linii dzielony między ZK proporcjonalnie do ilości z FuseDes.

## 5. Odpowiedzi na otwarte pytania z wywiadu

| Pytanie z wywiadu | Co mówi kod |
|---|---|
| Jak zamiennik od zakupowca wraca do iPart? | Kod istnieje (`AktualizujStatusyMPN`, przy zamknięciu), ale przez błąd porównania wielkości liter **ręczne statusy nie są dopisywane**. Dziś zamiennik nie wraca automatycznie. |
| Co oznacza „Nadwyżka”? | (zamówione + magazyn) − ilość z BOM; zawiera zapas i nadmiar z opakowań/MOQ. |
| Jak wyróżniane są zmiany w sPart? | Pogrubienie komórek zmienionych w ostatnim imporcie (B:O) + kolor kolumny B od białego (najstarsza aktualizacja) do zielonego (najnowsza), wg kolejności dat, nie liczby dni. Format `[#,#] [#]` nie występuje. |
| Jak działa tryb podsumowania? | Wybór w oknie przy otwarciu; wszystkie arkusze ZK → „Tryb Podsumowania”; makra zakupowe są zablokowane; intencja: edycja tylko BA:BL. Przez sposób ochrony (2.1) ręcznie można zmieniać także inne komórki. |
| Czy pomijanie linii niepotwierdzonych w zakupach to blokada? | Nie — tylko podświetlenie. Formuły liczą ilość do zamówienia także dla nich. |
| Skąd sPart bierze zapasy (supplyPart)? | Z łącza do pliku „Obudowy.xlsx” (nazwy `…v3`), odpowiadającego arkuszowi `supplyScrap` w scrapPart. |
| „Zamiennik jednorazowy”? | Status „Jednorazowo Dopuszczony” na liście wyboru (bez zapisu do iPart) oraz wartości Mounting „jednorazowo DNP”, „zamiennik SMT/THT” chronione przed nadpisaniem przy imporcie. |

## 6. Ryzyka i słabe punkty

1. **Zamienniki od zakupowca nie wracają do iPart** (2.6) — porównanie statusu po zamianie na małe litery z nazwami pisanymi wielką literą; niespójne etykiety w tabeli przekierowań; brak obsługi „Jednorazowo Dopuszczony” i `oneoff`.
2. **Ochrona w trybie podglądu i podsumowania nie blokuje komórek** — `ProtectALL` odblokowuje cały zakres A1:CZ10009 przed włączeniem ochrony.
3. **Zapis innych plików przy zamknięciu sPart** — iPart/fusePart (statusy) i cPart są zapisywane, zanim użytkownik zdecyduje o zapisie sPart; iPart jest zapisywany przy każdym zamknięciu, nawet bez zmian. Gdy iPart jest otwarty u innej osoby, otwiera się tylko do odczytu, a błąd zapisu jest wyciszany.
4. **Dopasowanie po pozycji wiersza** (import z iPart, eksport do cPart, pobieranie wydań z cPart). W cPart wiersze są tylko **wstawiane**, nigdy usuwane — zmiana kolejności lub usunięcie wiersza dodatkowego w sPart rozjedzie wiersze.
5. **Tylko PP1 aktualizowany z sPart** — kolejne partie w cPart nie dostają danych zakupowych. [niepewne] Może cPart kopiuje je sam (do sprawdzenia w analizie cPart).
6. **Zapas z pliku „Obudowy.xlsx”**, nie z „scrapPart.xlsx”; w scrapPart te same nazwy są zdefiniowane dwa razy (dla `supplyScrap` i `entScrap`), a stare nazwy (`…v2`) są uszkodzone (#REF!). Ryzyko, że zmiana reguł zapasu w scrapPart nie trafia do sPart.
7. **Obudowa spoza tabeli zapasu** → „bł.”, ilość do zamówienia nie liczy się dla tej linii.
8. **Zakupowiec nie widzi listy MPN niedozwolonych** — tylko status po wpisaniu MPN (sprawdzany przy zamknięciu lub przyciskiem). Gdy MPN jest na obu listach, wygrywa „zatwierdzony”.
9. **Dopasowanie do magazynu po ręcznie wpisanym PN** (AA) — literówka = stan 0 i zamówienie całości.
10. **Zmiana nazwy pliku według arkusza ZK** — przy kilku arkuszach ZK nazwa pliku zmienia się zależnie od tego, który arkusz zaimportowano ostatnio; poprzednia wersja trafia do `z_Nieaktualne`.
11. **Mounting „Bezpośrednio do Klienta”** nigdy nie jest aktualizowany z iPart — porównanie po zamianie na wielkie litery z tekstem pisanym małymi literami (wartość traktowana jako „niestandardowa”).
12. **Blokada podglądu w „ODŚWIEŻ” i „WYCZYŚĆ” nie działa** (niezdefiniowana zmienna `PodgladAktywny`); „WKLEJ” nie sprawdza trybu.
13. **Ceny chronione tylko nazwami nagłówków** — dodanie do szablonu cPart kolumny o nagłówku cenowym z sPart przeniesie ceny na magazyn.
14. **Stałe zakresy wyceny** (wiersz 19, kolumny AE–AQ) — inny układ pliku wyceny = przesunięte dane bez komunikatu.
15. **Twarde ścieżki sieciowe, login autora, adres e-mail działu i hasło** wpisane w kod (hasło także jako tekst bezpośrednio w module statusów, poza stałą). Klient odczytywany z nazwy folderu.
16. **Wyciszanie błędów** przy imporcie BOM w trakcie otwierania i przy eksporcie do cPart przy zamknięciu — nieudana aktualizacja może przejść niezauważona.
17. **Rozliczenie fuse** zależy od formatu tekstu FuseDes (`ZK_…: ilość`); inny zapis = 0 i ostrzeżenie w BO8.
18. **Procedura `Archiwizuj` jest pusta**; arkusze `arch_…` powstają ręcznie. Stan „Tryb Aktualizacji” nie jest nigdzie obsługiwany.

## 7. Pytania do potwierdzenia przez autora

| Pytanie | Moduł |
|---|---|
| Czy ręczny status MPN (np. „Zatwierdzony przez Klienta”) kiedykolwiek trafił do iPart? Kod wskazuje, że nie — czy poprawiamy w Excelu, czy tylko w aplikacji? | `sZK_p2_StatusyMPN` |
| Czy „Jednorazowo Dopuszczony” ma zostać tylko w sPart (zamiennik jednorazowy), a pozostałe ręczne statusy mają trafiać do iPart? | `sZK_p2_StatusyMPN` |
| Czy tryb podglądu ma blokować ręczną edycję komórek (dziś blokuje tylko makra)? | `Public_Function.ProtectALL` |
| Czy „Obudowy.xlsx” to ten sam plik co scrapPart (supplyScrap), czy kopia? Który jest aktualny? | formuła AI |
| Z jakiego pliku pochodzi wycena wklejana przyciskiem „WKLEJ” (dane od wiersza 19)? | `K_OtworzPlik.Wklej` |
| Czy jeden plik sPart z kilkoma arkuszami ZK to typowa sytuacja? Jak ma się wtedy nazywać plik? | `NowaNazwaPliku` |
| Czy kolejne partie (PP2, PP3…) w cPart powinny dostawać dane zakupowe z sPart, czy tylko PP1? | `sZK_p3_Eksport` |
| Czy zakupowiec powinien widzieć listę MPN niedozwolonych w sPart (dziś nie jest importowana)? | `sZK_p1_Import_iPart` |
| Czy PN do magazynu (AA) wpisuje się zawsze ręcznie? Czy mógłby być domyślnie MPN z „Nazwa”/Part Number? | formuły Z, AB |
| Czy dodawanie stanu zablokowanego do dostępnego jest zamierzone? | formuła AB |
| Czy zapis iPart i cPart przy zamknięciu sPart (także bez zapisu sPart) jest zamierzony? | `Workbook_BeforeClose` |
| Do czego miał służyć „Tryb Aktualizacji” i procedura `Archiwizuj`? | `A_ForwardUpdate`, `sZK_p5_Archiwum` |
| Czy gradient ma pokazywać kolejność aktualizacji (jak dziś), czy wiek zmiany w dniach (jak w wywiadzie)? | `KolorujAktualizacjePoCzasie` |
| Czy ZD z ceną 0 zł dla dostaw Klienta powinno powstawać automatycznie przy dostawcy „Klient”? | (brak kodu) |
