---
status: przyjęta
data: 2026-11-24
zakres: obszar Dokumentacja pakietu
zastępuje:
zastąpiona-przez:
---

# ADR-0003 Comparator i iPart jako jeden moduł

## Kontekst

- Dziś dane Klienta przechodzą przez trzy kroki: przeklejenie do pliku BOM_P&P, porównanie BOM z P&P w comparatorze (Power Apps + Power Automate), eksport CSV i import do pliku iPart z twardym mapowaniem kolumn.
- Comparator zmienia wielkość liter w danych przekazywanych dalej, obchodzi znaki specjalne (ograniczenie Power Apps) i gubi część zakresów desygnatorów. Decyzje z porównania nie mają śladu w iPart ([Decyzje VBA](../robocze/Decyzje%20VBA.md), 1.1–1.5).
- Wynik porównania (potwierdzenie linii, rozbieżności) jest częścią danych linii BOM.

## Decyzja

1. **Import BOM i P&P, porównanie i opracowanie BOM pakietu są jednym modułem** w obszarze Dokumentacja pakietu.
2. Plik BOM_P&P i eksport CSV znikają: BOM i P&P Klienta importujemy bezpośrednio, z mapowaniem kolumn.
3. Wynik porównania (podlinie, rozbieżności, potwierdzenie z podstawą) zapisuje się w danych linii BOM.

## Rozważane warianty

- **Comparator jako osobny moduł z kontraktem do iPart** — odrzucony: dwa moduły dzieliłyby te same linie BOM, a przekazanie między nimi odtworzyłoby dzisiejszy eksport CSV.
- **Zachowanie comparatora w Power Apps** — odrzucony: ograniczenia znaków i normalizacji, brak śladu decyzji.

## Konsekwencje

- Mechanizmy importu z pliku iPart (CSV, aktualizacja z MASTER, import iPart v1/v2) nie są przenoszone.
- Normalizacja służy tylko do porównania; dane Klienta pozostają w oryginalnym brzmieniu.
- Karta modułu w obszarze Dokumentacja pakietu obejmuje reguły porównania.
