---
status: przyjęta
data: 2026-11-24
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0002 Pliki Excel nie są już poprawiane

## Kontekst

- Analiza kodu VBA i comparatora wykazała błędy w plikach Excel ([analiza techniczna](../robocze/analiza-techniczna/wspolzaleznosci.md)), np. zamienniki z sPart nie wracają do iPart, a zatwierdzenie P&P nadpisuje Footprint w bazie bez ostrzeżenia.
- Każda poprawka wymaga rozesłania nowych wersji plików i aktualizacji plików już używanych w projektach. To wprowadza duże zamieszanie.
- iPart zastępuje pliki Excel ([ADR-0001](ADR-0001%20iPart%20jako%20system%20przejściowy%20do%20czasu%20modułu%20BOM.md)).

## Decyzja

1. **Plików Excel/VBA i comparatora już nie poprawiamy.**
2. Znalezione w nich błędy służą tylko temu, żeby iPart ich nie powtórzył. Zapisujemy je w materiałach roboczych, a reguły iPart — w kartach modułów.

## Rozważane warianty

- **Poprawianie najważniejszych błędów** — odrzucony: koszt rozsyłania i aktualizacji plików jest większy niż korzyść w okresie przejściowym.

## Konsekwencje

- Użytkownicy pracują na plikach w obecnym stanie, ze znanymi ograniczeniami, do czasu przejścia na iPart.
- Przegląd kodu VBA nie tworzy zadań poprawek w Excelu, tylko reguły iPart ([Decyzje VBA](../robocze/Decyzje%20VBA.md)).
- Migracja danych z plików musi uwzględnić znane błędy w danych.
