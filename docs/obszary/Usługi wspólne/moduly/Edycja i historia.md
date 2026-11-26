---
status: propozycja
zakres: moduł Edycja i historia (obszar Usługi wspólne)
przegląd: 2026-11-26
kod: częściowo — historia komponentów w src/iPart.Infrastructure
---

# Edycja i historia

## Cel

Moduł ustala, jak użytkownicy zmieniają dane: kto w danej chwili edytuje, co się dzieje z niezapisaną pracą i jak każda zmiana zostaje zapisana w historii.

## Odpowiedzialność

| Posiada | Nie posiada (i kto posiada) |
|---|---|
| blokady edycji i szkice | dane edytowane → obszar, do którego należą |
| wspólna budowa historii: zestaw zmian + zmiany pól | tabele historii obszaru → schemat tego obszaru |
| reguły komentarza do zmiany | |

## Pojęcia

Ze [Słownika](../../../system/03%20Słownik.md): sesja edycji, blokada edycji.

Pojęcia lokalne:
- **Zestaw zmian** — jeden zapis: „Zapisz zmiany”, import, zatwierdzenie albo decyzja.
- **Szkic** — niezapisana wersja robocza po wygaśnięciu albo zwolnieniu blokady.

## Model

```
Blokada (obiekt; kto; od kiedy; ostatnia aktywność)
Szkic (obiekt; kto; zmiany niezapisane)
Zestaw zmian (GUID; kto; kiedy; czego dotyczy; rodzaj operacji; komentarz)  — w schemacie obszaru
 └─ Zmiana pola (obiekt; pole; było; będzie)
```

## Reguły

1. **Każda zmiana zapisywana zawsze:** kto, kiedy, było → będzie ([ADR-0007](../../../decyzje/ADR-0007%20Jeden%20mechanizm%20historii%20zmian.md)). Jedno „Zapisz zmiany” = jeden wpis i jedno powiadomienie.
2. **Komentarz** opcjonalny w wersji roboczej, **obowiązkowy przy zmianie po zatwierdzeniu**.
3. **Historii nie można edytować ani usuwać z aplikacji.** Przechowywanie na razie bez limitu.
4. **Blokada całego obiektu** (np. BOM pakietu) na czas sesji edycji; pokazuje kto i od kiedy edytuje. W czasie edycji inni widzą ostatni zapis.
5. **Blokada wygasa po 1 godzinie bez aktywności;** wersja robocza zostaje jako szkic. Zmiany zapisane w międzyczasie przez innych → „było → będzie” do scalenia przy dokończeniu szkicu.
6. **Administrator grupy** może zwolnić blokadę od razu, z powodem (historia + pozycja „Do obsłużenia” dla właściciela blokady).
7. **Zamknięcie aplikacji lub wylogowanie** zwalnia blokadę od razu; niezapisane zmiany zostają jako szkic.
8. Później możliwa blokada per sekcja BOM zamiast całego BOM.

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| komenda | Rozpocznij edycję | zakłada blokadę |
| komenda | Zapisz zmiany | zestaw zmian; zwalnia blokadę |
| komenda | Zwolnij blokadę | właściciel albo administrator grupy (z powodem) |
| zapytanie | Historia obiektu | zestawy i zmiany pól |
| zapytanie | Kto edytuje | blokada obiektu |

## Zależności

- **Użytkownicy i dostępy** — administrator grupy.
- **Do obsłużenia i komunikaty** — powiadomienia o zapisie i zwolnieniu blokady.

## Legacy

| Legacy | Co znika |
|---|---|
| kopia arkusza porównywana po pozycji (iPart), kolumny Tag (dataPart), pogrubienia i gradienty dat (sPart, cPart, fusePart), zapis pliku jako jedyna „sesja” | zastąpione jednym mechanizmem |

## Stan

2026-11-26: 0.1.0 ma historię zmian komponentów (własna budowa) i token współbieżności komponentu. Brak blokad i szkiców. Historia komponentów przejdzie na wspólny mechanizm.

## Decyzje

- [ADR-0007](../../../decyzje/ADR-0007%20Jeden%20mechanizm%20historii%20zmian.md) Jeden mechanizm historii zmian
