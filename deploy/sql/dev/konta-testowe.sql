-- Tylko baza developera (LocalDB). Fikcyjne konta do wyboru przy starcie, dopóki nie ma kopii listy kont ProduMana.
-- Nie uruchamiać na bazie pilotażowej: tam produman.users wypełnia kopia od developera ProduMana (ADR-0014).
SET NOCOUNT ON;
DELETE FROM [produman].[users] WHERE [Login] LIKE N'test.%';
INSERT INTO [produman].[users] ([UserId], [Login], [FullName], [Email], [Department], [IsActive], [Deleted]) VALUES
 ('11111111-0000-4000-8000-000000000001', N'test.opiekun', N'Opiekun Projektu (test)', NULL, 100109, 1, 0),
 ('11111111-0000-4000-8000-000000000002', N'test.zakupy', N'Zakupowiec (test)', NULL, 100108, 1, 0),
 ('11111111-0000-4000-8000-000000000003', N'test.magazyn', N'Magazyn (test)', NULL, 100100, 1, 0),
 ('11111111-0000-4000-8000-000000000004', N'test.smt', N'Technolog SMT (test)', NULL, 100101, 1, 0),
 ('11111111-0000-4000-8000-000000000005', N'test.brakdzialu', N'Konto bez działu w słowniku (test)', NULL, 100110, 1, 0),
 ('11111111-0000-4000-8000-000000000006', N'test.nieaktywne', N'Konto nieaktywne (test — nie powinno się pokazać)', NULL, 100109, 0, 0);
