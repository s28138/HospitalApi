# HospitalApi

Projekt wykonany w ramach ćwiczeń z APBD. Aplikacja została przygotowana jako ASP.NET Core Web API z wykorzystaniem Entity Framework Core w podejściu Database First.

## Opis projektu

Aplikacja korzysta z lokalnej bazy danych `HospitalDb`, utworzonej na podstawie dostarczonego skryptu `create.sql`.

Modele oraz kontekst bazy danych zostały wygenerowane przez Entity Framework Core na podstawie istniejącej struktury bazy danych.

Projekt obsługuje dane pacjentów, przyjęć do szpitala, oddziałów, sal, łóżek oraz przypisań łóżek pacjentom.

## Wykorzystane technologie

- C#
- ASP.NET Core Web API
- Entity Framework Core
- Microsoft SQL Server LocalDB
- Swagger
- Git / GitHub

## Struktura projektu

- `Controllers` - kontrolery API
- `Data` - kontekst bazy danych
- `Models` - modele wygenerowane przez Entity Framework Core
- `DTOs` - klasy DTO używane w odpowiedziach i żądaniach
- `Database` - pliki dostarczone do zadania, w tym skrypt SQL i diagram ERD

## Baza danych

Baza danych została utworzona lokalnie w SQL Server LocalDB.

Nazwa bazy:

`HospitalDb`

Connection string znajduje się w pliku `appsettings.json`:

`Server=(localdb)\\MSSQLLocalDB;Database=HospitalDb;Trusted_Connection=True;TrustServerCertificate=True;`

## Endpointy

### Pobieranie pacjentów

`GET /api/patients`

Endpoint zwraca listę pacjentów wraz z informacjami o przyjęciach oraz przypisaniach łóżek.

Można też użyć parametru `search`:

`GET /api/patients?search=an`

Parametr `search` filtruje pacjentów po imieniu oraz nazwisku.

### Przypisanie łóżka pacjentowi

`POST /api/patients/{pesel}/bedassignments`

Endpoint przypisuje pacjentowi wolne łóżko danego typu na wskazanym oddziale i w podanym okresie.

Przykładowe body:

{
"from": "2026-05-20T14:00:00",
"to": "2026-05-30T10:00:00",
"bedType": "Standard",
"ward": "Kardiologia"
}

Jeżeli nie ma wolnego łóżka, endpoint zwraca kod `404` z odpowiednim komunikatem.

Jeżeli pacjent, oddział albo typ łóżka nie istnieje, API również zwraca odpowiedni komunikat błędu.

## Uruchomienie projektu

1. Utworzyć bazę danych `HospitalDb`.
2. Uruchomić skrypt `Database/create.sql`.
3. Sprawdzić connection string w `appsettings.json`.
4. Uruchomić projekt komendą:

`dotnet run`

5. Otworzyć Swaggera w przeglądarce:

`http://localhost:5248/swagger`

## Testowanie

Projekt był testowany przez Swaggera.

Sprawdzone zostały między innymi:

- pobieranie listy pacjentów,
- filtrowanie pacjentów parametrem `search`,
- przypisanie łóżka pacjentowi,
- obsługa sytuacji, gdy nie znaleziono pasującego łóżka.
