---
status: roboczy
zakres: analiza techniczna systemu plików Excel (legacy)
przegląd: 2026-11-23
---

# Analiza techniczna plików Part

Opis działania makr VBA, formuł i comparatora w obecnym systemie Excel. Materiał do potwierdzenia przez autora plików — przygotowany na podstawie kodu wyciągniętego z plików `.xlsm` (tylko odczyt) i notatek z wywiadu [Wywiad obieg plików](../Wywiad%20obieg%20plików.md).

Zacznij od syntezy: **[wspolzaleznosci.md](wspolzaleznosci.md)** — mapa przepływów między plikami, rozbieżności z wywiadem, zbiorcza lista błędów i najważniejsze pytania.

| Dokument | Plik źródłowy | Zakres |
|---|---|---|
| [comparator.md](comparator.md) | Kod comparatora (Power Apps + Power Automate) | Porównanie BOM z P&P |
| [ipart.md](ipart.md) | iPart.xlsm | 37 modułów, 4 formularze |
| [fusepart.md](fusepart.md) | fusePart.xlsm | 15 modułów |
| [spart.md](spart.md) | sPart_v3.xlsm | 19 modułów, 1 formularz |
| [cpart.md](cpart.md) | cPart_v3.xlsm | 21 modułów, 6 formularzy |
| [dpart.md](dpart.md) | dPart.xlsm, dPart_v3.f.xlsm | 10 modułów, 2 formularze; różnice wariantów |
| [pliki-bez-makr.md](pliki-bez-makr.md) | dataPart.xlsx, scrapPart.xlsx, entPart.xlsx | Arkusze i formuły |
| [qpart.md](qpart.md) | TS_KOSZTORYSANT_V1.3.xlsm | 28 modułów; model czasu i kosztu |

Każdy dokument ma tę samą budowę: przegląd modułów, mechanizmy (wejście, reguła, wynik), współzależności z innymi plikami, reguły biznesowe, odpowiedzi na pytania z wywiadu, ryzyka i pytania do autora.

Oznaczenia: **[zgodne z wywiadem]**, **[różnica względem wywiadu]**, **[nowe — nieomawiane]**, **[niepewne]**.

Dokumenty nie zawierają haseł, nazwisk, nazw Klientów, ścieżek sieciowych ani stawek. Pliki źródłowe i wyciągnięty kod leżą w `zrodla/` (poza repozytorium).
