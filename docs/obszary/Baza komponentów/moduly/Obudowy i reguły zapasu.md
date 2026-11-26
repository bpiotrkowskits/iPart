---
status: propozycja
zakres: moduł Obudowy i reguły zapasu (obszar Baza komponentów)
przegląd: 2026-11-26
kod: brak
---

# Obudowy i reguły zapasu

## Cel

Moduł prowadzi słowniki obudów (Case) i zasady z nimi związane: które nazwy obudów są równoważne, jak powinna wyglądać nazwa obudowy i ile komponentów kupujemy ponad BOM dla danej obudowy.

## Odpowiedzialność

| Posiada | Nie posiada (i kto posiada) |
|---|---|
| słownik obudów, licznik użycia obudowy | Case konkretnego komponentu → Komponenty |
| równoważne nazwy obudów (rodziny) | wyliczenie ilości do zamówienia → Zaopatrzenie |
| wzorce nazewnictwa obudów | ekran panelu administratora → Usługi wspólne |
| reguły zapasu: grupy, przypisanie obudowy do grupy, progi — osobno zakupy i towar powierzony | |
| zapamiętane przypisania obudów do grup (podpowiedzi) | |

## Pojęcia

Ze [Słownika](../../../system/03%20Słownik.md): Case, reguła zapasu, towar powierzony (patrz lista komponentów powierzanych).

Pojęcia lokalne:
- **Grupa zapasu** — zbiór obudów o tych samych progach zapasu.
- **Próg** — para „ilość od → zapas” w grupie.
- **Wzorzec nazewnictwa** — reguła zapisu nazwy obudowy (w dataPart: stdCase).

## Model

```
Obudowa (kod; nazwa; wzorzec; licznik użycia)
Rodzina równoważnych nazw (obudowy uznawane przy porównaniu za te same)
Wzorzec nazewnictwa
Grupa zapasu (rodzaj: zakupy / powierzone)
 ├─ Progi (ilość od → zapas)
 └─ Przypisanie obudowy (kto, kiedy, przy jakim ZK; potwierdzone / z podpowiedzi)
```

Tabele w schemacie `components` ([ADR-0005](../../../decyzje/ADR-0005%20Schemat%20bazy%20na%20obszar.md)).

## Reguły

1. **Wybór progu „od dołu”:** obowiązuje największy próg nie większy niż ilość — jednakowo dla zakupów i towaru powierzonego.
2. **Dwa zestawy wartości** (zakupy, powierzone) zostają.
3. **Reguły zapasu edytują** Kierownik Logistyki i Kierownik działu Obsługi Projektu.
4. **Obudowa bez grupy** → ostrzeżenie i decyzja zakupowca, zapamiętana jako podpowiedź (obudowa, przypisanie, kto, kiedy, przy jakim ZK). Grupa „nieprzypisane” tylko jako ostateczność.
5. **Równoważne nazwy obudów** są używane przy porównaniu Case (zamienniki, łączenie pozycji).
6. **Wzorce nazewnictwa** na razie tylko ostrzegają przy niezgodnym Case.
7. **Grupowa zmiana nazwy obudowy** działa wszędzie, z listą „było → będzie” do zatwierdzenia.
8. **Słowniki i wzorce edytuje tylko Kierownik działu Obsługi Projektu** (panel administratora).
9. **Do wersji alfa obudowy zostają tekstem.** Dopasowanie obudów do wzorców i automatyczna zmiana nazw po zmianie wzorca — po alfie (sesja 6, razem z qPart).

### Migracja

10. Grupy i przypisania z dataPart: zakupy — przypisania wątpliwe do raportu; powierzone — tylko potwierdzone, reszta decyzją przy użyciu.
11. Równoważne nazwy (altCase) i wzorce (stdCase) — do słowników.

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| zapytanie | Zapas dla obudowy | Case, ilość, rodzaj (zakupy / powierzone) → zapas albo „brak reguły” |
| zapytanie | Czy obudowy równoważne | dla porównania Case |
| komenda | Przypisz obudowę do grupy | zakupowiec; zapamiętywane |
| komenda | Zmień nazwę obudowy grupowo | Kierownik działu Obsługi Projektu; lista „było → będzie” |

## Zależności

- **Komponenty** — Case komponentu.
- **Zaopatrzenie** — korzysta z zapasu; zgłasza przypisania obudów.
- **Usługi wspólne** — panel administratora, dostępy, historia.

## Legacy

| Legacy | Co przejmuje moduł |
|---|---|
| scrapPart (dawniej „Obudowy”), supplyScrap, entScrap | grupy, progi, przypisania |
| dataPart: altCase | równoważne nazwy obudów |
| dataPart: stdCase | wzorce nazewnictwa |
| dataPart: qtyCase | licznik użycia (liczony automatycznie) |

Znika: dwa różne sposoby wyboru progu (sPart i makro listy powierzonych), łącze do pliku „Obudowy.xlsx”, wynik „bł.” dla obudowy spoza tabeli.

## Stan

2026-11-26: brak kodu. W 0.1.0 obudowa jest polem tekstowym komponentu.

## Otwarte pytania

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Dopasowanie obudów do wzorców i automatyczna zmiana nazw | sesja 6 (po alfie) |

## Decyzje

- [ADR-0005](../../../decyzje/ADR-0005%20Schemat%20bazy%20na%20obszar.md) Schemat bazy na obszar
