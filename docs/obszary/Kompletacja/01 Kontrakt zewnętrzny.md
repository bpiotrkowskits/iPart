---
status: szkic
zakres: obszar Kompletacja
przegląd: 2026-11-26
---

# Obszar Kompletacja — kontrakt zewnętrzny

## Odbiorcy

Zaopatrzenie (wydania i uwagi magazynu), produkcja (widok desygnacji), w przyszłości ProduMan (gotowość materiałowa) albo system, który przejmie obszar.

## Komendy przyjmowane

| Komenda | Od kogo | Dane | Skutek | Odpowiedź / zdarzenie |
|---|---|---|---|---|
| Utwórz partię | magazyn | ZK, ilość dla każdego ZK | nowa partia | Partia utworzona |
| Importuj opakowania | magazyn | plik z systemu magazynowego | przypisanie opakowań do linii | lista nieprzypisanych i konfliktów |
| Oznacz partię jako gotową | magazyn | partia | gotowość (wymaga zatwierdzenia P&P dla SMT i braku nieobsłużonych zmian) | Partia gotowa |

## Zdarzenia publikowane

| Zdarzenie | Kiedy | Dane | Typowy odbiorca |
|---|---|---|---|
| Partia gotowa | magazyn oznaczył partię | partia, ZK, technologia | produkcja, ProduMan |

## Zapytania udostępniane

| Zapytanie | Dane | Uwagi |
|---|---|---|
| Kompletacja partii | linie, opakowania, wydano, różnica, pojemniki | ceny niewidoczne |
| Symulacja ilości | ZK albo partia, nowa ilość → braki | bez zapisu |

## Oczekiwane od systemów zewnętrznych

- **Dokumentacja pakietu:** wersja BOM dla ZK (linie, sposób montażu i wydania, MPN), stan zatwierdzenia P&P per technologia, zmiany linii po zatwierdzeniu.
- **Zaopatrzenie:** pozycje zakupu z wybranym MPN i ilościami.
- **System magazynowy:** opakowania (MPN, ilość, lokalizacja, pojemnik) — na razie plik.

## Zasady zgodności

Zmiana kontraktu — z informacją dla obszarów Dokumentacja pakietu i Zaopatrzenie; po decyzji o przejęciu obszaru — uzgodnienie z systemem przejmującym.

## Otwarte pytania

Patrz [mapa obszaru](00%20Mapa%20obszaru.md).
