---
status: przyjęta
data: 2026-11-26
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0012 Budowa modułów zgodna z ProduMan

## Kontekst

- iPart jest systemem przejściowym; jego moduły mają dać się przejąć albo dołączyć do modularnego monolitu ProduMan ([ADR-0001](ADR-0001%20iPart%20jako%20system%20przejściowy%20do%20czasu%20modułu%20BOM.md)).
- Developer IT przekazał przejściowe zasady budowy modułów ProduMan (Domain / Application / Infrastructure, handlery bez MediatR, `Result<T>`, osobny `DbContext` i schemat na moduł, Outbox, .NET 8 w bibliotekach modułów, MVVM z CommunityToolkit.Mvvm i Material Design) — jako informację, nie polecenie. Standard „Granice modułów i usług” ProduMan: nowy moduł tylko przy samodzielnej odpowiedzialności, własności danych i stabilnym kontrakcie; nie dzielimy „na zapas”.
- Wersja 0.1.0 i początek etapu 1 mają jeden `DbContext` dla wszystkich schematów, klucz obcy między schematami (pakiet → Klient) i biblioteki na .NET 10.

## Decyzja

1. **Moduł backendowy = obszar iPart.** Każdy obszar ma trzy projekty `iPart.<Obszar>.Domain / .Application / .Infrastructure`, własny `DbContext`, własny schemat ([ADR-0005](ADR-0005%20Schemat%20bazy%20na%20obszar.md)) i własną historię migracji. Moduły z kart obszaru (np. Komponenty, Obudowy i reguły zapasu) to wewnętrzne części kodu modułu backendowego, nie osobne zestawy projektów.
2. **Reguły w modelu domeny.** Zapis: komenda → osobny handler w Application (wstrzykiwany bezpośrednio, bez MediatR) → EF Core w Infrastructure. Komenda zwraca `Result` albo `Result<T>`; błąd biznesowy to `Fail`, nie wyjątek. Bez procedur składowanych i wyzwalaczy z logiką, bez Event Sourcingu.
3. **Między modułami tylko identyfikatory** — bez kluczy obcych między schematami i bez odczytu tabel innego modułu. Wywołanie przez publiczny interfejs aplikacyjny modułu w tym samym procesie albo wiadomość przez Outbox zapisany w tej samej transakcji co zmiana (według tabeli „Dobór sposobu komunikacji” ProduMan).
4. **Biblioteki modułów na .NET 8** (EF Core 8), żeby dało się je dołączyć do monolitu ProduMan. Serwer aplikacji i klient WPF mogą być na .NET 10.
5. **Odczyt danych modułu przez serwer aplikacji** ([ADR-0010](ADR-0010%20Serwer%20aplikacji%20między%20klientem%20a%20bazą.md)); własne dane — EF `AsNoTracking`. Dane spoza iPart (widok kont ProduMana, widoki Nexo) — SQL tylko do odczytu (Dapper), w osobnych serwisach odczytu.
6. **Klient WPF:** MVVM z CommunityToolkit.Mvvm, Material Design in XAML (MaterialDesign3), nawigacja ViewModel-first, kolory przez tokeny według standardu „Motyw i tokeny UI” ProduMan. ViewModel korzysta tylko z interfejsów (klient API), nigdy z EF ani SQL; bez logiki biznesowej w ViewModelach.
7. **Czas:** każda kolumna z datą ma w nazwie, czy jest w UTC (`…Utc`).

## Rozważane warianty

- **Jeden `DbContext` dla całego iPart** (jak w 0.1.0) — odrzucony: łatwiejszy na start, ale moduł nie dałby się przenieść osobno, a klucze obce między schematami wiązałyby obszary.
- **Zestaw projektów na każdą kartę modułu** — odrzucony: kilkanaście projektów bez korzyści z „Granic modułów” (wspólna własność danych w obszarze).
- **Biblioteki na .NET 10** — odrzucony: nie dałoby się ich dołączyć do monolitu na .NET 8.

## Konsekwencje

- Kod etapu 1 (jeden `IPartDbContext`) dzielimy na moduły; migracje zaczynają się od nowa dla każdego modułu. Dane wersji 0.1.0 (baza testowa, etap nauki) nie są przenoszone — ich przeniesienie opisuje skrypt jednorazowy tylko wtedy, gdy okaże się potrzebne.
- Klucz obcy pakiet → Klient znika; pakiet przechowuje Guid Klienta, a istnienie Klienta sprawdza handler przez interfejs modułu Usługi wspólne.
- Własne klasy MVVM z 0.1.0 (`ObservableObject`, `RelayCommand`, `AsyncCommand`) zastępuje CommunityToolkit.Mvvm; wygląd przechodzi na Material Design przy przebudowie ekranów.
- Pojęcia, których iPart używa inaczej niż Słownik ProduMan (np. kompletacja, BOM), zgłaszamy developerowi IT.
