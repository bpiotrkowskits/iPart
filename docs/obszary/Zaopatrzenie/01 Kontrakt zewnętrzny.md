---
status: propozycja
zakres: obszar Zaopatrzenie
przegląd: 2026-11-26
---

# Obszar Zaopatrzenie — kontrakt zewnętrzny

## Odbiorcy

Kompletacja, Dokumentacja pakietu (zamienniki), Usługi wspólne („Do obsłużenia”, komunikaty); w przyszłości ProduMan albo system przejmujący gospodarkę materiałową.

## Komendy przyjmowane

| Komenda | Od kogo | Dane | Skutek | Odpowiedź / zdarzenie |
|---|---|---|---|---|
| Uwzględnij zmianę BOM | Dokumentacja pakietu (zdarzenie „Linia zmieniona po zatwierdzeniu”) | ZK, linia, „było → będzie” | lista dla zakupowca: dokupić / anulować / nadmiar ze zmiany | pozycja „Do obsłużenia” dla zakupowca |

## Zdarzenia publikowane

| Zdarzenie | Kiedy | Dane | Typowy odbiorca |
|---|---|---|---|
| Zamiennik zatwierdzony | zakupowiec wybrał MPN spoza listy | pakiet, linia, MPN, podstawa, zakres | Dokumentacja pakietu |
| MPN od Klienta | import odesłanej listy towaru powierzonego | pakiet, linia, MPN | Dokumentacja pakietu |
| Pozycje zestawienia zmienione | zapis zmian w zestawieniu | zestawienie, pozycje, „było → będzie” | Kompletacja |
| Uwaga zakupów | zakupowiec dodał uwagę | zestawienie / ZK / linia, treść | Kompletacja (magazyn) jako komunikat |

## Zapytania udostępniane

| Zapytanie | Dane | Uwagi |
|---|---|---|
| Zestawienie dla ZK | ZK → zestawienie, ZK w zestawieniu, wersje BOM, ilości | jeden ZK — jedno zestawienie |
| Pozycje zakupu | zestawienie → linie, wybrany MPN i status, ilości, ZD, LT, część powierzona | ceny tylko dla uprawnionych ról |
| Stan magazynu dla MPN | MPN → ilości wg rodzaju | z ostatniego importu |

## Oczekiwane od systemów zewnętrznych

- **Dokumentacja pakietu:** zatwierdzone wersje BOM dla ZK, MPN zatwierdzone i niedozwolone, zdarzenia o zmianach.
- **Baza komponentów:** zapas dla obudowy, porównanie parametrów, równoważne obudowy, dopisanie MPN zamiennika.
- **Usługi wspólne:** Klient, dostępy (ceny), „Do obsłużenia”, komunikaty, historia, blokady.
- **Nexo:** ZK i ZD (numery; docelowo odczyt) — sposób dostępu do ustalenia.
- **System magazynowy:** stan magazynu i cennik — na razie pliki.
- **Importer systemu magazynowego (drugi developer):** czyta kolumny O–Q listy towaru powierzonego — ich układ jest częścią tego kontraktu.

## Zasady zgodności

- Kontrakt posługuje się GUID i numerami ZK `nnn/rrrr` ([ADR-0006](../../decyzje/ADR-0006%20Stałe%20identyfikatory%20i%20powiązania%20zewnętrzne.md)).
- Zmianę kontraktu wewnątrz iPart wprowadzamy razem z odbiorcami w tej samej wersji aplikacji; odbiorcy nie czytają tabel schematu `purchasing` bezpośrednio.
- **Układ pliku listy towaru powierzonego zmieniamy tylko po uzgodnieniu z drugim developerem** (importer do systemu magazynowego).

## Otwarte pytania

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Jak iPart czyta ZK, ZD i stany z Nexo? | developer IT |
| Nadwyżka zakupowa i podsumowanie kosztów | Kierownik Logistyki |
