---
status: roboczy
zakres: pilotaż — test procesu comparator + iPart w aplikacji (wersja 0.3.2)
przegląd: 2026-11-27
---

# Scenariusz testu: od BOM Klienta do zatwierdzonej wersji BOM

Dla: Opiekuna Projektu, który pierwszy raz przechodzi proces w aplikacji iPart. Czas: ok. 1–2 godziny.

## Zanim zaczniesz

- **To jest test.** Baza jest testowa, a wybór konta jest bez hasła. Nie wpisuj danych, które muszą zostać tylko w firmie (np. ceny).
- **Przygotuj pliki jednego pakietu**, który znasz: **BOM Klienta (.xlsx)** i **P&P (.txt albo .xlsx)**. Najlepiej taki, przy którym w Excelu były pytania do Klienta.
- **Uruchom program:** `iPart.App.exe` z folderu wersji 0.3.2 na udziale. Instalacja nie jest potrzebna.
- **Notuj uwagi** na bieżąco (punkt „Uwagi” na końcu): co było niejasne, czego brakowało, co trwało za długo.

## Kroki

1. **Wybierz konto** „Opiekun Projektu (test)” i kliknij **Dalej**.
2. **Import BOM/P&P** (menu) — krok 1: Import BOM. U góry widać postęp: 1 Import BOM → 2 Import P&P → 3 Comparator → 4 Integracja Danych → 5 Zatwierdzenie i wersje. Przyciski przejścia dalej są zawsze na dole po prawej; powrót do poprzedniego kroku pyta o potwierdzenie (zmiany z bieżącego kroku przepadają).
   - **1. Klient:** wybierz Klienta z listy (nowego Klienta dodasz na stronie **Pakiet Elektroniczny**).
   - **2. Pakiet Elektroniczny:** wybierz pakiet tego Klienta (nowy pakiet tworzysz wcześniej na stronie **Pakiet Elektroniczny**; rewizja nieobowiązkowa).
   - **3. Plik BOM:** **Wybierz plik BOM** → arkusz i wiersz nagłówków → przypisz kolumny (albo **Podpowiedz po nazwach nagłówków**). Kolumnę porównywaną z P&P przypisz do **Value**. W pierwszej kolumnie **Comparator** zaznacz kolumny, które chcesz porównać z P&P (Value, Footprint, Description; PartNumber — jeśli chcesz, porównywany jako cały tekst). Part TS na tym etapie nie przypisujemy.
   - **Podgląd** → sprawdź „Problemy” i „Podgląd linii” → **Dalej: Import P&P**.
3. **Import P&P** — krok 2: **Wybierz plik P&P** → sprawdź przypisanie kolumn, zaznacz kolumny do porównania w kolumnie **Comparator** i **jednostkę X, Y** (mm albo mils) → **Podgląd** → **Dalej: Comparator**. Porównywane są pola zaznaczone i przy BOM, i przy P&P. Bez P&P: **Pomiń P&P**. **← Wróć do kroku 1** usuwa wczytany BOM.
4. **Comparator** — krok 3 (zakładka **Comparator**). Desygnatory P&P są wylistowane po przecinku; tabelę przewiniesz w poziomie suwakiem na dole. **← Wróć do kroku 2** cofa P&P i wszystkie rozstrzygnięcia (także rozdzielenia linii).
   - **Porównywane:** zaznacz pola, które bierzemy pod uwagę (Value, Footprint, Description, PartNumber). Po zmianie linie zgodne zatwierdzają się same, a niezgodne wracają do sprawdzenia.
   - **Pokaż tylko niezgodności** — filtr.
   - Tabela ma 8 kolumn jak w dotychczasowym comparatorze (Designators, Value, Footprint, Description, PartNumber — BOM i P&P). Komórka, w której wartość się różni, ma **pomarańczowe tło i pogrubienie**; reszta niezatwierdzonej linii — delikatny kolor. Najedź na linię, żeby zobaczyć wszystko.
   - **Pusty Footprint w BOM:** jeśli wszystkie desygnatory mają w P&P ten sam Footprint, program przyjmuje go z P&P. Jeśli różne (np. R1–R5 0402, R6–R10 0603) — w panelu po prawej pojawi się **Rozdziel linię według P&P**; każda grupa dostaje własną linię na końcu (numer np. „12 (z 5)”).
5. **Rozstrzygnij każdą niezgodność** (wybierz linię → panel po prawej, zakładka **Rozbieżność**). Wynik pokazuje każde pole osobno: nazwa pola, „BOM: …” i „P&P: …” w osobnych liniach, na końcu desygnatory. Linia wysłana do Wykazu Różnic zatwierdza się już tylko w zakładce **Wykaz Różnic**.
   - **Zatwierdź** → przy każdym polu, które się różni, wybierz wartość **z BOM** albo **z P&P** → opcjonalnie komentarz → **Zatwierdź linię**;
   - **Do Wykazu Różnic** → wpisz pytanie do Klienta → **Dodaj do Wykazu Różnic**.
6. **Wykaz Różnic** (zakładka): tabela w układzie firmowego wzoru → **Eksportuj do Excela** → wyślij Klientowi. Po odpowiedzi: **Wczytaj odpowiedzi z Excela…**.
7. **Zatwierdź pozycje Wykazu** (panel po prawej): podstawa **Odpowiedzi Klienta** albo **Wcześniejszej realizacji** (wpisz odpowiedź albo komentarz) → wartość **BOM**, **P&P** albo **Własna** → **Zatwierdź pozycję**.
8. **Tylko w P&P** (zakładka): desygnatory z P&P, których nie ma w BOM, z danymi z P&P (wartość, Footprint, opis, X/Y w mm, obrót, strona). Na razie tylko do wglądu.
9. **Dalej: Integracja Danych** — krok 4 (zakładka **Integracja Danych**): przypisanie najważniejszych informacji do rdzenia BOM — Lp., Desygnatory, Ilość, **Case, Footprint, Value** (z bazy komponentów po Part Number, a gdy komponentu nie ma w bazie — z danych Klienta po Comparatorze), PartNumber, Part TS. **Najedź na pozycję** — po prawej zobaczysz wszystkie dane z BOM Klienta (także kolumny bez przypisania) i skąd wzięły się wartości.
10. **Zatwierdzenie P&P** (Technolog SMT): zamknij program, uruchom ponownie, wybierz konto „Technolog SMT (test)”, otwórz pakiet (**Pakiet Elektroniczny** → **Otwórz pakiet**) → **Dalej: Integracja Danych** → **Dalej: Zatwierdzenie i wersje** (krok 5) → **Zatwierdź P&P**. Wróć na konto Opiekuna.
11. **Zatwierdź BOM:** krok 5 **Zatwierdzenie i wersje** → **Warunki** → wersja **dla ZK** (numer `nnn/rrrr`) albo **bazowa** → komentarz → **Zatwierdź BOM**.
12. **Historia:** Comparator → panel po prawej → zakładka **Historia** — czy każdy krok jest zapisany (kto, kiedy, było → będzie).

## Na co zwrócić uwagę

- Czy wynik porównania zgadza się z tym, co pokazywał comparator.
- Czy rozdzielenie linii daje takie linie, jakie zrobiłbyś w Excelu.
- Czy Case / Footprint / Value w iParcie są takie, jak trzeba.
- Ile czasu zajął proces w porównaniu z Excelem.

## Znane ograniczenia wersji 0.3.2

- BOM jest zapisywany w tle przy przejściu do Importu P&P (bez osobnego kroku „rewizja”). Ponowne wczytanie tego samego BOM do pakietu tworzy kolejny wczytany BOM — wybierasz go w nagłówku pakietu.
- Na etapie Comparatora nie dopisujemy komponentów Klienta, nie przypisujemy THT ani Part TS — Part TS omówimy osobno.
- Value z bazy komponentów jest tworzone z parametrów (rezystancja, pojemność, indukcyjność, częstotliwość); dla innych komponentów — z danych Klienta. W bazie testowej komponentów może nie być — wtedy wszystko pochodzi z danych Klienta.
- Zakładka „Tylko w P&P” jest tylko do wglądu; panel „Zatwierdzenie i wersje” — do uzgodnienia.
- Własną wartość można wpisać tylko w Wykazie Różnic; przy „Zatwierdź” w Comparatorze wybierasz BOM albo P&P.
- Zatwierdzenia, decyzji i rozdzielenia linii nie da się cofnąć w programie — w razie pomyłki zanotuj to w uwagach.
- Uprawnienia nie są sprawdzane (pilotaż — role umowne).
- Błędy programu zapisują się w `%LOCALAPPDATA%\iPart\logs` — przy zgłoszeniu podaj godzinę.

## Uwagi

| # | Krok | Co się stało / czego brakuje | Waga (blokuje / przeszkadza / drobne) |
|---|---|---|---|
| 1 | | | |
