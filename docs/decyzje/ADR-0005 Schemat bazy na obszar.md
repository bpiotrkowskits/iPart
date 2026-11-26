---
status: przyjęta
data: 2026-11-26
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0005 Schemat bazy na obszar

## Kontekst

- W wersji 0.1.0 wszystkie tabele (komponenty, słowniki, pakiety, BOM) leżą w jednym schemacie `dataPart`. Nazwa pochodzi od pliku Excel z bazą komponentów.
- iPart ma sześć obszarów ([Mapa obszarów](../system/02%20Mapa%20obszarów.md)), a przejęcie przez system magazynowy ma być tanie ([ADR-0001](ADR-0001%20iPart%20jako%20system%20przejściowy%20do%20czasu%20modułu%20BOM.md)).
- ProduMan stosuje schemat na moduł; inne moduły nie czytają jego tabel.

## Decyzja

1. **Jedna baza SQL Server, schemat na obszar:**

   | Schemat | Obszar |
   |---|---|
   | `components` | Baza komponentów, także reguły zapasu i słowniki obudów |
   | `documentation` | Dokumentacja pakietu |
   | `purchasing` | Zaopatrzenie |
   | `kitting` | Kompletacja |
   | `estimation` | Estymacja |
   | `shared` | Usługi wspólne, także Klient |

2. Nazwy schematów i tabel po angielsku, jak nazwy z kodu.
3. **Obszar zapisuje tylko do swojego schematu.** Klucze obce między schematami wyłącznie do identyfikatorów ([ADR-0006](ADR-0006%20Stałe%20identyfikatory%20i%20powiązania%20zewnętrzne.md)).
4. Schemat `dataPart` zmienia nazwę na `components`.

## Rozważane warianty

- **Jeden schemat dla całości** (stan 0.1.0) — odrzucony: przejęcie jednego obszaru wymagałoby rozplątywania tabel.
- **Osobna baza na obszar** — odrzucony: transakcje obejmujące kilka obszarów (np. zatwierdzenie BOM i powstanie pozycji do obsłużenia) byłyby trudne, a korzyść przy przejęciu taka sama jak ze schematów.

## Konsekwencje

- Migracja przenosząca tabele z `dataPart` do `components` i `documentation`.
- Skrypty uprawnień w [Środowiskach](../system/07%20Środowiska.md) nadają role do całej bazy; ewentualne ograniczenia per schemat — później.
- Przy przejęciu obszaru jego schemat migruje w całości, razem z historią ([ADR-0007](ADR-0007%20Jeden%20mechanizm%20historii%20zmian.md)).
