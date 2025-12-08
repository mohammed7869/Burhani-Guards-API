# Burhani Guards API

Minimal .NET 8 API that exposes captain and member login flows for the Burhani Guards app. The backend combines Dapper (SQLite) for deterministic records plus MongoDB Atlas for member profiles.

## Stack

- .NET 8 minimal APIs
- MongoDB Atlas (`members` collection) for member credentials
- SQLite + Dapper for captain credential seeding and member snapshot auditing
- BCrypt for password hashing

## Configuration

`appsettings.json` already contains the provided MongoDB Atlas URI and a default database/collection. Update the `Mongo` section if you store members elsewhere. The SQLite file path can be changed through the `Sqlite` section (defaults to `Data\burhani_guards.db` inside the API directory).

## Endpoints

| Method | Route                     | Body            | Description                               |
|--------|---------------------------|-----------------|-------------------------------------------|
| POST   | `/api/auth/captain/login` | `{ email, password }` | Validates the captain using Dapper-backed SQLite storage seeded with the provided static credentials. |
| POST   | `/api/auth/member/login`  | `{ email, password }` | Looks up the member inside MongoDB, verifies the BCrypt hash, persists a snapshot via Dapper, and returns a token. |

All responses share the same shape:

```jsonc
{
  "email": "user@example.com",
  "displayName": "Captain User",
  "role": "captain",
  "token": "<sha256 token>"
}
```

## Running locally

1. Install the .NET 8 SDK.
2. From `burhaniguardsapi`, run `dotnet run`.
3. Swagger UI becomes available at `https://localhost:5001/swagger` (development).

> Note: The project seeds the SQLite database automatically on startup and expects MongoDB member documents to store `passwordHash` values produced through `BCrypt.Net.BCrypt.HashPassword`.

