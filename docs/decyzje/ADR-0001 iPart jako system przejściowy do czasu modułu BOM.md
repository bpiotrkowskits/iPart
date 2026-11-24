---
status: proponowana
data: 2026-11-24
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0001 iPart jako system przejściowy do czasu modułu BOM

## Kontekst

- Dokumentacja BOM pakietów, baza komponentów, zakupy i kompletacja działają dziś w plikach Excel/VBA. Dane rozjeżdżają się między plikami, a decyzje z późniejszych etapów nie wracają do BOM.
- System magazynowy drugiego developera ma przejąć BOM (moduł BOM w budowie). Prace nad tym modułem bardzo się opóźniają, a termin nie jest znany.
- ProduMan ma podobną sytuację z gospodarką materiałową: obszar MRP buduje „tak, jakby już był zewnętrzny”, a docelowo przejmie go system magazynowy (ADR-0038 ProduMan). ProduMan przewiduje też wydzielenie estymacji.
- Moduł BOM, obszar MRP ProduMan i dokumentacja pakietów (`EssegiId`) częściowo pokrywają się z zakresem iPart.

## Decyzja

1. **iPart jest osobnym systemem** z własną bazą i własną dokumentacją w standardzie ProduMan. Z ProduManem, systemem magazynowym i dokumentacją pakietów łączy się tylko przez kontrakty.
2. **iPart jest formą przejściową**, która pozwala wyjść z Excela. Działa do czasu, gdy BOM i dane materiałowe przejmie system magazynowy.
3. **iPart budujemy tak, żeby przejęcie było tanie:**
   - pakiet, wersja BOM, linia BOM i komponent mają stałe identyfikatory publiczne, które przechodzą bez zmian przy migracji;
   - każda informacja ma jednego właściciela zapisu;
   - obszary komunikują się tylko przez kontrakty zewnętrzne; kontrakt danych BOM uzgadniamy z drugim developerem przed implementacją;
   - przy przejęciu dane migrują, a odbiorcy korzystają dalej z tego samego kontraktu.
4. **Kompletacja i Estymacja** są w iPart obszarami do wydzielenia. Decyzję, kto je przejmie (iPart czy moduł BOM systemu magazynowego), zapiszemy osobnym ADR.

## Rozważane warianty

- **iPart jako obszar ProduMan** — odrzucony: zakres BOM ma docelowo należeć do systemu magazynowego, a nie do ProduMan.
- **Czekanie na moduł BOM systemu magazynowego** — odrzucony: termin nieznany, a praca w Excelu dalej generuje błędy i rozbieżności.
- **Poprawianie plików Excel do czasu modułu BOM** — odrzucony ([ADR-0002](ADR-0002%20Pliki%20Excel%20nie%20są%20już%20poprawiane.md)).

## Konsekwencje

- iPart ma własną dokumentację: system, obszary, kontrakty i rejestr ADR, niezależny od ProduMan.
- Pojęcia wspólne bierzemy ze Słownika ProduMan ([Słownik](../system/03%20Słownik.md)).
- [Kontekst i cel systemu](../system/01%20Kontekst%20i%20cel%20systemu.md) opisuje warunki przyszłego przejęcia.
- Kontrakty obszarów Dokumentacja pakietu i Baza komponentów wymagają uzgodnienia z drugim developerem.
- Przejęcie oznacza drugą migrację danych: z Excela do iPart, a potem z iPart do systemu magazynowego. Druga jest tania tylko przy stałych identyfikatorach i uzgodnionym kontrakcie.

## Do weryfikacji

- Uzgodnienie z drugim developerem: czy moduł BOM przejmie BOM, bazę komponentów i zakupy, czy tylko część.
- Uzgodnienie z developerem IT: czy pkt 3 jest zgodny z kierunkiem ProduMan.
