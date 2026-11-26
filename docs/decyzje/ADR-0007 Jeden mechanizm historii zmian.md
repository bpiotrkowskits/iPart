---
status: przyjęta
data: 2026-11-26
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0007 Jeden mechanizm historii zmian

## Kontekst

- W plikach Excel historia była różna w każdym pliku: kopia arkusza porównywana po pozycji (iPart), 15 kolumn Tag bez dat i autorów (dataPart), pogrubienia zmian z ostatniego importu i gradienty dat (sPart, cPart, fusePart).
- Ustalono: każda zmiana zapisywana zawsze — kto, kiedy, było → będzie; jedno „Zapisz zmiany” = jeden wpis i jedno powiadomienie; zmiany po zatwierdzeniu BOM trafiają do zakupów i magazynu „do obsłużenia” ([Decyzje VBA](../robocze/Decyzje%20VBA.md), 2.12, 2.12a, 5.5).
- Wersja 0.1.0 ma osobną historię tylko dla komponentów.

## Decyzja

1. **Zestaw zmian** powstaje przy każdym zapisie: „Zapisz zmiany”, import, zatwierdzenie, decyzja. Zawiera: kto, kiedy, czego dotyczy (pakiet, ZK, komponent…), rodzaj operacji, komentarz.
2. **Zmiany pól** w zestawie: obiekt, pole, było → będzie. Z nich powstają listy „było → będzie” i widoki historii.
3. **Historia leży w schemacie swojego obszaru**, z tą samą budową we wszystkich obszarach ([ADR-0005](ADR-0005%20Schemat%20bazy%20na%20obszar.md)).
4. **Zatwierdzona wersja BOM jest niezmienna** — zmiana to nowa wersja; wersje są historią BOM. Zmiany pól zapisujemy dla wersji roboczej, bazy komponentów, zakupów i kompletacji.
5. Historii nie można edytować ani usuwać z aplikacji. **Przechowywanie: na razie bez limitu.**
6. Komentarz opcjonalny w wersji roboczej, **obowiązkowy przy zmianie po zatwierdzeniu**.

## Rozważane warianty

- **Tabele historyczne SQL Server (temporal tables)** — odrzucony jako jedyny mechanizm: zapisują wiersze, a nie sesję edycji z komentarzem i nie łączą zmian kilku tabel w jeden wpis.
- **Osobna historia w każdym module** — odrzucony: różne formaty, osobne widoki, trudniejsze listy „było → będzie”.

## Konsekwencje

- Historia komponentów z 0.1.0 przechodzi na wspólny mechanizm.
- Historia z Tagów dataPart nie jest migrowana ([Decyzje VBA](../robocze/Decyzje%20VBA.md), 9.2).
