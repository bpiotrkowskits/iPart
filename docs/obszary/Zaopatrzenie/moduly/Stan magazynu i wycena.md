---
status: propozycja
zakres: moduł Stan magazynu i wycena (obszar Zaopatrzenie)
przegląd: 2026-11-26
kod: brak
---

# Stan magazynu i wycena

## Cel

Moduł dostarcza zakupom danych z zewnątrz: stan magazynu, cennik magazynowy i wycenę od dostawców, z kursami walut. W pierwszej wersji z plików, docelowo bezpośrednio z systemu magazynowego.

## Odpowiedzialność

| Posiada | Nie posiada (i kto posiada) |
|---|---|
| import stanu magazynu (MPN, ilości: dostępne, w kompletacji, na ZD, zablokowane) | stan magazynowy jako źródło → system magazynowy |
| import cennika magazynowego | wybór MPN i ilości do zamówienia → Zestawienie zakupowe |
| import wyceny: mapowanie kolumn, dopasowanie, niedopasowane do decyzji | uprawnienia do cen → Usługi wspólne |
| kurs waluty zapisany przy wycenie | |
| kopie importowanych plików ([ADR-0008](../../../decyzje/ADR-0008%20Pliki%20źródłowe%20w%20bazie%20danych.md)) | |

## Model

```
Import stanu magazynu (plik; kto, kiedy) → wiersze stanu (MPN / symbol; ilości wg rodzaju)
Import cennika (plik; kto, kiedy) → ceny magazynowe
Wycena (plik; mapowanie kolumn; kurs i data kursu) → wiersze wyceny dopasowane do pozycji zakupu
```

Tabele w schemacie `purchasing`.

## Reguły

1. **Stan magazynu — import pliku** w pierwszej wersji, docelowo odczyt z systemu magazynowego. Rozróżnia ilości: dostępne, w kompletacji, na ZD, zablokowane (zablokowane nie są doliczane do dostępnych).
2. **Import wyceny z mapowaniem kolumn** i dopasowaniem po MPN albo numerze linii — nigdy po kolejności wierszy. Niedopasowane wiersze do decyzji.
3. **Kurs waluty zapisany przy wycenie** (do potwierdzenia: kurs z dnia wyceny).
4. **Cennik magazynowy** importowany plikiem, docelowo z systemu magazynowego.
5. **Ceny widzą tylko:** Zakupowiec, Kierownik Logistyki, Kierownik działu Obsługi Projektu — przez uprawnienia, nie przez ukrywanie kolumn. Poza cenami magazyn widzi wszystko.

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| komenda | Importuj stan magazynu | plik |
| komenda | Importuj cennik | plik |
| komenda | Importuj wycenę | plik, mapowanie, kurs |
| zapytanie | Stan dla MPN | ilości wg rodzaju, opakowania (jeśli są w imporcie) |
| zapytanie | Cena dla pozycji | tylko dla uprawnionych ról |

## Zależności

- **Zestawienie zakupowe** — odbiorca stanów i cen.
- **Usługi wspólne** — dostępy, pliki, historia.
- **System magazynowy** — docelowe źródło stanu i cennika.

## Legacy

| Legacy | Co przejmuje moduł | Co znika |
|---|---|---|
| sPart: arkusz stanu, WKLEJ wyceny, kursy przez Power Query, łącze do cennika | import stanu, wyceny, cennika; kurs przy wycenie | wklejanie, dopasowanie po kolejności, ochrona cen nazwami kolumn |

## Stan

2026-11-26: brak kodu.

## Otwarte pytania

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Kurs waluty: z dnia wyceny? | Zakupowiec, Kierownik Logistyki |
| Wzór arkusza wyceny zakupów (do mapowania importu) | Zakupowiec |
| Czy import stanu magazynu zawiera dane opakowań? | Magazyn, drugi developer |

## Decyzje

- [ADR-0008](../../../decyzje/ADR-0008%20Pliki%20źródłowe%20w%20bazie%20danych.md) Pliki źródłowe w bazie danych
