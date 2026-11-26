---
status: przyjęta
data: 2026-11-26
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0008 Pliki źródłowe w bazie danych

## Kontekst

- Dane w iPart powstają z plików: BOM i P&P Klienta, arkusz wyceny zakupów, odesłana przez Klienta lista towaru powierzonego.
- Wersja 0.1.0 zapisuje tylko nazwę zaimportowanego pliku. Pliki na dysku sieciowym bywają przenoszone i nadpisywane; folder dokumentacji Klienta prowadzą Opiekunowie klienta i nie da się go uporządkować.
- Pliki są małe (zwykle poniżej 1 MB).

## Decyzja

1. **Każdy plik, z którego powstają dane, zapisujemy w bazie** przy imporcie: zawartość, nazwa, typ, rozmiar, suma kontrolna, kto i kiedy.
2. Zapisany plik jest niezmienny; rekord, który z niego powstał (np. rewizja importu), wskazuje na niego.
3. Struktura folderów na dysku zostaje bez zmian; aplikacja nie zapisuje tam plików.

## Rozważane warianty

- **Pliki na udziale sieciowym, w bazie ścieżka** — odrzucony: ryzyko przeniesienia lub nadpisania, osobna kopia zapasowa.
- **Docelowo dokumentacja w systemie dokumentacji pakietów** — możliwe później jako integracja; nie zastępuje kopii, z której powstały dane.

## Konsekwencje

- Kopia zapasowa bazy obejmuje pliki — do uwzględnienia w planie kopii z IT ([Środowiska](../system/07%20Środowiska.md)).
- Pliki w schemacie obszaru, który je importuje ([ADR-0005](ADR-0005%20Schemat%20bazy%20na%20obszar.md)).
