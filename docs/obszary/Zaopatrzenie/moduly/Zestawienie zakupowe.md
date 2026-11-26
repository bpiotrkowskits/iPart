---
status: propozycja
zakres: moduł Zestawienie zakupowe (obszar Zaopatrzenie)
przegląd: 2026-11-26
kod: brak
---

# Zestawienie zakupowe

## Cel

Moduł zbiera ZK jednego Klienta kupowane razem i ustala dla każdej pozycji: który MPN kupujemy, ile potrzeba (BOM, zapas, magazyn) i pod jakim ZD. Zastępuje sPart i fusePart jedną ścieżką dla jednego i wielu ZK.

## Odpowiedzialność

| Posiada | Nie posiada (i kto posiada) |
|---|---|
| zestawienie: ZK, wersje BOM, ilości; numer `FUSEnnn/rrrr` i nazwa | linie BOM, MPN zatwierdzone i niedozwolone → BOM pakietu |
| pozycje zakupu i decyzje „połącz / nie łącz” (z powodem) | reguły zapasu → Obudowy i reguły zapasu |
| wybrany MPN i jego status, ilości: z BOM / zapas / z magazynu / do zamówienia | import stanu, ceny, kursy → Stan magazynu i wycena |
| dostawca, LT, odnośniki do ZD | lista komponentów powierzanych → Towar powierzony |
| zapamiętane decyzje łączenia (podpowiedzi) | partie i kompletacja → Kompletacja |

## Pojęcia

Ze [Słownika](../../../system/03%20Słownik.md): zestawienie zakupowe, pozycja zakupu, łączenie BOM-ów, ZK, ZD, LT, zamiennik, zamiennik jednorazowy, MPN zatwierdzone, MPN niedozwolone, nadmiar ze zmiany, reguła zapasu.

## Model

```
Zestawienie zakupowe (GUID; Klient; numer FUSEnnn/rrrr albo ZK; nazwa; operator)
 ├─ ZK w zestawieniu (ZK nnn/rrrr; wersja BOM; ilość)
 └─ Pozycja zakupu (połączone linie BOM; decyzja „połącz / nie łącz” z powodem;
                    wybrany MPN i status; ilości; dostawca; LT; cena, kurs; część powierzona)
     └─ ZD (numer z Nexo; ilość; LT) — jedno ZD może obejmować wiele zestawień
Zapamiętana decyzja łączenia (para komponentów; połącz / nie łącz; kto, kiedy)
```

Tabele w schemacie `purchasing` ([ADR-0005](../../../decyzje/ADR-0005%20Schemat%20bazy%20na%20obszar.md)). Partie należą do Kompletacji i wskazują ZK z zestawienia.

## Reguły

### Zestawienie

1. **Jedno pojęcie dla jednego i wielu ZK** ([ADR-0009](../../../decyzje/ADR-0009%20Zestawienie%20zakupowe%20zamiast%20osobnej%20ścieżki%20łączenia.md)). Z jednym ZK — pokazywane po numerze ZK; z kilkoma — `FUSEnnn/rrrr` (kolejny w roku) i opcjonalna nazwa.
2. **Tylko ZK jednego Klienta.** Jeden ZK — tylko w jednym zestawieniu, w całości.
3. ZK to rzeczywisty numer w formacie `nnn/rrrr`.
4. Zestawienie z kilkoma ZK tworzą i edytują: zakupowiec, Opiekun Projektu, Kierownik działu Obsługi Projektu. Dodanie ZK do istniejącego zestawienia dozwolone.
5. **Zestawienie nie ma własnych decyzji o BOM.** Linie, MPN i zamienniki zawsze pochodzą z BOM pakietów; błąd w BOM poprawia się w BOM pakietu.
6. **Bez osobnego zatwierdzenia** — zestawienie dostosowane przez zakupowca jest stanem obowiązującym i w tej formie idzie do kompletacji.

### Łączenie pozycji

7. Ten sam MPN + ten sam Case → łączenie automatyczne; ten sam MPN, różny Case → decyzja.
8. Te same parametry (po liczbach) + ten sam Case → automatycznie; różny Case → decyzja. Parametry zgodne, ale jedna linia ma dodatkowe nieukryte parametry → decyzja.
9. **Decyzje podejmuje zakupowiec**; decyzja zapisana (kto, kiedy, które linie) i zapamiętana jako podpowiedź dla tej samej pary komponentów.
10. **Linie DNP** nie łączą się z innymi; zostają na dole zestawienia.
11. **Rozłączenie** pojedynczej linii z pozycji — z powodem.
12. **Różnice między łączonymi liniami** pokazywane w konkretnej komórce (Description, Value, Notes…); pusta vs wypełniona komórka słabiej niż sprzeczność. Footprint nie jest pokazywany.
13. **MPN w połączonej pozycji:** zatwierdzone = część wspólna połączonych linii; niedozwolone = suma ze wszystkich połączonych linii. Pusta część wspólna → zakupowiec może połączyć, wskazując MPN z podstawą — to zamiennik w BOM pakietu.

### Wybór MPN

14. Zakupowiec widzi przy pozycji MPN zatwierdzone (z podstawą) i niedozwolone. Wybór z zatwierdzonych → status automatyczny; **niedozwolonego nie da się wybrać**.
15. **Nowy MPN spoza listy = zamiennik:** takie same lub lepsze parametry → automatycznie; w innym przypadku zatwierdza zakupowiec z podstawą. Zakres: BOM pakietu albo wersja BOM dla ZK. Zgłoszenie trafia do BOM pakietu; Opiekun Projektu dostaje informację.
16. Zakupowiec może dopisać do bazy komponentów nowy MPN zamiennika z podstawowymi danymi.

### Ilości

17. **Zapas z reguł zapasu według Case**; Case bez reguły → ostrzeżenie i decyzja zakupowca (zapamiętywana).
18. **Linie niepotwierdzone** są widoczne, ale **nieliczone** do zamówienia; zakupowiec może świadomie odblokować linię z powodem (np. długi LT).
19. **Pobranie z magazynu:** zakupowiec wskazuje MPN z magazynu (nie opakowanie). MPN z ilością „w kompletacji”, „na ZD” albo „zablokowane” → ostrzeżenie do akceptacji, z listą dostępnych opakowań i polem „ilość do pobrania z magazynu”.
20. **Rezerwacje** — poza zakresem.

### ZD i zmiany

21. **ZD to odnośnik na pozycji** (numer, ilość, LT). Jedna pozycja może mieć kilka ZD; ZD powstają w innym systemie — iPart ich nie blokuje.
22. **Zmiana BOM lub ilości ZK** → lista „było → będzie” dla zakupowca (dokupić / anulować / nadmiar ze zmiany), w „Do obsłużenia”.
23. **Przejście ZK w toku na nową wersję BOM** zatwierdza zakupowiec (propozycja Opiekuna Projektu).
24. **Uwagi zakupów do magazynu** to komunikaty, obsługiwane przez magazyn jak zmiany (z potwierdzeniem).

### Kolory (widok zakupów)

25. Status MPN: zgodny z BOM / z parametrami / Generic i zatwierdzony (Klient, TS) A9D18E; zamiennik tylko dla ZK 9DC3E6; do weryfikacji FFD966; niedozwolony — czarne tło, czerwony tekst. Jedna nazwa: „Niedozwolony”. Linie jak w BOM pakietu.

## Interfejs

| Rodzaj | Nazwa | Opis |
|---|---|---|
| komenda | Utwórz zestawienie | ZK jednego Klienta, wersje BOM, ilości |
| komenda | Dodaj ZK do zestawienia | |
| komenda | Połącz / rozłącz pozycję | z powodem |
| komenda | Wybierz MPN | z zatwierdzonych albo jako zamiennik |
| komenda | Odblokuj linię niepotwierdzoną | z powodem |
| komenda | Wpisz ZD | numer, ilość, LT |
| zapytanie | Pozycje zestawienia | dla Kompletacji: MPN, ilości, ZD, LT |
| zdarzenie publikowane | Zamiennik zatwierdzony | do BOM pakietu |
| zdarzenie konsumowane | Wersja BOM zatwierdzona / linia zmieniona | od BOM pakietu |

## Zależności

- **BOM pakietu** — zatwierdzone wersje BOM, MPN zatwierdzone i niedozwolone; przyjmuje zamienniki.
- **Obudowy i reguły zapasu** — zapas dla Case.
- **Stan magazynu i wycena** — stany, ceny, kursy.
- **Usługi wspólne** — dostępy (ceny), „Do obsłużenia”, komunikaty, historia, blokady.
- **Nexo** — ZK i ZD (sposób dostępu do ustalenia).

## Legacy

| Legacy | Co przejmuje moduł | Co znika |
|---|---|---|
| sPart | ilości, statusy MPN, zamienniki, ZD, LT | wklejanie stanu, ręczny PN magazynu przy każdej linii, „Zablokuj wartości”, tryby pliku |
| fusePart | łączenie pozycji, różnice, rozłączanie | osobny plik z listami MPN, klucz tekstowy `Part TS/Case`, zatwierdzenie łączenia, ZK bez roku |

Okres przejściowy: zakupy rozpoczęte w Excelu kończymy w Excelu ([Decyzje VBA](../../../robocze/Decyzje%20VBA.md), 9.4).

## Stan

2026-11-26: brak kodu.

## Otwarte pytania

| Pytanie | Kto może odpowiedzieć |
|---|---|
| Od którego momentu zmiana w zestawieniu wymaga obsłużenia przez zakupowca (np. od pierwszego ZD)? | Kierownik działu Obsługi Projektu, Zakupowiec |
| Nadwyżka zakupowa i podsumowanie kosztów | Kierownik Logistyki |

## Decyzje

- [ADR-0009](../../../decyzje/ADR-0009%20Zestawienie%20zakupowe%20zamiast%20osobnej%20ścieżki%20łączenia.md) Zestawienie zakupowe zamiast osobnej ścieżki łączenia
- [ADR-0006](../../../decyzje/ADR-0006%20Stałe%20identyfikatory%20i%20powiązania%20zewnętrzne.md) Stałe identyfikatory
