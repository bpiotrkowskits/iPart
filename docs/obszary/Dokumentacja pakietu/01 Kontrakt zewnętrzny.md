---
status: propozycja
zakres: obszar Dokumentacja pakietu
przegląd: 2026-11-26
---

# Obszar Dokumentacja pakietu — kontrakt zewnętrzny

## Odbiorcy

Zaopatrzenie, Kompletacja, Estymacja, Baza komponentów, Usługi wspólne; w przyszłości system magazynowy (moduł BOM) przy przejęciu ([ADR-0001](../../decyzje/ADR-0001%20iPart%20jako%20system%20przejściowy%20do%20czasu%20modułu%20BOM.md)).

## Komendy przyjmowane

| Komenda | Od kogo | Dane | Skutek | Odpowiedź / zdarzenie |
|---|---|---|---|---|
| Dodaj zamiennik | Zaopatrzenie (zakupowiec) | pakiet, linia, MPN, podstawa, zakres: BOM pakietu / wersja BOM dla ZK | MPN zatwierdzony w BOM pakietu albo w wersji dla ZK | Zamiennik zapisany; informacja dla Opiekuna Projektu |
| Dodaj MPN od Klienta | Zaopatrzenie (Towar powierzony) | pakiet, linia, MPN | MPN zatwierdzony przez Klienta, na stałe w BOM pakietu | MPN zapisany |
| Zatwierdź przejście ZK na nową wersję | Zaopatrzenie (zakupowiec) | ZK, wersja BOM | ZK korzysta z nowej wersji | Wersja dla ZK zmieniona |

## Zdarzenia publikowane

| Zdarzenie | Kiedy | Dane | Typowy odbiorca |
|---|---|---|---|
| Wersja BOM zatwierdzona | zatwierdzenie BOM | pakiet, wersja, ZK, których dotyczy | Zaopatrzenie, Kompletacja |
| Linia zmieniona po zatwierdzeniu | zapis zmian w zatwierdzonym BOM (nowa wersja) | ZK, linia, „było → będzie”, komentarz | Zaopatrzenie, Kompletacja, Usługi wspólne („Do obsłużenia”) |
| P&P zatwierdzone | zatwierdzenie P&P przez technologa | pakiet, wersja, technologia | Kompletacja |
| Footprint zatwierdzony | zatwierdzenie P&P z wyborem „w bazie” | komponent, Footprint (ew. wariant Klienta) | Baza komponentów |
| Nowy MPN do bazy | Opiekun wybrał „dodaj” na liście nowych MPN | MPN, producent, dane z BOM, z zatwierdzeniem / bez | Baza komponentów |

## Zapytania udostępniane

| Zapytanie | Dane | Uwagi |
|---|---|---|
| Wersja BOM dla ZK | linie: numer, desygnatory, ilość, Part TS, sposób montażu i wydania, potwierdzenie, MPN zatwierdzone i niedozwolone | tylko wersje zatwierdzone; MPN wstrzymane niewidoczne |
| Stan zatwierdzenia P&P | pakiet, wersja → technologia, kto, kiedy | per technologia |
| Pakiety Klienta | Klient → pakiety, wersje, ZK | do tworzenia zestawień zakupowych |

## Oczekiwane od systemów zewnętrznych

- **Baza komponentów:** rozpoznanie MPN (MPN + producent → kandydaci), dane komponentu z footprintem dedykowanym dla Klienta, zdarzenie o zmianie danych komponentu.
- **Usługi wspólne:** Klient, użytkownicy i dostępy, ustawienia (firma → Klient → pakiet → linia), blokady, historia, „Do obsłużenia”, komunikaty.
- **Dokumentacja pakietów (DokuMan):** tożsamość pakietu (`EssegiId`) — sposób powiązania do ustalenia.
- **Nexo:** numery ZK `nnn/rrrr` (na razie wpisywane ręcznie).

## Zasady zgodności

- Kontrakt posługuje się GUID i kodami słowników ([ADR-0006](../../decyzje/ADR-0006%20Stałe%20identyfikatory%20i%20powiązania%20zewnętrzne.md)).
- Obszary iPart są w jednej aplikacji i jednej bazie: zmianę kontraktu wprowadzamy razem z odbiorcami w tej samej wersji aplikacji. Odbiorca nie czyta tabel schematu `documentation` bezpośrednio ([ADR-0005](../../decyzje/ADR-0005%20Schemat%20bazy%20na%20obszar.md)).
- Zmiana widoczna dla systemów zewnętrznych — z uprzedzeniem ich właściciela.

## Otwarte pytania

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Jak pakiet wiąże się z `EssegiId`? | drugi developer |
