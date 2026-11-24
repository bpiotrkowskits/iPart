---
status: obowiązuje
zakres: cała dokumentacja iPart
przegląd: 2026-11-24
---

# Zasady dokumentacji

Dokumentacja iPart przyjmuje **standard dokumentacji ProduMan** (zasady ProduMan, przegląd 2026-11-13). Treść zasad poniżej jest jego kopią. Zmieniliśmy ją tylko tam, gdzie iPart działa inaczej. Różnice zebrane są w rozdziale 9. Przy zmianie standardu po stronie ProduMan porównujemy obie wersje i świadomie przenosimy zmiany.

---

## 1. Poziomy i typy dokumentów

Dokumentacja ma trzy poziomy: **system → obszar → moduł**. Każdy dokument należy do dokładnie jednego poziomu i ma jedną funkcję.

| Typ | Poziom | Funkcja | Gdzie | Szablon |
|---|---|---|---|---|
| Instrukcja główna | — | punkt wejścia, struktura, zasady w skrócie | `README.md` | — |
| Opis systemu | system | kontekst, mapa obszarów, architektura techniczna, kierunek i etapy rozwoju, środowiska | `system/` | — |
| Słownik | system | znaczenie pojęć | `system/03 Słownik.md` | — |
| Mapa obszaru | obszar | cel, granica, moduły, relacje, przebiegi, pytania | `obszary/<obszar>/00 Mapa obszaru.md` | [Mapa obszaru](szablony/Mapa%20obszaru.md) |
| Kontrakt zewnętrzny | obszar | co obszar przyjmuje i publikuje na zewnątrz | `obszary/<obszar>/01 Kontrakt zewnętrzny.md` | [Kontrakt zewnętrzny](szablony/Kontrakt%20zewnętrzny.md) |
| Karta modułu | moduł | model, reguły, interfejs, zależności, legacy, stan | `obszary/<obszar>/moduly/<Moduł>.md` | [Karta modułu](szablony/Karta%20modułu.md) |
| Standard | przekrój | zasady techniczne obowiązujące wszystkie moduły | `standardy/` | — |
| Decyzja (ADR) | dowolny | jedna istotna decyzja: kontekst, wybór, konsekwencje | `decyzje/` | [Decyzja](szablony/Decyzja.md) |
| Materiał roboczy | obszar | notatki z wywiadów, analizy, mapy systemów zewnętrznych | `obszary/<obszar>/robocze/` | — |
| Materiał roboczy przekrojowy | system | materiał roboczy dotyczący kilku obszarów naraz (wywiad, analiza systemu Excel, przegląd kodu VBA) | `robocze/` | — |

**Materiał roboczy jest niewiążący.** Gdy jego treść zostanie zweryfikowana, przenosimy ją do właściwego dokumentu, a materiał oznaczamy jako wykorzystany lub usuwamy.

## 2. Nagłówek dokumentu

Każdy dokument zaczyna się od nagłówka YAML:

```yaml
---
status: propozycja        # szkic | propozycja | obowiązuje | zastąpiony | roboczy
zakres: obszar Zaopatrzenie  # czego dotyczy dokument
przegląd: 2026-11-24      # data ostatniej weryfikacji treści
zastąpiony-przez:         # tylko przy statusie "zastąpiony"
---
```

| Status | Znaczenie |
|---|---|
| `szkic` | treść niepełna, w trakcie pisania |
| `propozycja` | treść kompletna, czeka na potwierdzenie (biznesowe lub techniczne) |
| `obowiązuje` | zatwierdzona; kod i inne dokumenty muszą być z nią zgodne |
| `zastąpiony` | nieaktualny; wskazuje następcę i trafia do `_archiwum/` |
| `roboczy` | materiał roboczy, niewiążący |

## 3. Zasady treści

1. **Jeden fakt — jedno miejsce.** Status modułu, definicja pojęcia, reguła — każde zapisane raz. Inne dokumenty linkują.
2. **Jeden dokument — jedna funkcja.** Model pozostaje w karcie modułu. Cel, zależności i zbiorczy stan etapów należą do dokumentu kierunkowego; nie łączymy ich z backlogiem ani dziennikiem sesji.
3. **Opisujemy stan obowiązujący.** Zamierzony kierunek jest dozwolony, ale jako dokument o statusie `propozycja` albo jako wyraźnie oznaczona sekcja „Kierunek”.
4. **Stan implementacji krótko.** Karta modułu ma sekcję „Stan”: kilka zdań z datą. Nie prowadzimy szczegółowych trackerów zadań ani checklist kolejnych sesji.
5. **Pojęcia ze Słownika.** Nowe pojęcie najpierw dopisujemy do [Słownika](../system/03%20Słownik.md), potem używamy. Pojęć zdefiniowanych w Słowniku ProduMan nie definiujemy ponownie — Słownik iPart odsyła do nich (rozdział 9).
6. **Rozmiar.** Dokument dłuższy niż ok. 400 linii dzielimy według funkcji.
7. **Wyższy poziom nie powtarza niższego.** Mapa obszarów wymienia obszary; szczegóły modułów są tylko w kartach modułów.

### Wywiad, przegląd kodu i implementacja

- Wywiady z użytkownikami i przegląd kodu VBA dają materiał roboczy: procesy, przykłady, warianty i pytania. Materiał roboczy nie wyznacza automatycznie modułu ani reguły.
- Zatwierdzony wynik zamkniętego fragmentu wywiadu lub przeglądu trafia zwięźle do właściwej karty modułu, a istotna zmiana architektury do ADR. Nie kopiujemy całej treści w obie strony.
- Sprzeczność między materiałem roboczym a dokumentem trzeba ujawnić i rozstrzygnąć, nie stosować automatycznej zasady „nowszy zapis wygrywa”.
- Definicje klas, pól i szczegółowe scenariusze testów pozostają w kodzie i testach. Dokumentacja utrzymuje znaczenie, własność i kluczowe reguły, bez równoległego modelu technicznego.
- Zatwierdzenie reguły nie oznacza jej wdrożenia. Stan domeny, podłączenie w aplikacji i migracja danych są rozróżnione w krótkiej sekcji „Stan”.

## 4. Decyzje

- **Jeden rejestr** dla całego systemu iPart: [`decyzje/`](../decyzje/README.md), numeracja ciągła `ADR-0001`, `ADR-0002`… Numeracja jest niezależna od rejestru ProduMan. Odwołując się do decyzji ProduMan, piszemy „ADR-NNNN ProduMan”.
- **Numer nowego ADR** to kolejny wolny numer w rejestrze. Przed nadaniem numeru sprawdzamy, czy na innych gałęziach nie ma nowszych ADR.
- **ADR zapisujemy**, gdy decyzja zmienia granice obszarów lub modułów, kontrakty, model danych, technologię albo sposób pracy — i gdy ktoś za pół roku zapyta „dlaczego”.
- **Drobne ustalenia** modułu (reguła walidacji, nazwa pola) trafiają do sekcji „Reguły” karty modułu jako stan, nie jako historia.
- **Przyjętej decyzji nie edytujemy merytorycznie.** Zmiana = nowy ADR, który zastępuje poprzedni; stary dostaje status `zastąpiona` i link do następcy.
- **Doprecyzowanie.** ADR, który zmienia tylko część wcześniejszej decyzji, ma w nagłówku `doprecyzowuje: ADR-XXXX (pkt N)`; wcześniejsza zostaje `przyjęta` i dostaje `doprecyzowana-przez:` — gdy nowy ADR jest `proponowana`, dopiero po jego akceptacji.
- Każdy ADR ma datę, status i zakres (system / obszar / moduł).
- **Decyzje robocze.** Gdy pytanie trzeba rozstrzygnąć, żeby dokumentacja mogła powstawać dalej, a decydujący nie jest dostępny, piszący zapisuje rozstrzygnięcie jako decyzję roboczą — w sekcji „Decyzje robocze (do akceptacji)” karty modułu, mapy obszaru lub kontraktu, bezpośrednio przed „Otwarte pytania”. Decyzja robocza, która spełnia kryteria ADR, dostaje ADR o statusie `proponowana`.
  - Inne dokumenty mogą się na niej opierać, ale nie jest poleceniem do wdrożenia.
  - Po akceptacji treść zostaje w regułach lub modelu dokumentu, punkt znika z sekcji, a ADR dostaje status `przyjęta`.
  - Po odrzuceniu ADR dostaje status `odrzucona`, a dokumenty, które się na niej opierały, są poprawiane.

## 5. Czego nie umieszczamy w dokumentacji

| Nie umieszczamy | Gdzie zamiast tego |
|---|---|
| backlog, listy zadań, plan na następną sesję | narzędzie do zadań (do ustalenia) |
| prompty dla agentów AI, transkrypty sesji | poza repozytorium dokumentacji |
| logi testów, wyniki ręcznych przebiegów | narzędzie do zadań / opis zmiany (commit, PR) |
| skrypty SQL, dane migracyjne | repozytorium kodu (`src/`, `deploy/`) |
| dane Klientów, nazwy projektów, imiona i nazwiska pracowników | nigdzie — zamiast nich rola albo kod tymczasowy |
| zrzuty ekranu bez kontekstu | obraz tylko jako ilustracja w dokumencie, w `_zasoby/` obok niego |

## 6. Nazewnictwo i format

- **Język:** polski. Nazwy z kodu (klasy, tabele, projekty) po angielsku, w `backtickach`.
- **Katalogi:** małe litery, bez polskich znaków, słowa łączone myślnikiem: `dokumentacja-pakietu`.
- **Pliki:** czytelna nazwa po polsku; dokumenty o ustalonej kolejności z prefiksem `NN ` (`00 Mapa obszaru.md`). Bez znaku „—” w nazwach plików.
- **Linki:** Markdown, ścieżki względne, spacje jako `%20`: `[Słownik](../system/03%20Słownik.md)`. Nie używamy wikilinków `[[...]]`.
- **Diagramy:** Mermaid w bloku kodu.
- **Nazwy obszarów i modułów** w dokumentacji po polsku; nazwa projektu w kodzie podana w karcie modułu.

## 7. Utrzymanie

- Zmiana w kodzie, która zmienia opisane zachowanie, **aktualizuje dokument w tym samym commicie**.
- Przy każdej merytorycznej zmianie aktualizujemy pole `przegląd`.
- Dokument nieaktualny dostaje status `zastąpiony` i trafia do `_archiwum/`.
- Przy przenoszeniu wiedzy z materiałów roboczych i z systemu Excel: weryfikujemy z kodem i biznesem, zapisujemy w nowym miejscu, nie kopiujemy hurtem.

## 8. Praca z agentami AI

- Agent zaczyna od [README](../README.md) i [Słownika](../system/03%20Słownik.md).
- Agent nie tworzy nowych typów dokumentów ani katalogów spoza tych zasad.
- Agent nie traktuje dokumentów o statusie `szkic`/`propozycja`, materiałów roboczych, decyzji roboczych ani ADR o statusie `proponowana` jako polecenia do wdrożenia.
- Agent zgłasza niespójności zamiast je po cichu poprawiać.
- Agent nie zapisuje w dokumentacji pamięci sesji, promptów ani list zadań.
- Agent nie zapisuje danych Klientów, nazw projektów ani imion i nazwisk pracowników.
- Dokumentacja ProduMan jest dla agenta źródłem pojęć wspólnych i kontraktów ProduMan. Agent nie zmienia jej i nie przenosi z niej treści bez polecenia.

## 9. Różnice względem standardu ProduMan

| Temat | ProduMan | iPart |
|---|---|---|
| Materiał roboczy przekrojowy | tylko w obszarach | dodatkowo katalog `robocze/` dla materiałów obejmujących kilka obszarów (wywiad, analiza systemu Excel, przegląd kodu VBA) |
| Słownik | jedyne źródło pojęć ProduMan | pojęcia wspólne z ProduMan **nie są definiowane ponownie** — Słownik iPart odsyła do Słownika ProduMan. Przy kolizji znaczeń zmieniamy nazwę po stronie iPart (np. „Nadwyżka zakupowa”) |
| Legacy | aplikacje WinForms, `TS_Base API`, TaskMan | system plików Excel/VBA (comparator, iPart, sPart…) i comparator w Power Apps |
| Numer ADR | skrypt `scripts/adr.ps1` | kolejny wolny numer w rejestrze; rejestr niezależny od ProduMan |
| Warsztaty | tablica Miro | wywiady i przegląd kodu VBA (materiał roboczy) |
| Dane wrażliwe | — | bez danych Klientów, nazw projektów, imion i nazwisk pracowników |
