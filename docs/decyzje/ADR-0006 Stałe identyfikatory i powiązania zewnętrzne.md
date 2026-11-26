---
status: przyjęta
data: 2026-11-26
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0006 Stałe identyfikatory i powiązania zewnętrzne

## Kontekst

- W plikach Excel linie łączono po pozycji wiersza, a Klienta i projekt odczytywano z nazw folderów. Przestawienie wiersza albo przeniesienie pliku psuło dane.
- Przejęcie przez system magazynowy wymaga identyfikatorów, które przechodzą bez zmian ([ADR-0001](ADR-0001%20iPart%20jako%20system%20przejściowy%20do%20czasu%20modułu%20BOM.md)).
- iPart łączy się z Nexo, systemem magazynowym, dokumentacją pakietów i firmowym systemem logowania — każdy ma własne klucze.

## Decyzja

1. **Każdy rekord biznesowy ma stały GUID**, nadawany przy utworzeniu i nigdy niezmieniany: pakiet, rewizja importu, wersja BOM, linia, komponent, zestawienie zakupowe, partia, komunikat, Klient.
2. **Słowniki mają stały kod** (np. `THT`), niezależny od wyświetlanej nazwy edytowanej w panelu administratora.
3. **Klucze innych systemów to osobne powiązania**, nie identyfikatory iPart: ZK ↔ numer `nnn/rrrr` (później dokument Nexo), pakiet ↔ `EssegiId`, komponent ↔ symbol magazynowy, Klient ↔ kontrahent Nexo, użytkownik ↔ konto firmowego systemu logowania.
4. **Numery dla ludzi są osobne od identyfikatorów:** numer linii (stały w pakiecie), rewizji importu, wersji BOM, partii (`PP1`…), zestawienia z kilkoma ZK (`FUSEnnn/rrrr`, kolejny w roku, z opcjonalną nazwą).

## Rozważane warianty

- **Numery liczbowe nadawane przez bazę** — odrzucony: zmieniają się przy migracji do innej bazy.
- **Klucze z Nexo jako identyfikatory** — odrzucony: nie wszystkie rekordy mają odpowiednik w Nexo, a zmiana systemu ERP zmieniłaby tożsamość danych.

## Konsekwencje

- Klient i słowniki z 0.1.0 (dziś liczba `int`) dostają GUID lub kod — migracja.
- Kontrakty obszarów posługują się GUID i kodami słowników.
