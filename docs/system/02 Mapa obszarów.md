---
status: propozycja
zakres: system
przegląd: 2026-11-25
---

# Mapa obszarów

System dzieli się na **obszary**, czyli duże części odpowiadające sposobom pracy w firmie. Obszar składa się z modułów i komunikuje się z innymi obszarami tylko przez swój kontrakt zewnętrzny. Ten dokument opisuje obszary i relacje między nimi. Moduły są opisane w mapach obszarów.

## Obszary

| Obszar | Odpowiada za | Legacy (pliki Excel) | Dokumentacja |
|---|---|---|---|
| **Dokumentacja pakietu** | import i porównanie BOM i P&P Klienta, potwierdzenie linii, BOM pakietu (Part TS, MPN zatwierdzone i niedozwolone, zamienniki), zatwierdzenie P&P i BOM, wersje BOM | comparator, iPart | do utworzenia |
| **Baza komponentów** | komponenty i ich parametry, Case, Footprint, sposób montażu, powiązanie MPN z symbolem magazynowym, słowniki | dataPart | do utworzenia |
| **Zaopatrzenie** | zapotrzebowanie i zakupy komponentów pod ZK, łączenie BOM-ów do wspólnych zakupów, zgłoszenia zamienników od zakupowca, towar powierzony, reguły zapasu, wycena, nadwyżka zakupowa | sPart, fusePart, entPart, scrapPart | do utworzenia |
| **Kompletacja** | kompletacja komponentów pod ZK i PP, desygnacja opakowań przy wydaniu na produkcję | cPart, dPart | do utworzenia; zakres czeka na decyzję o przejęciu (zasada 5) |
| **Estymacja** | szacowanie czasu montażu pakietu | qPart (niedokończony) | do utworzenia; zakres czeka na decyzję o przejęciu (zasada 5) |
| **Usługi wspólne** | użytkownicy, role i administracja dostępów (edycja / odczyt / brak), historia zmian, blokady edycji, powiadomienia, ustawienia firmy i Klienta | — (dziś w każdym pliku osobno) | do utworzenia |

## Relacje

Diagram pokazuje kierunek docelowy. Strzałka nie oznacza działającej integracji.

```mermaid
flowchart LR
    K[/Pliki Klienta/]
    Nexo[(Nexo)]
    WMS[(System magazynowy<br/>+ kompletor)]
    PM[ProduMan]

    Dok[Dokumentacja pakietu]
    Baza[Baza komponentów]
    Zao[Zaopatrzenie]
    Kom[Kompletacja]
    Est[Estymacja]
    Wsp[Usługi wspólne]

    K -- BOM, P&P --> Dok
    Baza -- dane komponentów --> Dok
    Dok -- nowe MPN, zatwierdzony Footprint --> Baza
    Dok -- zatwierdzona wersja BOM --> Zao
    Zao -- propozycja zamiennika --> Dok
    Dok -- zatwierdzona wersja BOM --> Kom
    Zao -- zamówione i dostarczone komponenty --> Kom
    Baza -- PADS, sposób montażu --> Est
    Nexo -. ZK, ZD, stany .-> Zao
    Kom <-. kompletacja .-> WMS
    Est -. estymacja .-> PM
    Kom -. gotowość materiałowa .-> PM
    Wsp --- Dok
    Wsp --- Zao
    Wsp --- Kom
```

## Zasady między obszarami

1. **Dokumentacja pakietu jest jedynym właścicielem BOM pakietu**: linii, Part TS, MPN zatwierdzonych i niedozwolonych oraz wersji BOM. Inne obszary nie zmieniają BOM. Decyzja podjęta w innym obszarze, np. zamiennik od zakupowca, wraca do BOM jako zgłoszenie do Dokumentacji pakietu. Tak realizujemy cel „jedno źródło danych pakietu” z [Kontekstu](01%20Kontekst%20i%20cel%20systemu.md).
2. **Baza komponentów jest właścicielem danych komponentu**: parametrów, Case, Footprint i sposobu montażu. Dokumentacja pakietu czyta te dane i zgłasza zmiany, np. nowe MPN albo Footprint po zatwierdzeniu P&P. Zgłoszenia nie nadpisują bazy po cichu.
3. **Zaopatrzenie i Kompletacja pracują tylko na zatwierdzonej wersji BOM.** Nie widzą wersji roboczej ani zamienników z wstrzymaną decyzją.
4. **Obszar komunikuje się z innymi tylko przez swój kontrakt zewnętrzny.** Właścicielem kontraktu jest obszar, który go wystawia.
5. **Kompletacja i Estymacja są obszarami do wydzielenia.** ProduMan przewiduje, że gospodarkę materiałową (obszar MRP) i estymację przejmie inny system. Nie wiadomo jeszcze, czy będzie to iPart, czy moduł BOM systemu magazynowego. Do czasu decyzji oba obszary w iPart opisujemy na poziomie mapy i kontraktu, bez kart modułów.

## Decyzje robocze (do akceptacji)

1. **Zamiennik od zakupowca zapisuje się w BOM pakietu (Dokumentacja pakietu), a nie w Zaopatrzeniu.** Zamiennik o takich samych lub lepszych parametrach jest akceptowany automatycznie; inny zatwierdza zakupowiec z podaniem podstawy. Zakupowiec wybiera zakres: BOM pakietu albo wersja BOM dla ZK ([Decyzje VBA](../robocze/Decyzje%20VBA.md), 2.9 i 4.1). Zaopatrzenie wysyła zgłoszenie, Dokumentacja pakietu zapisuje zmianę.
2. **Łączenie BOM-ów należy do Zaopatrzenia** i jest zestawieniem bez własnych decyzji o BOM: przechowuje ZK, wersje BOM, ilości i decyzje „połącz / nie łącz”. Łączenie tylko w obrębie jednego Klienta ([Decyzje VBA](../robocze/Decyzje%20VBA.md), 3.1–3.7).

## Otwarte pytania

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Kto przejmie Kompletację i Estymację: iPart czy moduł BOM systemu magazynowego? | Kierownik działu Obsługi Projektu, developer IT, drugi developer |
| Jak Kompletacja iPart ma się do obszaru MRP ProduMan (zlecenie kompletacji, zgłoszenie braku)? Czy cPart i dPart to ta sama kompletacja? | developer IT, Kierownik działu Obsługi Projektu |
| Jak iPart czyta ZK, ZD i stany z Nexo: widoki SQL, API, pliki? | developer IT |
| Czy PP z plików Excel odpowiada ZP ProduMan? | Kierownik działu Obsługi Projektu, developer IT |
| Jak pakiet w iPart wiąże się z `EssegiId` w dokumentacji pakietów? | drugi developer |

## Powiązane

- [Kontekst i cel systemu](01%20Kontekst%20i%20cel%20systemu.md)
- [Słownik](03%20Słownik.md)
- [Rejestr decyzji](../decyzje/README.md)
