# Finals_Q1 Backend (Todo API)

## Setup

```powershell
dotnet restore
dotnet run --project .\TodoApi\TodoApi.csproj
```

API base URL: `http://localhost:5181` (see `TodoApi/Properties/launchSettings.json`)

## Endpoints

- `GET /api/todos`
- `POST /api/todos`
- `PUT /api/todos/{id}`
- `DELETE /api/todos/{id}`
- `GET /api/todos/verify` (Blockchain-style chain validation)

## Architecture Notes

- ASP.NET Core Web API with controller-based routing.
- In-memory `List<Todo>` used as storage.
- CORS configured for `http://localhost:5173`.
- Minimal validation: empty titles are rejected.
- Blockchain-style immutability: each todo stores `Hash` and `PreviousHash`, and the chain is validated via `GET /api/todos/verify`.

## Bonus Challenge (Backend Ledger)

- Hash is computed as SHA-256 of `Id|Title|Completed|PreviousHash`.
- New items link to the previous item's hash (or `GENESIS` for the first).
- `PUT`/`DELETE` rebuild the chain to keep integrity checks consistent.
