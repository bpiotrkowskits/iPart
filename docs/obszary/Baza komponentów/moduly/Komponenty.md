---
status: propozycja
zakres: moduł Komponenty (obszar Baza komponentów)
przegląd: 2026-11-26
kod: src/iPart.Core/Components, src/iPart.Core/Units, src/iPart.Infrastructure (SqlServerComponentService, Import), src/iPart.App/ViewModels (ComponentsViewModel, ImportExcelViewModel)
---

# Komponenty

## Cel

Moduł prowadzi rekord każdego komponentu używanego w firmie: co to jest (parametry, Case), jak go montujemy (Footprint, sposób montażu, PADS) i pod jakimi symbolami magazynowymi występuje. Dane są wspólne dla wszystkich pakietów i Klientów.

## Odpowiedzialność

| Posiada | Nie posiada (i kto posiada) |
|---|---|
| komponent: MPN, producent, nazwy, typ, Case, Footprint, sposób montażu, MSL, RoHS, temperatury, Critical, status produktu, AEC, odnośniki | MPN zatwierdzone i niedozwolone linii BOM → BOM pakietu |
| parametry typowane: rezystory, kondensatory (z ESR), cewki, LED, złącza, kryształy | słowniki obudów, reguły zapasu → Obudowy i reguły zapasu |
| PADS | stany magazynowe → system magazynowy, Zaopatrzenie |
| statusy zatwierdzenia Case, Footprint, parametrów/Value i sposobu montażu | Klient → Usługi wspólne |
| footprinty dedykowane (wariant oznaczony Klientem) | |
| powiązania z symbolami magazynowymi | |
| ustawienia wyświetlania Part TS na poziomie komponentu | |
| migracja z dataPart | |

## Pojęcia

Ze [Słownika](../../../system/03%20Słownik.md): komponent, MPN, Case, Footprint, footprint dedykowany, sposób montażu, Value, parametr nieukrywalny, PADS, symbol magazynowy, Part TS.

Pojęcia lokalne:
- **Status danej** — stan zatwierdzenia jednego pola komponentu: brak / niezatwierdzona / zatwierdzona (kto, kiedy). W dataPart: flagi `|` i `login [data]`.

## Model

```
Komponent (GUID; klucz: MPN + producent)
 ├─ dane ogólne (nazwy, typ, MSL, RoHS, temperatury, Critical, status produktu, AEC, odnośniki)
 ├─ Case, Footprint, sposób montażu, Value/parametry — każde ze statusem (kto, kiedy)
 ├─ Parametry typowane (liczby z jednostkami)
 ├─ PADS
 ├─ Footprint dedykowany (Klient → wariant footprintu)
 ├─ Symbol magazynowy (symbol; własny / powierzony przez Klienta)
 └─ Ustawienia wyświetlania Part TS
```

- Tabele w schemacie `components` ([ADR-0005](../../../decyzje/ADR-0005%20Schemat%20bazy%20na%20obszar.md)); historia według [ADR-0007](../../../decyzje/ADR-0007%20Jeden%20mechanizm%20historii%20zmian.md).
- Szczegóły techniczne 0.1.0 (normalizacja MPN, jednostki, współbieżność, migracja): [Model komponentów etap nauki](../../../robocze/Model%20komponentów%20etap%20nauki.md), [Import dataPart etap nauki](../../../robocze/Import%20dataPart%20etap%20nauki.md).

## Reguły

### Tożsamość

1. **Klucz: MPN + producent.** Ten sam MPN u dwóch producentów to dwa komponenty.
2. **Klient nie jest częścią klucza.** Klient oznacza właściciela fizycznego towaru, nie dokumentacji — dane techniczne są wspólne. Własność leży przy symbolu magazynowym: komponent może mieć symbol własny i symbole „powierzony przez Klienta”.
3. MPN zapisany dokładnie (tylko obcięte białe znaki na brzegach); bez rekordów „Nieznany” i bez automatycznego scalania.

### Statusy zatwierdzenia

4. Case, Footprint, Value/parametry i sposób montażu mają status: brak / niezatwierdzona / zatwierdzona (kto, kiedy). **Zmiana danej zatwierdzonej cofa ją do niezatwierdzonej** i wymaga ponownego zatwierdzenia przez uprawnioną osobę.
5. **Case** ustala Opiekun Projektu; **Footprint** — Technolog SMT (w zatwierdzeniu P&P). Footprint raz ustalony obowiązuje wszędzie.
6. **Brak zatwierdzonego Footprintu** → w BOM pakietu lokalny wpis jako kopia Case; po zatwierdzeniu P&P dopisywany do bazy jako zatwierdzony.
7. **Footprint dedykowany** to cecha komponentu: wariant oznaczony Klientem, pokazywany, gdy BOM i P&P należą do tego Klienta.

### Parametry

8. **Parametry jako liczby z jednostkami.** Value w formacie firmy, zawsze pełny.
9. **ESR z jednostką;** bez jednostki = mΩ. W bazie dokładnie, w Value jako `ESR0300`. Bez cichego „naprawiania” wpisu.
10. **PADS** uzupełnia Opiekun Projektu przy tworzeniu komponentu (potrzebne do estymacji).
11. **Critical** zostaje, z nową logiką (do ustalenia).

### Dopisywanie komponentów

12. Nowe MPN z BOM Klienta dodaje Opiekun Projektu (bez zatwierdzenia albo z zatwierdzeniem) — nic nie jest dopisywane automatycznie.
13. **Zakupowiec może dopisać nowy MPN zamiennika** z podstawowymi danymi; resztę uzupełnia Opiekun Projektu.
14. Gdy pojawi się ten sam MPN innego producenta — nowy rekord; Opiekun uzupełnia producenta w starym (zob. decyzje robocze).

### Migracja z dataPart

15. **Przenosimy komponenty z danymi** (ok. 25 tys. MPN), **bez rekordów z samym MPN** (424).
16. Flagi: `login [data]` → zatwierdzona (kto, kiedy, oznaczenie „z dataPart”); `|`, brak flagi i stare wpisy bez daty → niezatwierdzona (stary tekst do historii). **Case bez flagi = niezatwierdzony.**
17. **Footprint — tylko zatwierdzone**; pozostałe puste.
18. Sposób montażu — jako niezatwierdzony, podpowiadany przy pierwszym użyciu. thtCase daje tylko sposób montażu THT (niezatwierdzony), gdy komponent go nie ma; footprinty z thtCase pomijamy.
19. Parametry nieczytelne → raport migracji do ręcznego przejrzenia, bez zgadywania. **defValue i historia z kolumn Tag — nie przenosimy**; historia zaczyna się od migracji.
20. PADS — wszystkie. Footprinty dedykowane — wszystkie, jako warianty oznaczone Klientem.
21. **dataPart działa dalej w czasie alfy;** przy pełnym starcie drugi import danych dopisanych po pierwszej migracji (zob. decyzje robocze).

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| komenda | Dodaj komponent | Opiekun Projektu; zakupowiec — z podstawowymi danymi |
| komenda | Zmień dane komponentu | w sesji edycji; zmiana danej zatwierdzonej cofa status |
| komenda | Zatwierdź daną | Case, Footprint, Value/parametry, sposób montażu |
| komenda | Zapisz Footprint z P&P | od BOM pakietu: „w bazie / tylko w projekcie” |
| zapytanie | Rozpoznaj MPN | MPN (+ producent) → kandydaci |
| zapytanie | Dane komponentu | dla BOM pakietu, Zaopatrzenia, Kompletacji, Estymacji; z footprintem dedykowanym dla Klienta |
| zdarzenie publikowane | Dane komponentu zmienione | „było → będzie” — do BOM pakietu |

## Zależności

- **Obudowy i reguły zapasu** — słownik obudów, równoważne nazwy, wzorce (ostrzeżenie przy niezgodnym Case).
- **Usługi wspólne** — Klient (footprinty dedykowane, symbole powierzone), użytkownicy, historia, blokady.
- **System magazynowy / Nexo** — symbole magazynowe (sposób dostępu do ustalenia).

## Legacy

| Legacy | Co przejmuje moduł | Co znika |
|---|---|---|
| dataPart: partsData, resValue, capValue, ctceValue, PADS | komponenty, parametry, PADS, statusy | flagi tekstowe, kolumny Tag, defValue |
| dataPart: footprinty dedykowane Klienta | warianty footprintu oznaczone Klientem | formuła biorąca pierwszy footprint |
| dataPart: thtCase | sposób montażu THT | osobna biblioteka footprintów THT |
| dataPart: Critical | znacznik Critical (nowa logika) | — |

## Stan

2026-11-26: wersja 0.1.0 ma agregat komponentu z typowanymi parametrami, słowniki, walidację, zapis z historią komponentów i podgląd importu dataPart (bez zapisu). Klucz w kodzie to jeszcze MPN + producent + Klient — do zmiany. Brak: statusów zatwierdzenia, footprintów dedykowanych, symboli magazynowych. Nic nie działa produkcyjnie.

## Decyzje robocze (do akceptacji)

1. **Producent po migracji pusty** (brak w dataPart). Gdy pojawi się ten sam MPN innego producenta — nowy rekord, a Opiekun uzupełnia producenta w starym. Przy rozpoznawaniu MPN z BOM: dopasowanie po producencie z BOM, przy kilku kandydatach — wybór Opiekuna ([Projekt bazy danych](../../../robocze/Projekt%20bazy%20danych.md), 10.5).
2. **Drugi import z dataPart:** nowe MPN dodawane; komponenty zmienione i w dataPart, i w aplikacji → raport do decyzji; po drugim imporcie dataPart tylko do odczytu ([Decyzje VBA](../../../robocze/Decyzje%20VBA.md), 9.6).

## Otwarte pytania

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Jakie podstawowe dane podaje zakupowiec, dopisując nowy MPN zamiennika? | Kierownik działu Obsługi Projektu, Zakupowiec |
| Logika Critical: co oznacza, kto ustawia, jakie ma skutki | Kierownik działu Obsługi Projektu |
| Footprinty dedykowane: gdy komponent ma kilka wariantów dla jednego Klienta — który obowiązuje? | Kierownik działu Obsługi Projektu, Technolog SMT |
| Skąd iPart bierze symbole magazynowe (import, system magazynowy, Nexo)? | drugi developer, developer IT |

## Decyzje

- [ADR-0005](../../../decyzje/ADR-0005%20Schemat%20bazy%20na%20obszar.md) Schemat bazy na obszar
- [ADR-0006](../../../decyzje/ADR-0006%20Stałe%20identyfikatory%20i%20powiązania%20zewnętrzne.md) Stałe identyfikatory
- [ADR-0007](../../../decyzje/ADR-0007%20Jeden%20mechanizm%20historii%20zmian.md) Jeden mechanizm historii zmian
