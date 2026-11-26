---
status: przyjęta
data: 2026-11-26
zakres: obszar Zaopatrzenie, obszar Kompletacja
zastępuje:
zastąpiona-przez:
---

# ADR-0009 Zestawienie zakupowe zamiast osobnej ścieżki łączenia

## Kontekst

- Dziś są dwie ścieżki: pojedynczy ZK (iPart → sPart → cPart) i łączenie (fusePart → sPart → cPart, osobny szablon dPart). Mechanizmy partii, ilości i eksportu istnieją podwójnie.
- fusePart jest osobnym plikiem z własnymi listami MPN i zatwierdzeniem; decyzje z niego nie wracają do iPart.
- Ustalono ([Decyzje VBA](../robocze/Decyzje%20VBA.md), 3.1–3.7): łączenie to zestawienie bez własnych decyzji o BOM; decyzje dotyczą zawsze BOM albo BOM dla ZK; łączenie tylko w obrębie jednego Klienta.

## Decyzja

1. **Jedno pojęcie: zestawienie zakupowe** z jednym albo kilkoma ZK jednego Klienta. Zestawienie z kilkoma ZK ma numer `FUSEnnn/rrrr` i opcjonalną nazwę; z jednym ZK — pokazywane po numerze ZK.
2. Zestawienie przechowuje: ZK, użyte wersje BOM i ilości; pozycje zakupu z decyzjami „połącz / nie łącz” (z powodem); partie z kompletacją. **Linie, MPN i zamienniki zawsze pochodzą z BOM pakietów.**
3. **Jeden ZK — tylko w jednym zestawieniu, w całości.** Zakup i kompletację etapami obsługują partie i kolejne ZD.
4. **ZD to odnośnik na pozycji zakupu** (numer, ilość, LT). Jedno ZD obejmuje wiele zestawień, jedna pozycja może mieć kilka ZD. ZD powstają w innym systemie.
5. Zestawienie z kilkoma ZK tworzą zakupowiec, Opiekun Projektu albo Kierownik działu Obsługi Projektu.

## Rozważane warianty

- **Łączenie jako osobny dokument z własnymi decyzjami** (jak fusePart) — odrzucony: decyzje nie wracałyby do jednego źródła danych pakietu.
- **Osobne ścieżki dla pojedynczego ZK i łączenia** — odrzucony: podwójne mechanizmy partii, kompletacji i dPart.

## Konsekwencje

- Łączenie BOM-ów należy do obszaru Zaopatrzenie ([Mapa obszarów](../system/02%20Mapa%20obszarów.md)).
- Partie i kompletacja działają tak samo dla jednego i wielu ZK; partia ma ilość dla każdego ZK.
