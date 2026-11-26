---
status: propozycja
zakres: moduł Użytkownicy i dostępy (obszar Usługi wspólne)
przegląd: 2026-11-26
kod: brak
---

# Użytkownicy i dostępy

## Cel

Moduł odpowiada na pytania: kim jest użytkownik, co może zobaczyć i zmienić, do jakiej grupy należy i kto go zastępuje.

## Odpowiedzialność

| Posiada                                                         | Nie posiada (i kto posiada)                                   |
| --------------------------------------------------------------- | ------------------------------------------------------------- |
| odniesienie do konta firmowego systemu logowania                | konta, hasła, logowanie → firmowy system logowania            |
| role (kilka na osobę)                                           | nieobecności jako źródło → docelowo moduł Pracownicy ProduMan |
| macierz dostępów: dostęp × rola → edycja / odczyt / brak        |                                                               |
| grupy i członkostwo; administrator grupy jako cecha członkostwa |                                                               |
| zastępstwa (od–do)                                              |                                                               |

## Pojęcia

Ze [Słownika](../../../system/03%20Słownik.md): administrator grupy, zastępstwo, operator tematu.

Pojęcia lokalne:
- **Rola** — zestaw dostępów (np. Zakupowiec). Osoba może mieć kilka ról.
- **Grupa** — dział albo etap pracy (np. Magazyn). Służy do zastępstw, administratorów i zadań bez operatora; nie daje dostępów.
- **Dostęp** — nazwany obszar pracy w aplikacji (np. „Ceny”) z poziomem: edycja / odczyt / brak.

## Model

```
Użytkownik (GUID; odniesienie do konta systemu logowania)
 ├─ Role użytkownika
 ├─ Członkostwo w grupie (administrator grupy: tak / nie)
 └─ Zastępstwo (zastępca; od–do; kto wpisał)
Rola ─ Dostęp (poziom: edycja / odczyt / brak)
```

Tabele w schemacie `shared`.

## Reguły

1. **Użytkownik z firmowego systemu logowania;** iPart przechowuje tylko odniesienie do konta. Role, grupy i zastępstwa — lokalnie.
2. **Dostępy przez uprawnienia, nie przez ukrywanie kolumn** (np. ceny).
3. **Zastępstwo** wpisuje sam użytkownik albo administrator grupy. W okresie zastępstwa pozycje „Do obsłużenia” trafiają do zastępcy, kopia zostaje u nieobecnego.
4. **Administrator grupy** (niekoniecznie kierownik) wpisuje zastępstwa i zwalnia blokady edycji w swojej grupie. **Wyznacza go i zmienia Kierownik działu Obsługi Projektu** (np. gdy administrator jest nieobecny).
5. **Rola „Operator danych BOM” jest scalona z „Opiekun Projektu”.**
6. **Grupy na start:** Obsługa Projektu, Technologia SMT, Zakupy (z Kierownikiem Logistyki), Magazyn, Produkcja, Opiekunowie klienta; później zapewne Technologia THT.

### Macierz dostępów (pierwsza wersja)

E — edycja, O — odczyt, — brak.

| Dostęp                                       | Opiekun Projektu | Kier. działu OP | Technolog SMT | Zakupowiec | Kier. Logistyki | Magazyn | Produkcja | Opiekun klienta |
| -------------------------------------------- | ---------------- | --------------- | ------------- | ---------- | --------------- | ------- | --------- | --------------- |
| Import i porównanie BOM / P&P                | E                | E               | O             | O          | O               | —       | —         | O               |
| BOM pakietu (Part TS, MPN, zamienniki stałe) | E                | E               | O             | E¹         | O               | O       | O         | O               |
| Zatwierdzenie BOM                            | E                | E               | —             | —          | —               | —       | —         | —               |
| Zatwierdzenie P&P i Footprint                | —                | E               | E             | —          | —               | —       | O         | —               |
| Baza komponentów                             | E                | E               | E²            | E³         | O               | O       | O         | —               |
| Zestawienia zakupowe (w tym łączenie)        | E⁴               | E               | —             | E          | E               | O       | —         | —               |
| Ceny                                         | —                | O               | —             | E          | E               | —       | —         | —               |
| Reguły zapasu                                | O                | E               | —             | O          | E               | —       | —         | —               |
| Kompletacja, partie, opakowania              | O                | O               | —             | O          | O               | E       | O         | —               |
| Widok dla produkcji (dPart)                  | O                | O               | O             | O          | O               | E       | O         | —               |
| Panel administratora                         | —                | E               | —             | —          | —               | —       | —         | —               |

¹ zamienniki z podstawą; ² tylko Footprint; ³ tylko dopisanie nowego MPN zamiennika z podstawowymi danymi; ⁴ tworzenie i edycja zestawień z kilkoma ZK.

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| zapytanie | Bieżący użytkownik | konto, role, grupy |
| zapytanie | Czy ma dostęp | dostęp → edycja / odczyt / brak |
| zapytanie | Kto odbiera | osoba → ona albo zastępca w danym dniu |
| komenda | Wpisz zastępstwo | użytkownik albo administrator grupy |
| komenda | Wyznacz administratora grupy | Kierownik działu Obsługi Projektu |
| komenda | Zmień dostęp roli | panel administracji dostępów |

## Zależności

- **Firmowy system logowania** (dwóch pozostałych developerów) — konta.
- **Do obsłużenia i komunikaty** — korzysta z zastępstw i grup.

## Legacy

Brak odpowiednika: w plikach Excel hasło w kodzie i tryby pliku.

## Stan

2026-11-26: brak kodu. W 0.1.0 login Windows zapisywany tekstem przy rekordach.

## Otwarte pytania

| Pytanie                                                                                | Kto może odpowiedzieć         |
| -------------------------------------------------------------------------------------- | ----------------------------- |
| Firmowy system logowania: mechanizm, stały identyfikator konta, lista kont, dane osoby | developer IT, drugi developer |

## Decyzje

- [ADR-0006](../../../decyzje/ADR-0006%20Stałe%20identyfikatory%20i%20powiązania%20zewnętrzne.md) Stałe identyfikatory
