---
status: roboczy
zakres: pilotaż — test procesu comparator + iPart w aplikacji (wersja 0.2.0)
przegląd: 2026-11-27
---

# Scenariusz testu: od BOM Klienta do zatwierdzonej wersji BOM

Dla: Opiekuna Projektu, który pierwszy raz przechodzi proces w aplikacji iPart. Czas: ok. 1–2 godziny.

## Zanim zaczniesz

- **To jest test.** Baza jest testowa, a wybór konta jest bez hasła. Nie wpisuj danych, które muszą zostać tylko w firmie (np. ceny).
- **Przygotuj pliki jednego pakietu**, który znasz: **BOM Klienta (.xlsx)** i **P&P (.txt albo .xlsx)**. Najlepiej taki, przy którym w Excelu były pytania do Klienta.
- **Uruchom program:** `iPart.App.exe` z folderu wersji 0.2.0 na udziale. Instalacja nie jest potrzebna.
- **Notuj uwagi** na bieżąco (punkt „Uwagi” na końcu): co było niejasne, czego brakowało, co trwało za długo.

## Kroki

1. **Wybierz konto** „Opiekun Projektu (test)” i kliknij **Dalej**.
2. **Utwórz pakiet:** menu **Pakiety** → panel „Nowy pakiet”: kod, rewizja Klienta, Klient (w razie potrzeby **Dodaj Klienta**) → **Utwórz pakiet**.
3. **Zaimportuj BOM Klienta:** menu **Import BOM** → wybierz pakiet → **Wybierz plik BOM** → arkusz i wiersz nagłówków → przypisz kolumny do pól (albo **Podpowiedz po nazwach nagłówków**) → **Podgląd** → sprawdź zakładki „Problemy” i „Podgląd linii” → **Zapisz jako nową rewizję importu**. Otworzy się BOM pakietu.
   - Sprawdź: czy desygnatory zostały dobrze rozbite, czy MPN z kolumny Part Number są w panelu linii (zakładka **TS i MPN**, „z BOM Klienta”).
4. **Zaimportuj P&P:** na stronie BOM pakietu → **Importuj P&P…** → **Wybierz plik P&P** → sprawdź wiersz nagłówków i przypisanie kolumn (podpowiedź po nazwach nagłówków) → **Podgląd** → **Zapisz P&P**.
5. **Przejrzyj porównanie:** kolumna **Porównanie z P&P**; filtr **Z rozbieżnościami**; zakładka **Tylko w P&P** (desygnatory, których nie ma w BOM).
6. **Rozstrzygnij każdą rozbieżność** (panel linii → zakładka **Porównanie**):
   - wszystko jasne → **Sprawdziłem**;
   - trzeba zapytać Klienta → wpisz pytanie → **Do Wykazu Różnic**.
7. **Wykaz Różnic:** zakładka **Wykaz Różnic** → **Kopiuj Wykaz Różnic** → wklej do maila albo dokumentu (tak, jak wysyłasz pytania przez Opiekuna klienta).
8. **Odpowiedzi Klienta:** zaznacz linię z Wykazu → wpisz **Odpowiedź Klienta** → **Zapisz odpowiedź Klienta**.
9. **Sekcja TS i MPN** (zakładka **TS i MPN**): uzupełnij **Part TS** i **sposób montażu** → **Zapisz sekcję TS**; dodaj zamiennik (MPN + podstawa) albo **MPN niedozwolony** (zaznacz „Niedozwolony” + powód).
10. **Potwierdź linie** (zakładka **Potwierdzenie**): wybierz podstawę (odpowiedź Klienta / wcześniejsza realizacja / decyzja Opiekuna z uzasadnieniem) → **Potwierdź linię**. Linia traci kolor „niepotwierdzona”.
11. **Zatwierdzenie P&P** (Technolog SMT): zamknij program, uruchom go ponownie, wybierz konto „Technolog SMT (test)”, otwórz pakiet → **Zatwierdź P&P**. Wróć na konto Opiekuna.
12. **Zatwierdź BOM:** zakładka **Zatwierdzenie i wersje** → przeczytaj **Warunki** → wersja **dla ZK** (numer `nnn/rrrr`) albo **bazowa** → komentarz → **Zatwierdź BOM**. Wersja pojawi się na liście „Zatwierdzone wersje”.
13. **Historia:** panel linii → zakładka **Historia** — czy każdy krok jest zapisany (kto, kiedy, było → będzie).

## Na co zwrócić uwagę

- Czy wynik porównania zgadza się z tym, co pokazywał comparator.
- Czy czegoś z pliku iPart brakuje w procesie (pole, kolumna, decyzja).
- Czy kolory i podpowiedzi po najechaniu na linię są czytelne.
- Ile czasu zajął proces w porównaniu z Excelem.

## Znane ograniczenia wersji 0.2.0

- Rozbicie desygnatorów działa automatycznie (z ostrzeżeniem przy zakresach skróconych) — bez decyzji „rozwiń / zostaw jako tekst / wpisz własne” z makiety.
- Brak rozpoznania MPN w bazie komponentów i automatycznego Part TS z parametrów — Part TS wpisujesz ręcznie.
- Podlinie (część linii zgodna, część nie) i wydzielanie linii — jeszcze nie; rozbieżność dotyczy całej linii.
- Potwierdzenia i decyzji nie da się cofnąć w programie — w razie pomyłki zanotuj to w uwagach.
- Uprawnienia nie są sprawdzane (pilotaż — role umowne).
- Błędy programu zapisują się w `%LOCALAPPDATA%\iPart\logs` — przy zgłoszeniu podaj godzinę.

## Uwagi

| # | Krok | Co się stało / czego brakuje | Waga (blokuje / przeszkadza / drobne) |
|---|---|---|---|
| 1 | | | |
