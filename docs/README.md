---
status: propozycja
zakres: cała dokumentacja
przegląd: 2026-11-24
---

# Dokumentacja iPart

iPart to wewnętrzny system firmy EMS do prowadzenia dokumentacji BOM pakietów, bazy komponentów, zakupów i kompletacji. Zastępuje system plików Excel/VBA i jest formą przejściową do czasu, gdy BOM przejmie system magazynowy ([ADR-0001](decyzje/ADR-0001%20iPart%20jako%20system%20przejściowy%20do%20czasu%20modułu%20BOM.md)).

Ta dokumentacja opisuje, **z jakich części składa się system, za co odpowiada każda z nich, jak się ze sobą komunikują i jakie decyzje obowiązują**. Stosuje standard dokumentacji ProduMan ([Zasady dokumentacji](zasady/Zasady%20dokumentacji.md)).

---

## Jak czytać

1. [Kontekst i cel systemu](system/01%20Kontekst%20i%20cel%20systemu.md) — po co jest system, kto z niego korzysta, z czym się łączy, warunki przyszłego przejęcia.
2. [Mapa obszarów](system/02%20Mapa%20obszarów.md) — z jakich obszarów składa się system i jak się ze sobą komunikują.
3. [Słownik](system/03%20Słownik.md) — znaczenie pojęć, z odesłaniami do Słownika ProduMan dla pojęć wspólnych.
4. Mapa obszaru, którego dotyczy zadanie (`obszary/`), a z niej karta modułu.
5. [Decyzje](decyzje/README.md) — gdy trzeba wiedzieć, *dlaczego* coś jest tak, a nie inaczej.
6. [Środowiska](system/07%20Środowiska.md) — baza danych, uprawnienia, publikacja aplikacji i cykl nowej wersji.

## Struktura

| Katalog | Zawiera | Nie zawiera |
|---|---|---|
| [`system/`](system/) | opis całego systemu: kontekst, mapa obszarów, słownik, środowiska | szczegółów obszarów i modułów |
| `obszary/` | po jednym katalogu na obszar: mapa obszaru, kontrakt zewnętrzny, karty modułów (`moduly/`), materiały robocze (`robocze/`) | opisu innych obszarów (tylko linki) |
| `standardy/` | przekroje techniczne obowiązujące wszystkie moduły | wiedzy domenowej |
| [`decyzje/`](decyzje/README.md) | jeden rejestr decyzji (ADR) dla całego systemu | zadań i planów |
| [`zasady/`](zasady/Zasady%20dokumentacji.md) | zasady pisania dokumentacji i szablony dokumentów | — |
| [`robocze/`](robocze/) | materiały robocze przekrojowe: wywiad, analiza systemu Excel, przegląd kodu VBA | niczego, co obowiązuje |

## Poziomy

```
System   — co jest w systemie i jak części się łączą (bez szczegółów)
 └ Obszar — co obszar posiada, jakie ma moduły, jaki kontrakt wystawia
    └ Moduł — model, reguły, interfejs, zależności, przejmowane legacy (pliki Excel)
```

Każdy poziom opisuje tylko siebie i linkuje do poziomu niższego.

## Stan dokumentacji

| Dokument | Status |
|---|---|
| System: kontekst, mapa obszarów, słownik | propozycja |
| [System: środowiska](system/07%20Środowiska.md) | propozycja; środowisko na serwerze w trakcie ustalania z IT |
| System: architektura techniczna, kierunek i etapy rozwoju | do utworzenia |
| Obszary: [Dokumentacja pakietu](obszary/Dokumentacja%20pakietu/00%20Mapa%20obszaru.md), [Baza komponentów](obszary/Baza%20komponentów/00%20Mapa%20obszaru.md), [Zaopatrzenie](obszary/Zaopatrzenie/00%20Mapa%20obszaru.md), [Usługi wspólne](obszary/Usługi%20wspólne/00%20Mapa%20obszaru.md) — mapy, kontrakty zewnętrzne i karty modułów | propozycja |
| Obszary: [Kompletacja](obszary/Kompletacja/00%20Mapa%20obszaru.md), [Estymacja](obszary/Estymacja/00%20Mapa%20obszaru.md) — mapa i kontrakt, bez kart | propozycja / szkic; zakres czeka na decyzję o przejęciu |
| [Materiały robocze](robocze/) | roboczy: [wywiad](robocze/Wywiad%20obieg%20plików.md), [analiza techniczna](robocze/analiza-techniczna/README.md), [przegląd kodu VBA](robocze/Decyzje%20VBA.md) (sesje 1–5; przeniesione do kart modułów), [projekt bazy danych](robocze/Projekt%20bazy%20danych.md) (przeniesiony do ADR i kart), opis kodu etapu nauki ([model komponentów](robocze/Model%20komponentów%20etap%20nauki.md), [import dataPart](robocze/Import%20dataPart%20etap%20nauki.md)) |

## Najważniejsze zasady

Pełne zasady: [Zasady dokumentacji](zasady/Zasady%20dokumentacji.md). W skrócie:

- **Jeden fakt — jedno miejsce.** Inne dokumenty linkują, nie kopiują.
- **Jeden dokument — jedna funkcja i jeden poziom.**
- **Status w nagłówku** każdego dokumentu: `szkic`, `propozycja`, `obowiązuje`, `zastąpiony`, `roboczy`.
- **Pojęcia ze Słownika.** Pojęć wspólnych z ProduMan nie definiujemy ponownie.
- **Bez backlogu, promptów, transkryptów i logów** w dokumentacji.
- **Bez danych Klientów, nazw projektów, imion i nazwisk pracowników.**
- **Decyzje robocze** (sekcja „Decyzje robocze (do akceptacji)” przed „Otwarte pytania”, ADR `proponowana`) czekają na akceptację — dokumentacja się na nich opiera, ale nie są poleceniem do wdrożenia.

## Dla agentów AI

- Zaczynaj od tego pliku, potem Słownik i dokumenty właściwe dla zadania.
- Materiały robocze, dokumenty o statusie `propozycja` lub `szkic`, decyzje robocze i ADR `proponowana` nie są poleceniem do wdrożenia — potwierdź z użytkownikiem.
- Gdy zauważysz niespójność między dokumentami albo między dokumentem a kodem, zgłoś ją; nie poprawiaj po cichu.
- Nie dopisuj do dokumentacji notatek z sesji, list zadań ani promptów.
- Dokumentacja ProduMan jest źródłem pojęć wspólnych; nie zmieniaj jej i nie kopiuj z niej treści.
