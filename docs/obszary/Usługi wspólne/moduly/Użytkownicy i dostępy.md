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

| Posiada | Nie posiada (i kto posiada) |
|---|---|
| użytkownik: Guid konta Nexo i kopia danych konta z ostatniego logowania | konta, hasła, logowanie, aktywność konta → Nexo (LoginApi) |
| uprawnienia konta (katalog, poziom, zakres) i szablony uprawnień | dział, role w działach, umiejętności → moduł Pracownicy ProduMan |
| grupy i członkostwo; administrator grupy jako cecha członkostwa | nieobecności jako źródło → docelowo moduł Pracownicy ProduMan |
| zastępstwa (od–do) | |

## Pojęcia

Ze [Słownika](../../../system/03%20Słownik.md): konto, administrator grupy, zastępstwo, operator tematu.

Pojęcia lokalne:
- **Uprawnienie** — pozycja katalogu (klucz, np. `purchasing.prices`) z poziomem **podgląd** albo **zapis** i zakresem (dział osoby, grupa działów, wszystko).
- **Szablon** — gotowy zestaw uprawnień nadawany osobie (dawniej „rola” z macierzy, np. Zakupowiec). Po nadaniu lista osoby może się różnić od szablonu.
- **Superadministrator** — konto z konfiguracji serwera: pełny dostęp, nadaje uprawnienia.
- **Grupa** — dział albo etap pracy (np. Magazyn). Służy do zastępstw, administratorów i zadań bez operatora; nie daje uprawnień.

## Model

```
Użytkownik (Guid konta Nexo; login, nazwa, e-mail, numer działu — z ostatniego logowania)
 ├─ Uprawnienia konta (mapa klucz → poziom i zakres; kto i kiedy zmienił; wersja)
 ├─ Członkostwo w grupie (administrator grupy: tak / nie)
 └─ Zastępstwo (zastępca; od–do; kto wpisał)
Katalog uprawnień i szablony — w kodzie
```

Tabele w schemacie `shared`. Zmiany uprawnień — w historii obszaru ([ADR-0007](../../../decyzje/ADR-0007%20Jeden%20mechanizm%20historii%20zmian.md)).

## Reguły

> **W pilotażu** ([ADR-0014](../../../decyzje/ADR-0014%20Pilotaż%20bez%20serwera%20i%20z%20kopiami%20danych%20ProduMana.md)): użytkownik wybiera konto z listy kont ProduMana (konta aktywne i nieusunięte), bez hasła — tożsamość i uprawnienia są umowne. Dane konta i działy pochodzą z kopii `produman.users` i `employees.Departments`; nieznany dział pokazujemy jako „dział N (nieznany)”.

1. **Logowanie kontem Nexo** (login i hasło Nexo, sprawdzane przez serwer w LoginApi — [ADR-0011](../../../decyzje/ADR-0011%20Logowanie%20kontem%20Nexo%20przez%20LoginApi.md)). Tożsamością jest Guid konta; hasła nie zapisujemy. Konto to nie pracownik.
2. **Konta wyłączone:** iPart sam pokazuje otwarte pozycje przypisane do kont wyłączonych albo długo nieaktywnych — do decyzji człowieka, bez automatycznego przepisywania.
3. **Uprawnienia ma konto** ([ADR-0013](../../../decyzje/ADR-0013%20Uprawnienia%20nadawane%20kontu%20z%20szablonów.md)). Brak uprawnienia na liście = brak dostępu. Kod sprawdza uprawnienie, nigdy nazwę szablonu.
4. **Serwer sprawdza uprawnienia przy każdej operacji**, z bieżącego stanu (pamięć na minutę) — odebranie działa bez ponownego logowania. Dostęp przez uprawnienia, nie przez ukrywanie kolumn (np. ceny).
5. **Superadministrator:** Kierownik działu Obsługi Projektu (konfiguracja serwera). W alfie uprawnienia nadaje developer według szablonów, na prośbę superadministratora. Nikt nie zmienia własnych uprawnień.
6. **Zakres działów** zapisywany od początku; dopóki żadna reguła iPart nie zależy od działu, szablony nadają „wszystko”.
7. **Zastępstwo** wpisuje sam użytkownik albo administrator grupy. W okresie zastępstwa pozycje „Do obsłużenia” trafiają do zastępcy, kopia zostaje u nieobecnego.
8. **Administrator grupy** (niekoniecznie kierownik) wpisuje zastępstwa i zwalnia blokady edycji w swojej grupie. **Wyznacza go i zmienia Kierownik działu Obsługi Projektu** (np. gdy administrator jest nieobecny).
9. **Szablon „Opiekun Projektu” obejmuje dawną rolę „Operator danych BOM”.**
10. **Grupy na start:** Obsługa Projektu, Technologia SMT, Zakupy (z Kierownikiem Logistyki), Magazyn, Produkcja, Opiekunowie klienta; później zapewne Technologia THT. Dane tymczasowe — do przeniesienia, gdy moduł Pracownicy ProduMan będzie miał grupy osób.

### Katalog uprawnień i szablony

Z — zapis, P — podgląd, — brak. Szablony odpowiadają kolumnom macierzy z sesji 4; przypisy macierzy są osobnymi uprawnieniami (wiersze oznaczone „tylko”).

| Uprawnienie | Klucz | Opiekun Projektu | Kier. działu OP | Technolog SMT | Zakupowiec | Kier. Logistyki | Magazyn | Produkcja | Opiekun klienta |
|---|---|---|---|---|---|---|---|---|---|
| Import i porównanie BOM / P&P | `documentation.import` | Z | Z | P | P | P | — | — | P |
| BOM pakietu (Part TS, MPN, zamienniki) | `documentation.bom` | Z | Z | P | P | P | P | P | P |
| tylko zamienniki w BOM, z podstawą | `documentation.bom.substitutes` | — | Z | — | Z | — | — | — | — |
| Zatwierdzenie BOM | `documentation.bom.approval` | Z | Z | — | — | — | — | — | — |
| Zatwierdzenie P&P i Footprint | `documentation.pnp.approval` | — | Z | Z | — | — | — | P | — |
| Baza komponentów | `components.base` | Z | Z | P | P | P | P | P | — |
| tylko Footprint komponentu | `components.footprint` | — | Z | Z | — | — | — | — | — |
| tylko nowy MPN zamiennika (podstawowe dane) | `components.substitute-mpn` | — | Z | — | Z | — | — | — | — |
| Reguły zapasu | `components.stock-rules` | P | Z | — | P | Z | — | — | — |
| Zestawienia zakupowe | `purchasing.sets` | P | Z | — | Z | Z | P | — | — |
| tylko zestawienia z kilkoma ZK (łączenie) | `purchasing.sets.multi-zk` | Z | Z | — | Z | Z | — | — | — |
| Ceny | `purchasing.prices` | — | P | — | Z | Z | — | — | — |
| Kompletacja, partie, opakowania | `kitting` | P | P | — | P | P | Z | P | — |
| Widok dla produkcji (dPart) | `kitting.production-view` | P | P | P | P | P | Z | P | — |
| Panel administratora | `shared.admin-panel` | — | Z | — | — | — | — | — | — |

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| komenda | Zaloguj | login i hasło konta Nexo → sesja (serwer sprawdza w LoginApi) |
| zapytanie | Bieżący użytkownik | konto, uprawnienia, grupy |
| zapytanie | Czy ma uprawnienie | klucz → podgląd / zapis / brak (z zakresem) |
| zapytanie | Kto odbiera | osoba → ona albo zastępca w danym dniu |
| komenda | Wpisz zastępstwo | użytkownik albo administrator grupy |
| komenda | Wyznacz administratora grupy | Kierownik działu Obsługi Projektu |
| komenda | Nadaj uprawnienia z szablonu / zmień uprawnienia | superadministrator; w alfie developer |

## Zależności

- **LoginApi** (konta Nexo) — logowanie; **widok kont ProduMana** — lista aktywnych kont (sposób dostępu do ustalenia).
- **Do obsłużenia i komunikaty** — korzysta z zastępstw i grup.

## Legacy

Brak odpowiednika: w plikach Excel hasło w kodzie i tryby pliku.

## Stan

2026-11-26: w kodzie (gałąź etapu 1): tabele użytkowników, uprawnień konta i grup w schemacie `shared`; katalog uprawnień i szablony w kodzie (test pilnuje zgodności z tabelą w tej karcie); klient LoginApi z testami. Brak: sesji serwera, ekranu logowania, zastępstw. W 0.1.0 login Windows był zapisywany tekstem.

## Otwarte pytania

| Pytanie                                                                                | Kto może odpowiedzieć         |
| -------------------------------------------------------------------------------------- | ----------------------------- |
| Dostęp do listy aktywnych kont (widok kont ProduMana) | developer IT |

## Decyzje

- [ADR-0006](../../../decyzje/ADR-0006%20Stałe%20identyfikatory%20i%20powiązania%20zewnętrzne.md) Stałe identyfikatory
- [ADR-0011](../../../decyzje/ADR-0011%20Logowanie%20kontem%20Nexo%20przez%20LoginApi.md) Logowanie kontem Nexo przez LoginApi
- [ADR-0013](../../../decyzje/ADR-0013%20Uprawnienia%20nadawane%20kontu%20z%20szablonów.md) Uprawnienia nadawane kontu z szablonów
