---
status: propozycja
zakres: moduł Ustawienia i panel administratora (obszar Usługi wspólne)
przegląd: 2026-11-26
kod: częściowo — src/iPart.App/Resources/DataColors.xaml
---

# Ustawienia i panel administratora

## Cel

Moduł przechowuje Klientów i ustawienia, które zmieniają zachowanie aplikacji (firma → Klient → pakiet → linia), oraz daje Kierownikowi działu Obsługi Projektu jedno miejsce do zarządzania słownikami i kolorami.

## Odpowiedzialność

| Posiada | Nie posiada (i kto posiada) |
|---|---|
| Klient (GUID, nazwa, kod kraju, powiązanie z kontrahentem Nexo) | dane słowników obudów i reguł zapasu → Baza komponentów |
| ustawienia: firma → Klient → pakiet → linia | ustawienia wyświetlania na poziomie komponentu → Komponenty |
| kolory danych | dostępy → Użytkownicy i dostępy |
| ekran panelu administratora | |

## Pojęcia

Ze [Słownika](../../../system/03%20Słownik.md): ustawienia Klienta, Part TS.

## Model

```
Klient (GUID; nazwa; kod kraju; język listy towaru powierzonego; powiązanie z kontrahentem Nexo)
Ustawienie (poziom: firma / Klient / pakiet / linia; klucz; wartość)
Kolor danych (znaczenie; widok/dział; kolor tła; kolor tekstu)
```

Tabele w schemacie `shared`.

## Reguły

1. **Lista Klientów:** jednorazowy import + ręczne dopisywanie; później integracja z innymi systemami firmy.
2. **Ustawienia działają kolejno:** firma → komponent → Klient → pakiet → linia (niższy poziom nadpisuje wyższy). Przykłady ustawień Klienta: PPM zawsze w Part TS, MPN zawsze (także RC), język listy towaru powierzonego.
3. **Zmiana ustawień w istniejących pakietach** → lista „było → będzie” do zatwierdzenia.
4. **Panel administratora — tylko Kierownik działu Obsługi Projektu:** równoważne nazwy obudów, wzorce nazewnictwa, grupowa zmiana nazwy obudowy, kolory danych.
5. **Kolor zależy od działu (widoku)** — to samo znaczenie może mieć inny kolor w zakupach i w magazynie. Kolor nie jest jedyną informacją: pełna informacja w podpowiedzi po najechaniu.
6. **Kolory danych są takie same w trybie jasnym i ciemnym.**
7. **Kolejność, gdy pasuje kilka znaczeń:** DNP → niepotwierdzona → montaż inny niż SMT → sposób wydania. Statusy MPN i kompletacji kolorują tylko swoje komórki.

### Paleta domyślna

| Znaczenie | Zakupy | Kompletacja i widok dla produkcji |
|---|---|---|
| linia niepotwierdzona | F4B183 | F4B183 |
| DNP | C9C9C9 | C9C9C9 |
| montaż THT / Press-Fit / MECH / Plugs | BFFF9F | BFFF9F |
| bezpośrednio do Klienta | F7F5A3 | F7F5A3 |
| MPN zgodny z BOM / z parametrami / Generic | A9D18E | bez koloru |
| MPN zatwierdzony (Klient, TS) | A9D18E | 9DC3E6 |
| zamiennik tylko dla ZK | 9DC3E6 | 9DC3E6 |
| do weryfikacji | FFD966 | FF0000 + blokada przypisania opakowań |
| Niedozwolony | czarne tło, czerwony tekst | czarne tło, czerwony tekst |
| kompletna / częściowo / brak | — | 70AD47 / FFC000 / FFC7CE z tekstem 9C0006 |
| wydane ponad zapotrzebowanie (THT, MECH, Press-Fit, Plugs) | — | FF5050 |

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| zapytanie | Ustawienie obowiązujące | klucz + kontekst (Klient, pakiet, linia) → wartość |
| zapytanie | Kolor danych | znaczenie + widok |
| komenda | Dodaj Klienta | |
| komenda | Zmień ustawienie | z listą „było → będzie” dla istniejących pakietów |

## Zależności

- **Użytkownicy i dostępy** — dostęp do panelu.
- **Baza komponentów** — słowniki edytowane w panelu.

## Legacy

| Legacy | Co przejmuje moduł | Co znika |
|---|---|---|
| reguły pod konkretnych Klientów wpisane w kod VBA | ustawienia Klienta | — |
| formatowanie warunkowe w plikach | kolory danych | reguły sprawdzające złą kolumnę, różne nazwy statusów |
| lista Klientów z kodem kraju (dataPart) | Klient | Klient i projekt z nazw folderów |

## Stan

2026-11-26: paleta kolorów w `src/iPart.App/Resources/DataColors.xaml` (stała, bez edycji w panelu); kolorowane są linie niepotwierdzone i komponenty o montażu innym niż SMT. Klient w 0.1.0 ma identyfikator liczbowy — do zmiany na GUID. Brak ustawień i panelu.

## Decyzje

- [ADR-0006](../../../decyzje/ADR-0006%20Stałe%20identyfikatory%20i%20powiązania%20zewnętrzne.md) Stałe identyfikatory
