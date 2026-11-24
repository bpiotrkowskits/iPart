---
status: szkic
zakres: moduł <Nazwa> (obszar <Obszar>)
przegląd: RRRR-MM-DD
kod: <nazwa projektu/katalogu w src/, jeśli istnieje>
---

# <Nazwa modułu>

> Szablon karty modułu. Usuń sekcje, które nie mają jeszcze treści, zamiast zostawiać je puste. Karta rośnie razem z modułem — od odkrywania domeny do implementacji.

## Cel

1–3 zdania: na jakie pytanie biznesowe odpowiada moduł.

## Odpowiedzialność

| Posiada | Nie posiada (i kto posiada) |
|---|---|
| … | … → Moduł X |

## Pojęcia

Pojęcia ze Słownika (`system/03 Słownik.md`), których używa moduł. Pojęcia lokalne (znane tylko w tym module) — z definicją.

## Model

Agregaty i ich odpowiedzialność. Na etapie odkrywania: kandydaci, oznaczeni jako „kandydat”.

## Reguły

Reguły biznesowe, które moduł egzekwuje. Stan obowiązujący, nie historia ustaleń.

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| komenda | … | … |
| zapytanie | … | … |
| zdarzenie publikowane | … | … |
| zdarzenie konsumowane | … | od kogo |

## Zależności

Moduły i systemy zewnętrzne, z których moduł korzysta, i w jaki sposób (zdarzenia, zapytania, kontrakt).

## Legacy

Co moduł przejmuje z legacy (tabele, pola, serwisy, joby) i jak wygląda okres przejściowy.

## Stan

Kilka zdań z datą: co istnieje w kodzie, co działa produkcyjnie.

## Decyzje robocze (do akceptacji)

Rozstrzygnięcia podjęte przez piszącego bez decydującego (zasady: „Zasady dokumentacji”, rozdział 4). Numerowana lista; przy decyzji z ADR — link. Po akceptacji punkt znika, a treść zostaje w regułach lub modelu.

## Otwarte pytania

Numerowana lista pytań z informacją, kto może odpowiedzieć.

## Decyzje

Linki do ADR dotyczących modułu.
