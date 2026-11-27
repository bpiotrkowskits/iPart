---
status: propozycja
zakres: obszar Usługi wspólne
przegląd: 2026-11-26
---

# Obszar Usługi wspólne — kontrakt zewnętrzny

## Odbiorcy

Wszystkie obszary iPart.

## Komendy przyjmowane

| Komenda | Od kogo | Dane | Skutek | Odpowiedź / zdarzenie |
|---|---|---|---|---|
| Dodaj pozycję „Do obsłużenia” | każdy obszar | rodzaj (zmiana / zadanie / komunikat), obiekt, temat, ew. grupa | pozycja u operatora tematu albo w grupie; w zastępstwie — u zastępcy | GUID pozycji |
| Podejmij temat | każdy obszar | temat (pakiet, ZK, zestawienie, partia), osoba | operator tematu ustalony | — |
| Wyślij komunikat | każdy obszar (użytkownik) | obiekt (pakiet, wersja BOM, ZK, linia), treść, adresaci | komunikat do zapoznania | GUID komunikatu |
| Załóż / zwolnij blokadę | każdy obszar | obiekt, użytkownik | blokada albo informacja, kto edytuje | — |
| Zapisz plik | każdy obszar przy imporcie | zawartość, nazwa, typ | plik zapisany, niezmienny ([ADR-0008](../../decyzje/ADR-0008%20Pliki%20źródłowe%20w%20bazie%20danych.md)) | GUID pliku |

## Zdarzenia publikowane

| Zdarzenie | Kiedy | Dane | Typowy odbiorca |
|---|---|---|---|
| Blokada wygasła / zwolniona | po 1 godzinie bez aktywności albo przez administratora grupy | obiekt, właściciel, powód | obszar obiektu (zachowanie szkicu) |

## Zapytania udostępniane

| Zapytanie | Dane | Uwagi |
|---|---|---|
| Bieżący użytkownik | konto, role, grupy | |
| Czy ma dostęp | dostęp → edycja / odczyt / brak | macierz w [Użytkownicy i dostępy](moduly/Użytkownicy%20i%20dostępy.md) |
| Klient | GUID → nazwa, kod kraju, język, kontrahent Nexo | |
| Ustawienie obowiązujące | klucz + kontekst (Klient, pakiet, linia) → wartość | firma → komponent → Klient → pakiet → linia |
| Kolor danych | znaczenie + widok → kolor | |
| Plik | GUID → zawartość | |

## Wspólna budowa historii

Historia leży w schemacie każdego obszaru, ale ma tę samą budowę: zestaw zmian (kto, kiedy, czego dotyczy, rodzaj operacji, komentarz) i zmiany pól (obiekt, pole, było → będzie) — [ADR-0007](../../decyzje/ADR-0007%20Jeden%20mechanizm%20historii%20zmian.md), [Edycja i historia](moduly/Edycja%20i%20historia.md).

## Oczekiwane od systemów zewnętrznych

- **LoginApi (konta Nexo):** sprawdzenie loginu i hasła; Guid konta, login, nazwa, e-mail, numer działu ([ADR-0011](../../decyzje/ADR-0011%20Logowanie%20kontem%20Nexo%20przez%20LoginApi.md)).
- **Widok kont ProduMana:** lista aktywnych kont (Guid → nazwa) — sposób dostępu do ustalenia.
- **Nexo:** kontrahenci (powiązanie z Klientem) — później.
- **ProduMan:** integracja z Teams do powiadomień; nieobecności (tylko „niedostępny od–do”) i powiązanie konto ↔ pracownik z modułu Pracownicy; wspólny moduł uprawnień — później.

## Zasady zgodności

- Zmianę kontraktu wprowadzamy razem ze wszystkimi obszarami w tej samej wersji aplikacji.
- Obszary nie czytają tabel schematu `shared` bezpośrednio ([ADR-0005](../../decyzje/ADR-0005%20Schemat%20bazy%20na%20obszar.md)).

## Otwarte pytania

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Dostęp do listy aktywnych kont (widok kont ProduMana) | developer IT |
