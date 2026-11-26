---
status: propozycja
zakres: moduł Do obsłużenia i komunikaty (obszar Usługi wspólne)
przegląd: 2026-11-26
kod: brak
---

# Do obsłużenia i komunikaty

## Cel

Moduł dba o to, żeby żadna zmiana, zadanie ani wiadomość nie zginęły: każda trafia do konkretnej osoby i ma ślad, kto i jak ją obsłużył.

## Odpowiedzialność

| Posiada | Nie posiada (i kto posiada) |
|---|---|
| pozycje „Do obsłużenia” ze stanem | treść zmiany „było → będzie” → obszar, w którym zaszła zmiana |
| operator tematu (pakiet, ZK, zestawienie, partia) | zastępstwa, grupy → Użytkownicy i dostępy |
| komunikaty, adresaci, potwierdzenia przeczytania | |

## Pojęcia

Ze [Słownika](../../../system/03%20Słownik.md): Do obsłużenia, operator tematu, komunikat, zastępstwo.

## Model

```
Pozycja do obsłużenia (GUID; rodzaj: zmiana / zadanie / komunikat; odnośnik do obiektu;
                      adresat; stan: nowa / obsłużona (kto, kiedy, jak) / odrzucona (powód))
Operator tematu (temat: pakiet / ZK / zestawienie / partia; osoba; od kiedy)
Komunikat (GUID; przypięty do: pakiet / wersja BOM / ZK / linia BOM; treść; autor; data; aktualny / nieaktualny)
 └─ Adresat (dział albo osoba) → Potwierdzenie (kto, kiedy)
```

Tabele w schemacie `shared`.

## Reguły

### Do obsłużenia

1. Pozycja trafia **do konkretnej osoby** — operatora tematu. Bez przypisań do Klientów.
2. Stan: nowa / obsłużona (kto, kiedy, jak) / odrzucona z powodem. Licznik nowych po otwarciu aplikacji.
3. W okresie zastępstwa pozycja trafia do zastępcy, kopia zostaje u nieobecnego.
4. **Źródła pozycji:**

| Zdarzenie | Odbiorca |
|---|---|
| zmiana zatwierdzonego BOM | zakupowiec ZK; magazyn, jeśli kompletacja ruszyła |
| zmiana Footprintu | Technolog SMT |
| BOM czeka na zatwierdzenie P&P | Technolog SMT |
| uwaga zakupów | magazyn |
| zmiana ilości ZK albo zestawienia po rozpoczęciu zakupów | zakupowiec |
| zamiennik zatwierdzony przez zakupowca | Opiekun Projektu (informacja) |
| nowa wersja BOM / rewizja od Klienta | Opiekun Projektu pakietu |
| zmiana linii z przypisanymi opakowaniami | magazyn |
| zwolnienie blokady przez administratora grupy | właściciel blokady |

5. Powiadomienia e-mail albo Teams — później (integracja z Teams ProduMan).

### Komunikaty

6. **Od wszystkich do wszystkich.** Komunikat przypięty do pakietu, wersji BOM, ZK albo linii BOM; komunikaty do pakietu i do linii są rozróżnione.
7. Adresat: dział albo osoba. Komunikat trafia do „Do obsłużenia” jako „do zapoznania”.
8. **Potwierdzenie przeczytania zawsze obowiązkowe**; przycisk „Potwierdź wszystkie”. Autor widzi, kto potwierdził.
9. Bez limitu liczby; stare można oznaczyć jako nieaktualne.
10. **Uwagi zakupów do magazynu** to komunikaty; magazyn obsługuje je jak zmiany.

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| komenda | Dodaj pozycję | od obszarów: rodzaj, obiekt, temat |
| komenda | Obsłuż / odrzuć pozycję | z opisem albo powodem |
| komenda | Wyślij komunikat | obiekt, treść, adresaci |
| komenda | Potwierdź / potwierdź wszystkie | |
| komenda | Podejmij temat | ustala operatora tematu |
| zapytanie | Moje pozycje | lista i licznik |

## Zależności

- **Użytkownicy i dostępy** — grupy, zastępstwa.
- **Wszystkie obszary** — źródła pozycji i obiekty komunikatów.

## Legacy

| Legacy | Co przejmuje moduł |
|---|---|
| komórki START!J16:J26 w iPart, okienka przy imporcie w sPart, cPart, fusePart | komunikaty z adresatami i potwierdzeniem |
| pogrubienia i gradienty zmian w plikach | pozycje „Do obsłużenia” |

## Stan

2026-11-26: brak kodu.

## Decyzje robocze (do akceptacji)

1. **Operator tematu** to osoba, która podjęła temat — utworzyła, zaimportowała albo przejęła. Zadanie bez operatora (np. pierwsze zatwierdzenie P&P) trafia do całej grupy, a po podjęciu przez kogoś — do niego ([Decyzje VBA](../../../robocze/Decyzje%20VBA.md), 9.8).

## Decyzje

- [ADR-0007](../../../decyzje/ADR-0007%20Jeden%20mechanizm%20historii%20zmian.md) Jeden mechanizm historii zmian
