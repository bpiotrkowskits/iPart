---
status: propozycja
zakres: moduł Towar powierzony (obszar Zaopatrzenie)
przegląd: 2026-11-26
kod: brak
---

# Towar powierzony

## Cel

Moduł przygotowuje dla Klienta listę komponentów, które Klient ma dostarczyć (towar powierzony), i przyjmuje odesłaną przez Klienta listę jako dane o dostawie.

## Odpowiedzialność

| Posiada | Nie posiada (i kto posiada) |
|---|---|
| lista komponentów powierzanych dla zestawienia zakupowego (generowana na żądanie) | które linie są powierzone, ilości → Zestawienie zakupowe |
| import odesłanej listy (kopia pliku) | MPN zatwierdzone linii → BOM pakietu |
| ostrzeżenia o ilości od Klienta | zapas dla towaru powierzonego → Obudowy i reguły zapasu |
| | język listy → ustawienia Klienta (Usługi wspólne) |

## Model

```
Lista komponentów powierzanych (zestawienie; wygenerowana: kto, kiedy; język)
Odesłana lista (plik; kto, kiedy) → wiersze: MPN od Klienta, ilość od Klienta
```

Tabele w schemacie `purchasing`.

## Reguły

1. **Jedna lista na zestawienie zakupowe; zapas liczony od sumy** ilości powierzonych (reguła „od dołu”, wartości dla towaru powierzonego).
2. **Ilość powierzona tylko z linii powierzonych.** Pozycja mieszana (powierzona w jednym pakiecie, kupowana w innym) → pokazana zakupowcowi jako różnica do decyzji.
3. Pozycje do potwierdzenia przez Klienta oznaczone „do potwierdzenia przez Klienta”.
4. **Stały układ pliku**; język = ustawienie Klienta. **Kolumny O–Q** („Symbol”, „Nazwa PL”, „Result”) zostają bez zmian — wymaga ich importer do systemu magazynowego.
5. **Import odesłanej listy:** MPN podany przez Klienta = zatwierdzony przez Klienta, z podstawą „lista towaru powierzonego od Klienta”, dopisany na stałe do BOM pakietu. Ilość od Klienta mniejsza niż z zapasem → ostrzeżenie dla zakupowca.

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| komenda | Wygeneruj listę | dla zestawienia zakupowego |
| komenda | Importuj odesłaną listę | plik od Klienta |
| zdarzenie publikowane | MPN od Klienta | do BOM pakietu |

## Zależności

- **Zestawienie zakupowe** — linie powierzone, ilości.
- **Obudowy i reguły zapasu** — zapas dla towaru powierzonego.
- **BOM pakietu** — przyjmuje MPN zatwierdzone przez Klienta.
- **Importer systemu magazynowego** (drugi developer) — czyta kolumny O–Q.

## Legacy

| Legacy | Co przejmuje moduł | Co znika |
|---|---|---|
| entPart (plik), makro listy w iPart i fusePart | lista, układ, import odesłanej listy | język z listy krajów, cała ilość pozycji przy powierzeniu tylko w jednym pakiecie, inny sposób wyboru progu |

## Stan

2026-11-26: brak kodu.

## Decyzje

- [ADR-0009](../../../decyzje/ADR-0009%20Zestawienie%20zakupowe%20zamiast%20osobnej%20ścieżki%20łączenia.md) Zestawienie zakupowe zamiast osobnej ścieżki łączenia
