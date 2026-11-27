---
status: przyjęta
data: 2026-11-26
zakres: system
zastępuje:
zastąpiona-przez:
---

# ADR-0013 Uprawnienia nadawane kontu z szablonów

## Kontekst

- W sesji 4 ustalono macierz dostępów: dostęp × rola → edycja / odczyt / brak, z przypisami ograniczającymi edycję ([Użytkownicy i dostępy](../obszary/Usługi%20wspólne/moduly/Użytkownicy%20i%20dostępy.md)).
- W ProduMan uprawnienia nadaje się **kontu**, nie roli: katalog uprawnień w kodzie, przy każdym poziom (podgląd, zapis) i zakres działów; superadmini w konfiguracji; szablony przy nadawaniu (ADR-0025 ProduMan). Role w działach to dane pracownika w module Pracownicy, nie podstawa uprawnień. Developer IT prosi o ten sam wzorzec, żeby uprawnienia dało się później przenieść.
- Logowanie kontem Nexo, tożsamość = Guid konta ([ADR-0011](ADR-0011%20Logowanie%20kontem%20Nexo%20przez%20LoginApi.md)).

## Decyzja

1. **Uprawnienia ma konto**, nie rola: lista uprawnień osoby, każde z poziomem **podgląd** albo **zapis** i zakresem (dział osoby, grupa działów, wszystko). Brak uprawnienia na liście = brak dostępu, także dla uprawnień dodanych do katalogu później.
2. **Katalog uprawnień w kodzie.** Ograniczenia z przypisów macierzy są osobnymi uprawnieniami (np. „zamienniki w BOM pakietu — zapis” dla zakupowca), a nie odmianą poziomu.
3. **Macierz z sesji 4 staje się zestawem szablonów** („Opiekun Projektu”, „Zakupowiec”…). Szablon wpisuje uprawnienia przy nadawaniu; potem lista osoby może się różnić. Zmiana szablonu nie zmienia samoczynnie osób, które już go dostały.
4. **Kod sprawdza uprawnienie, nigdy nazwę roli ani szablonu.** Sprawdza je serwer przy każdej operacji, z bieżącego stanu (pamięć na minutę), nie z sesji.
5. **Superadministrator** w konfiguracji serwera: Kierownik działu Obsługi Projektu (i developer na czas wdrożenia). Ma pełny dostęp i nadaje uprawnienia.
6. **W alfie** uprawnienia nadaje developer według szablonów, na prośbę Kierownika działu Obsługi Projektu (bez ekranu). Każda zmiana jest zapisana w historii (kto, komu, było → będzie).

## Rozważane warianty

- **Role z macierzą** (osoba = rola, dostęp wynika z macierzy) — odrzucony: wyjątek dla jednej osoby wymaga nowej roli, a uprawnień nie dałoby się przenieść do wspólnego modułu firmy bez przebudowy.

## Konsekwencje

- Pojęcie „rola” w iPart oznacza odtąd szablon uprawnień; rola organizacyjna osoby należy do modułu Pracownicy ProduMan.
- Zakres działów jest zapisywany od początku; dopóki żadna reguła iPart nie zależy od działu, szablony nadają zakres „wszystko”.
- Administrator grupy (zastępstwa, zwalnianie blokad) pozostaje osobną cechą członkostwa w grupie.
