---
status: proponowana
data: 2026-11-24
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0004 Schemat bazy wdraża tylko administrator

## Kontekst

- iPart to aplikacja WPF, którą każdy użytkownik uruchamia u siebie. Łączy się z jedną bazą SQL Server kontem Windows.
- EF Core pozwala aplikacji zastosować migracje przy starcie, ale wymaga to uprawnień do zmiany struktury bazy dla każdego użytkownika.
- Na serwerze firmy bazą zarządza IT.

## Decyzja

1. **Aplikacja nigdy nie zmienia struktury bazy.** Nie stosuje migracji przy starcie.
2. **Schemat wdraża administrator** idempotentnym skryptem SQL z `deploy/sql/`, wygenerowanym z migracji EF Core.
3. **Użytkownicy mają tylko role `db_datareader` i `db_datawriter`.**
4. **Najpierw baza, potem aplikacja.** Zmiany schematu tylko dodają tabele i kolumny, żeby poprzednia wersja aplikacji działała na nowej bazie.
5. Aplikacja sprawdza przy połączeniu, czy schemat bazy pasuje do jej wersji, i pokazuje komunikat, gdy brakuje tabel.

## Rozważane warianty

- **Migracje przy starcie aplikacji** — odrzucony: każdy użytkownik potrzebowałby uprawnień `db_owner`, a dwie wersje aplikacji mogłyby jednocześnie migrować bazę.
- **Migracje przez `dotnet ef` na serwerze** — dopuszczalne jako alternatywa dla administratora z SDK .NET; skrypt SQL pozostaje podstawą, bo nie wymaga SDK.

## Konsekwencje

- Każda wersja ze zmianą modelu ma skrypt aktualizacji w `deploy/sql/` ([Środowiska](../system/07%20Środowiska.md)).
- Usunięcie lub zmiana kolumny wymaga dwóch wersji: najpierw aplikacja przestaje z niej korzystać, potem skrypt ją usuwa.

## Do weryfikacji

- Akceptacja IT: kto i jak uruchamia skrypty na serwerze.
