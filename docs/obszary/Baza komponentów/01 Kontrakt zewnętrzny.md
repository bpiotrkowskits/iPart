---
status: propozycja
zakres: obszar Baza komponentów
przegląd: 2026-11-26
---

# Obszar Baza komponentów — kontrakt zewnętrzny

## Odbiorcy

Dokumentacja pakietu, Zaopatrzenie, Kompletacja, Estymacja, Usługi wspólne (panel administratora).

## Komendy przyjmowane

| Komenda | Od kogo | Dane | Skutek | Odpowiedź / zdarzenie |
|---|---|---|---|---|
| Dodaj komponent | Dokumentacja pakietu (Opiekun) | MPN, producent, dane z BOM, z zatwierdzeniem / bez | nowy komponent | GUID komponentu |
| Dodaj MPN zamiennika | Zaopatrzenie (zakupowiec) | MPN, producent, podstawowe dane (zakres do ustalenia) | nowy komponent do uzupełnienia przez Opiekuna | GUID komponentu |
| Zapisz Footprint | Dokumentacja pakietu (zatwierdzenie P&P) | komponent, Footprint, ew. Klient (wariant dedykowany) | Footprint zatwierdzony | Dane komponentu zmienione |
| Przypisz obudowę do grupy zapasu | Zaopatrzenie (zakupowiec) | Case, grupa, rodzaj (zakupy / powierzone), ZK | przypisanie zapamiętane jako podpowiedź | — |

## Zdarzenia publikowane

| Zdarzenie | Kiedy | Dane | Typowy odbiorca |
|---|---|---|---|
| Dane komponentu zmienione | zapis zmian komponentu albo grupowa zmiana nazwy obudowy | komponent, „było → będzie”, status | Dokumentacja pakietu |

## Zapytania udostępniane

| Zapytanie | Dane | Uwagi |
|---|---|---|
| Rozpoznaj MPN | MPN (+ producent) → kandydaci | przy kilku kandydatach wybiera Opiekun |
| Dane komponentu | GUID (+ Klient) → parametry, Case, Footprint (z wariantem dedykowanym Klienta), sposób montażu, PADS, statusy | |
| Symbole magazynowe | komponent → symbole (własny / powierzony przez Klienta) | |
| Zapas dla obudowy | Case, ilość, rodzaj → zapas albo „brak reguły” | próg „od dołu” |
| Czy obudowy równoważne | Case A, Case B → tak / nie | |
| Porównaj parametry | komponent A, komponent B, parametry widoczne w Part TS → taki sam / lepszy / gorszy | dla zamienników i łączenia |

## Oczekiwane od systemów zewnętrznych

- **Usługi wspólne:** Klient (dla footprintów dedykowanych i symboli powierzonych), użytkownicy i dostępy, historia, blokady.
- **System magazynowy / Nexo:** symbole magazynowe — sposób dostępu do ustalenia.

## Zasady zgodności

- Kontrakt posługuje się GUID komponentu i kodami słowników ([ADR-0006](../../decyzje/ADR-0006%20Stałe%20identyfikatory%20i%20powiązania%20zewnętrzne.md)); nazwy obudów i słowników mogą się zmieniać.
- Zmianę kontraktu wprowadzamy razem z odbiorcami w tej samej wersji aplikacji; odbiorcy nie czytają tabel schematu `components` bezpośrednio ([ADR-0005](../../decyzje/ADR-0005%20Schemat%20bazy%20na%20obszar.md)).

## Otwarte pytania

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Zakres podstawowych danych przy dopisaniu MPN zamiennika przez zakupowca | Kierownik działu Obsługi Projektu, Zakupowiec |
| Skąd iPart bierze symbole magazynowe? | drugi developer, developer IT |
