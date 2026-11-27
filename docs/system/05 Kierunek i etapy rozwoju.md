---
status: obowiązuje
zakres: system — kierunek, zakres wersji alfa, etapy budowy
przegląd: 2026-11-26
---

# Kierunek i etapy rozwoju

## Cel i warunek powodzenia

Celem jest całkowite wyjście z plików Excel/VBA: jedno źródło danych pakietu dla wszystkich działów ([Kontekst i cel systemu](01%20Kontekst%20i%20cel%20systemu.md)). iPart jest systemem przejściowym ([ADR-0001](../decyzje/ADR-0001%20iPart%20jako%20system%20przejściowy%20do%20czasu%20modułu%20BOM.md)).

Droga: **wersja alfa (pilot) → pełny start → koniec okresu przejściowego.**

- **Alfa = pilot.** Kilka wybranych pakietów przechodzi **całą ścieżkę** tylko w aplikacji: import BOM, opracowanie, zakupy, kompletacja i widok dla produkcji. Pozostałe pakiety prowadzimy w Excelu, bez pracy równoległej na tym samym pakiecie ([Decyzje VBA](../robocze/Decyzje%20VBA.md), 9.6).
- **Układ pilotażu** ([ADR-0014](../decyzje/ADR-0014%20Pilotaż%20bez%20serwera%20i%20z%20kopiami%20danych%20ProduMana.md)): program łączy się z bazą testową bezpośrednio, bez serwera aplikacji i LoginApi; użytkownik wybiera konto z listy, bez hasła — uprawnienia i „kto zmienił” są umowne, więc w pilotażu nie wprowadzamy danych, które trzeba chronić przed częścią uczestników. Rdzeń jak docelowo (operacje przez handlery, Guid konta, `PublicId`), żeby serwer i logowanie dołożyć po pilotażu bez przepisywania.
- **W alfie zestawienie zakupowe obejmuje jeden ZK.** Do pilota wybieramy pakiety, których ZK nie są łączone z innymi. Model bazy przewiduje kilka ZK, więc łączenie dochodzi po alfie bez przebudowy.
- **Alfa jest udana**, gdy pakiety pilotażowe zostały obsłużone do wydania na produkcję bez sięgania do plików Excel, a produkcja i logistyka potwierdziły, że dane się zgadzają.
- **Pełny start:** wszystkie nowe pakiety w aplikacji, drugi import z dataPart, dataPart tylko do odczytu.
- **Koniec okresu przejściowego:** po zamknięciu ostatniego pakietu rozpoczętego w Excelu, bez daty z góry.

## Zakres wersji alfa

Cała ścieżka w uproszczonej formie. „Uproszczone” oznacza tę samą regułę z mniejszą wygodą, a nie inną regułę. Model danych od początku jest docelowy, żeby po alfie nie migrować danych drugi raz.

| Obszar / moduł                             | W alfie                                                                                                                                                                 | Uproszczenie w alfie                                                                                        | Po alfie                                                                                                             |
| ------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| **Usługi wspólne** — Użytkownicy i dostępy | wybór konta z listy kont ProduMana, uprawnienia konta z szablonów, grupy | lista kont i słownik działów jako kopie danych ProduMana; uprawnienia nadaje developer; bez hasła | ekran administracji dostępów, zastępstwa, administrator grupy                                                        |
| Do obsłużenia i komunikaty                 | lista „Do obsłużenia”, operator tematu, komunikaty z potwierdzeniem                                                                                                     | tylko w aplikacji (licznik po otwarciu)                                                                     | e-mail / Teams                                                                                                       |
| Edycja i historia                          | historia zmian, blokada całego BOM z wygasaniem i szkicem                                                                                                               | —                                                                                                           | blokada per sekcja                                                                                                   |
| Ustawienia i panel administratora          | Klienci, ustawienia firmy i Klienta                                                                                                                                     | kolory stałe; bez panelu — zmiany słowników przez developera na prośbę Kierownika działu OP                 | panel administratora, ustawienia pakietu i linii                                                                     |
| **Baza komponentów** — Komponenty          | migracja z dataPart, edycja, statusy zatwierdzeń, footprinty dedykowane, PADS                                                                                           | symbole magazynowe wpisywane ręcznie                                                                        | drugi import z dataPart (pełny start)                                                                                |
| Obudowy i reguły zapasu                    | reguły zapasu z migracji, edycja progów, przypisanie obudowy przez zakupowca                                                                                            | obudowy jako tekst                                                                                          | wzorce, grupowa zmiana nazwy (sesja 6)                                                                               |
| **Dokumentacja pakietu** — BOM pakietu     | import BOM i P&P, porównanie, podlinie, potwierdzenia, sekcja TS, Part TS, MPN zatwierdzone i niedozwolone, zamienniki, zatwierdzenie P&P i BOM, wersje bazowe i dla ZK | nowa rewizja od Klienta: dopasowanie po desygnatorach, podziały i scalenia linii rozstrzyga Opiekun ręcznie | „uczenie się” zapisów Value Klienta                                                                                  |
| **Zaopatrzenie** — Zestawienie zakupowe    | zestawienie z **jednym ZK**, wybór MPN, zamienniki, ilości z zapasem i magazynem, ZD                                                                                    | ZK z kopii zleceń ProduMana (`nexo.vw_customer_orders`), nowe — ręcznie; ZD ręcznie | **łączenie kilku ZK** (zestawienie `FUSEnnn/rrrr`), odczyt z Nexo, nadwyżka zakupowa (czeka na Kierownika Logistyki) |
| Stan magazynu i wycena                     | stan magazynu (migawka do decyzji, bez własnej ewidencji), cennik i wycena; ceny tylko dla uprawnionych (umownie) | stany ręcznie albo z danych startowych; cennik i wycena z pliku | odczyt z systemu magazynowego                                                                                        |
| Towar powierzony                           | lista dla Klienta, import odesłanej listy                                                                                                                               | —                                                                                                           | —                                                                                                                    |
| **Kompletacja**                            | partie, import opakowań, „Wydano”, zmiany do obsłużenia, gotowość partii, widok dla produkcji                                                                           | import opakowań z pliku                                                                                     | symulacja „Zmień ilości”, odczyt z systemu magazynowego                                                              |
| **Estymacja**                              | —                                                                                                                                                                       | —                                                                                                           | sesja 6                                                                                                              |

## Etapy budowy

Kolejność wynika z zależności: każdy etap korzysta z poprzednich. Etap kończy się, gdy jego część ścieżki działa na danych testowych.

| Etap                                   | Rezultat                                                                                                               | Zależy od |
| -------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- | --------- |
| 1. Fundament                           | schematy na obszar, GUID i kody słowników, Klient z GUID, wspólna historia, użytkownicy i dostępy, pliki w bazie       | —         |
| 2. Baza komponentów                    | klucz MPN + producent, statusy, footprinty dedykowane, zapis migracji z dataPart, reguły zapasu                        | 1         |
| 3. BOM pakietu                         | import i porównanie z P&P, opracowanie, zatwierdzenia, wersje                                                          | 1, 2      |
| 4. Do obsłużenia i komunikaty, blokady | wspólne mechanizmy pracy podpięte pod BOM pakietu                                                                      | 1, 3      |
| 5. Zaopatrzenie                        | zestawienie, importy, towar powierzony                                                                                 | 2, 3, 4   |
| 6. Kompletacja                         | partie, opakowania, widok dla produkcji                                                                                | 3, 5      |
| 7. Publikacja i pilot | program na udziale i skrypt bazy testowej ([Środowiska](07%20Środowiska.md)), kopie danych ProduMana, migracja dataPart, wybór pakietów pilotażowych, szkolenie | 1–6       |

## Stan

2026-11-26: dokumentacja systemu, obszarów i modułów w statusie propozycji. Układ pilotażu uzgodniony z developerem IT ([ADR-0014](../decyzje/ADR-0014%20Pilotaż%20bez%20serwera%20i%20z%20kopiami%20danych%20ProduMana.md)); serwer i logowanie przez LoginApi — po pilotażu.

**Etap 1 w toku** (gałąź `etap-1-fundament`). Zrobione:
- model danych: schematy `components`, `documentation`, `shared`; Klient z GUID; klucz komponentu MPN + producent; wspólna historia w każdym schemacie; kopia pliku BOM w bazie przy imporcie; kody sposobów montażu;
- uprawnienia nadawane kontu: katalog i szablony w kodzie, zapis po Guidzie konta ([ADR-0013](../decyzje/ADR-0013%20Uprawnienia%20nadawane%20kontu%20z%20szablonów.md));
- kopia wspólnego kodu ProduMana (`ProduMan.SharedKernel`, .NET 8);
- kopie danych ProduMana: lista kont (`produman.users`) i słownik działów (`employees.Departments`, dane startowe z ADR-0036 ProduMana);
- klient LoginApi — gotowy, nieużywany w pilotażu.

- aplikacja desktopowa w nowym układzie (wersja 0.2.0): Material Design 3, menu po lewej, nawigacja ViewModel-first (CommunityToolkit.Mvvm); wybór konta przy starcie; Pakiety; BOM pakietu z kolorami linii, panelem linii (sekcja TS i Oryginał), potwierdzeniem z podstawą (handler komendy) i historią pakietu (serwis odczytu); Import BOM, Baza komponentów i podgląd importu dataPart przeniesione bez zmian.

- **proces comparator + iPart (wersja 0.2.0, 2026-11-27):** import P&P z kopią pliku, porównanie BOM–P&P po desygnatorach, decyzja „sprawdziłem” / Wykaz Różnic z odpowiedzią Klienta, sekcja TS (Part TS, sposób montażu), MPN zatwierdzone (automatycznie z BOM Klienta) i niedozwolone, zatwierdzenie P&P, zatwierdzenie BOM jako niezmienna wersja (bazowa albo dla ZK); każdy krok w historii pakietu;
- **wdrożenie testowe:** baza testowa zaktualizowana skryptem `deploy/sql/aktualizacja-0.2.0-pilot.sql`, konta testowe, wydanie 0.2.0 z wbudowanym .NET w folderze wersji, dziennik błędów, sprawdzenie zgodności bazy przy starcie; scenariusz testu: [Scenariusz testu pilotażowego BOM](../robocze/Scenariusz%20testu%20pilotażowego%20BOM.md).

Do zrobienia w etapie 1: kopie zleceń i pakietów Nexo (`nexo.*`) — po otrzymaniu kopii od developera IT; `EssegiId` przy pakiecie; przeniesienie starszych ekranów (Import BOM, Komponenty) na nowy wygląd i operacje przez handlery.

## Otwarte pytania

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Które pakiety wchodzą do pilota i kto z każdego działu w nim pracuje | Kierownik działu Obsługi Projektu |
