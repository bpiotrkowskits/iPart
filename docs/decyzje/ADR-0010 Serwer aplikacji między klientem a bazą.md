---
status: przyjęta
data: 2026-11-26
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0010 Serwer aplikacji między klientem a bazą

## Kontekst

- Wersja 0.1.0 łączy aplikację WPF bezpośrednio z SQL Server. Grupa Windows użytkowników ma role `db_datareader` i `db_datawriter` ([Środowiska](../system/07%20Środowiska.md)).
- Ustalono dostępy zależne od roli (edycja / odczyt / brak), ceny widoczne tylko dla Zakupowca, Kierownika Logistyki i Kierownika działu Obsługi Projektu oraz historię, której nie można zmieniać ani usuwać ([Użytkownicy i dostępy](../obszary/Usługi%20wspólne/moduly/Użytkownicy%20i%20dostępy.md), [ADR-0007](ADR-0007%20Jeden%20mechanizm%20historii%20zmian.md)).
- Przy połączeniu bezpośrednim każdy użytkownik może ominąć aplikację (np. Excel, SSMS) i odczytać ceny albo zmienić historię.
- iPart ma być łatwy do przejęcia ([ADR-0001](ADR-0001%20iPart%20jako%20system%20przejściowy%20do%20czasu%20modułu%20BOM.md)); ProduMan i system magazynowy działają w układzie klient → serwer aplikacji → baza.

> **Wdrożenie po pilotażu** — w pilotażu obowiązuje [ADR-0014](ADR-0014%20Pilotaż%20bez%20serwera%20i%20z%20kopiami%20danych%20ProduMana.md); ta decyzja wyznacza kierunek.

## Decyzja

1. **Między aplikacją WPF a bazą działa serwer aplikacji (API).** Tylko serwer ma dostęp do bazy; użytkownicy nie mają loginów SQL.
2. **Dostępy, reguły i zapis historii egzekwuje serwer.** Aplikacja WPF ukrywa niedostępne funkcje dla wygody, ale nie jest zabezpieczeniem.
3. Serwer zawiera moduły wszystkich obszarów iPart (jeden proces, schemat na obszar — [ADR-0005](ADR-0005%20Schemat%20bazy%20na%20obszar.md)).
4. Użytkownik loguje się kontem Nexo; login i hasło sprawdza serwer w LoginApi ([ADR-0011](ADR-0011%20Logowanie%20kontem%20Nexo%20przez%20LoginApi.md)).

## Rozważane warianty

- **Połączenie bezpośrednie zabezpieczone rolami SQL** (osobne role dla grup, ceny w osobnym schemacie, historia tylko do dopisywania) — odrzucony: reguły dostępu w dwóch miejscach, słabsza podstawa przy przejęciu, część reguł (np. dostęp per pakiet, blokady) trudna do wyrażenia w SQL.

## Konsekwencje

- Etap 1 (fundament) obejmuje serwer aplikacji; IT musi udostępnić miejsce do jego uruchomienia (do ustalenia).
- Zmienia się model uprawnień w bazie: dostęp ma konto serwera, nie grupa użytkowników ([Środowiska](../system/07%20Środowiska.md) — do przepisania).
- Schemat bazy nadal wdraża administrator ([ADR-0004](ADR-0004%20Schemat%20bazy%20wdraża%20tylko%20administrator.md)).
- Kod 0.1.0 (WPF → EF Core) przechodzi na układ WPF → API → EF Core.
- Publikacja obejmuje dwie części: serwer i aplikację WPF; zgodność wersji klienta i serwera trzeba sprawdzać przy połączeniu.
