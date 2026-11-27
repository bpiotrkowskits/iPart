---
status: przyjęta
data: 2026-11-26
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0011 Logowanie kontem Nexo przez LoginApi

## Kontekst

- Firmowe aplikacje (ProduMan) logują osoby **kontem Nexo** przez usługę **LoginApi** (login i hasło Nexo). Konto to nie pracownik: część pracowników nie ma konta, część kont nie ma pracownika. ProduMan nie zna loginów Windows i nie łączy ich z kontami.
- Stały identyfikator konta to **Guid konta Nexo** — nie zmienia się przy zmianie loginu ani nazwiska i zostanie nim także, gdyby logowanie przeszło z Nexo gdzie indziej (ADR-0036 ProduMan, pkt 11).
- Dział konta to numer ze wspólnego słownika działów; role w działach, grupy osób i umiejętności będą danymi pracownika w module Pracownicy ProduMan (jeszcze nie ma).
- Nie ma testowego LoginApi; testy logują się prawdziwymi kontami. Guidy kont są takie same we wszystkich środowiskach.
- iPart ma serwer aplikacji między klientem a bazą ([ADR-0010](ADR-0010%20Serwer%20aplikacji%20między%20klientem%20a%20bazą.md)).

> **Wdrożenie po pilotażu** — w pilotażu obowiązuje [ADR-0014](ADR-0014%20Pilotaż%20bez%20serwera%20i%20z%20kopiami%20danych%20ProduMana.md); ta decyzja wyznacza kierunek.

## Decyzja

1. **Osoba loguje się do iPart loginem i hasłem konta Nexo.** Aplikacja WPF przekazuje je serwerowi iPart, a serwer sprawdza je w LoginApi (`POST login/`, `Authorization: Basic`). Logowanie jest udane tylko przy HTTP 200, `status == 1` i niepustym `user.id`.
2. **Tożsamością użytkownika iPart jest Guid konta Nexo.** Przypisania (operator tematu, zastępstwa, uprawnienia) i historia zapisują Guid konta; historia — razem z nazwą z chwili zapisu.
3. **Hasła nie zapisujemy** i nie trzymamy dłużej niż trwa wywołanie. Z odpowiedzi LoginApi odczytujemy tylko: Guid, login, nazwę, e-mail, numer działu. Treści odpowiedzi nie logujemy. Komunikat błędu z LoginApi nie trafia do użytkownika.
4. **Tokenu z LoginApi nie używamy** — iPart nie wywołuje API ProduMana. Po udanym logowaniu serwer iPart wydaje własną sesję.
5. **Dane konta** (nazwa, e-mail, dział) iPart zapisuje jako kopię z chwili ostatniego logowania. Nie prowadzi własnej listy działów ani katalogu umiejętności.

## Rozważane warianty

- **Konto Windows (uwierzytelnienie zintegrowane)** — odrzucony: firmowe konta to konta Nexo; nie ma powiązania Windows ↔ konto, jedynym możliwym łącznikiem byłby niepotwierdzony jako jednoznaczny e-mail.
- **Wywołanie LoginApi bezpośrednio z aplikacji WPF** — odrzucony: serwer iPart nie mógłby sprawdzić wyniku (token podpisany kluczami ProduMana, których nie dostajemy).

## Konsekwencje

- Serwer iPart musi mieć dostęp sieciowy do LoginApi (`https://login.api.ts/api`).
- Lista wszystkich aktywnych kont (np. do wyboru zastępcy przed pierwszym logowaniem osoby) wymaga dostępu do widoku kont ProduMana — sposób do ustalenia. Do tego czasu iPart zna osoby, które już się zalogowały.
- Dezaktywacji kont nikt nie zgłasza: iPart sam pokazuje otwarte pozycje przypisane do kont, które przestały się logować albo zostały wyłączone — do decyzji człowieka, bez automatycznego przepisywania.
- Nieobecności (do zastępstw) są w ProduMan po numerze pracownika i są danymi wrażliwymi — na razie zastępstwa wpisujemy ręcznie.
- Wersja 0.1.0 zapisywała login Windows tekstem; nowe zapisy używają Guida konta.
- Doprecyzowanie [ADR-0006](ADR-0006%20Stałe%20identyfikatory%20i%20powiązania%20zewnętrzne.md), pkt 3: dla użytkownika Guid konta Nexo jest jednocześnie identyfikatorem w iPart — wyjątek od zasady własnych identyfikatorów, bo ten Guid jest gwarantowany jako stały także po zmianie systemu logowania.

## Do weryfikacji

- **Token z LoginApi po pilotażu.** Pkt 4 opiera się na informacji, że iPart nie dostanie kluczy tokenu. W odpowiedzi z 2026-11-26 developer IT podaje jako kierunek po pilotażu „logowanie tokenem z LoginApi”. Do wyjaśnienia przed wersją po pilotażu: czy serwer iPart ma przyjmować token z LoginApi (i jak go sprawdzać), czy wydawać własną sesję.
