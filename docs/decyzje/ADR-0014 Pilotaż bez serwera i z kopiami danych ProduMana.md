---
status: przyjęta
data: 2026-11-26
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0014 Pilotaż bez serwera i z kopiami danych ProduMana

## Kontekst

- Plan etapu 1 był ułożony pod wersję produkcyjną: serwer aplikacji ([ADR-0010](ADR-0010%20Serwer%20aplikacji%20między%20klientem%20a%20bazą.md)), logowanie przez LoginApi ([ADR-0011](ADR-0011%20Logowanie%20kontem%20Nexo%20przez%20LoginApi.md)), podział na moduły ([ADR-0012](ADR-0012%20Budowa%20modułów%20zgodna%20z%20ProduMan.md)). Uruchomienie tego przed pilotażem zajęłoby tydzień, a każda zmiana wymagałaby wdrożenia na serwerze.
- Developer IT: na pilotaż ograniczamy integrację z ProduManem do minimum i robimy tylko to, co później byłoby drogie do zmiany. Serwer, LoginApi i token wyznaczają kierunek, ale wchodzą po pilotażu.

## Decyzja

**W pilotażu:**

1. **Program WPF łączy się z bazą testową bezpośrednio**, jak w 0.1.0. Bez serwera aplikacji, IIS i LoginApi.
2. **Przy starcie użytkownik wybiera konto z listy**, bez hasła. To nie jest zabezpieczenie: każdy może wybrać dowolne konto, a kto ma dostęp do bazy, czyta wszystko. Uprawnienia (np. do cen) i zapis „kto zmienił” są umowne — w pilotażu nie wprowadzamy danych, które trzeba chronić przed częścią uczestników.
3. **Rdzeń jak docelowo:** ViewModel wywołuje handler komendy albo serwis odczytu, nigdy `DbContext` ani SQL. Serwer i logowanie dojdą bez przepisywania logiki.
4. **W danych od początku:** Guid konta Nexo jako identyfikator osoby, `DepartmentId` jako dział, `PublicId` (Guid) dla każdego rekordu, do którego ktoś będzie się odwoływał.
5. **Wspólny kod ProduMana** (`Result`, `Result<T>`, `EntityBase`, `AggregateRoot`, `ValueObject`, `DomainEventBase`) jako niezmieniana kopia w osobnym projekcie .NET 8 z oryginalnymi przestrzeniami nazw. Przy dołączeniu do ProduMana kopię zastępuje oryginał.
6. **Kopie danych ProduMana i Nexo** w tych samych nazwach i kolumnach co widoki, tylko do odczytu, odświeżane w całości na prośbę: lista kont (`produman.users`, 7 kolumn, bez identyfikatorów stanowiskowych), słownik działów (`employees.Departments`), zlecenia i pakiety (`nexo.vw_customer_orders`, `nexo.vw_customer_orders_lines`, `nexo.vw_electronic_packages`). iPart zapisuje u siebie tylko odwołania (`DocumentId`/numer ZK, `PozycjaId`, `AsortymentId`, `EssegiId`). Po pilotażu kopie zastąpi odczyt z widoków, bez zmian w zapytaniach.
7. **Pakiet wiążemy przez `EssegiId`.** ZD i stany magazynowe wpisujemy ręcznie albo z danych startowych — bez własnej ewidencji stanów.
8. **Bez Outboxu:** zmiana i jej skutki mieszczą się w jednej transakcji; zdarzenia domenowe zbieramy w agregacie i obsługujemy w tej samej transakcji.
9. **Uprawnienia:** katalog w kodzie po Guidzie konta ([ADR-0013](ADR-0013%20Uprawnienia%20nadawane%20kontu%20z%20szablonów.md)), superadmini w konfiguracji, klucze `obszar.funkcja`. Wspólnego formatu z ProduManem nie ma i mechanicznego przeniesienia nikt nie obiecuje.
10. **UI:** MVVM z CommunityToolkit.Mvvm, nawigacja ViewModel-first, bez logiki w ViewModelach; Material Design in XAML (MaterialDesign3.Defaults); kolory tylko z jednego słownika zasobów, nazwane według roli, przez `DynamicResource`; standard „UI WPF” ProduMana jako wskazówka. Bez odwzorowania UI ProduMana jeden do jednego.
11. **Jedna baza, jeden kontekst danych, schematy obszarów zostają** ([ADR-0005](ADR-0005%20Schemat%20bazy%20na%20obszar.md)). Podział kodu na projekty i konteksty per obszar ([ADR-0012](ADR-0012%20Budowa%20modułów%20zgodna%20z%20ProduMan.md), pkt 1) — po pilotażu.

**Po pilotażu** (kierunek bez zmian): serwer aplikacji na IIS (host `*.api.ts`), logowanie tokenem z LoginApi, konto aplikacji bez prawa zmian schematu, kopia testowa bazy `iPart_Test`, odczyt z widoków zamiast kopii, nowa baza z zabezpieczonym połączeniem.

## Rozważane warianty

- **Pełny układ produkcyjny od początku** — odrzucony: tydzień przed pierwszym uruchomieniem i koszt każdej zmiany przy wdrożeniu, zanim pilotaż potwierdzi zakres.

## Konsekwencje

- ADR-0010 i ADR-0011 obowiązują jako kierunek, ale ich wdrożenie przechodzi na czas po pilotażu. Klient LoginApi zostaje w kodzie, nieużywany w pilotażu.
- Własna tabela użytkowników iPart znika: dane konta pochodzą z kopii listy kont; iPart zapisuje Guid konta.
- Hasło do bazy testowej zostaje w pilotażu bez zmian (baza tylko testowa); przy wersji produkcyjnej powstaje nowa baza z zabezpieczonym połączeniem.
