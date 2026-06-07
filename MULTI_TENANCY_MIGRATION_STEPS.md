# Multi-tenancy: running the EF Core migrations

The sandbox this assistant runs in has no .NET SDK, so these migrations must be
generated and applied on your machine. Everything below assumes you run commands
from the solution root: `/Users/pavelarias/Documents/GitHub/HistorialMedico`.

## What changed (recap)

- New `Tenant` entity, stored in the **AspnetUsers** database (via `ApplicationDbContext`),
  because `HistorialDbContext` (database `HistorialMedico`) and `ApplicationDbContext`
  (database `AspnetUsers`) are physically separate — EF can't create cross-database FKs.
- `BaseEntity` (the base class for `Patient`, `MedicalHistory`, `Attachment`,
  `AdditionalPhone`, `ExpedienteCounter`) now has a required `TenantId` (Guid) column
  + index, and `HistorialDbContext` applies a global query filter so every query is
  automatically scoped to the current tenant.
- `User` now has a nullable `TenantId` (null = platform super-admin).
- JWTs now carry a `"tenantId"` claim; `ICurrentTenantService` reads it per-request.

This means **two** migrations are needed — one per `DbContext` — because they map to
two different databases with two different `MigrationsHistory` tables.

## Prerequisites

Make sure the EF Core CLI tools are installed/up to date:

```bash
dotnet tool update --global dotnet-ef
```

`Microsoft.EntityFrameworkCore.Design` is already referenced by the **Presentation**
project, so the CLI will work as long as you point it at that project as the
"startup project" (it loads `appsettings.json` / DI configuration from there) while
targeting **Infrastructure** (where the `DbContext` classes and `Migrations/` folders live).

## 1. Migration for `ApplicationDbContext` (AspnetUsers DB — adds `Tenants` table + `User.TenantId`)

```bash
cd "/Users/pavelarias/Documents/GitHub/HistorialMedico"

dotnet ef migrations add AddTenantsAndUserTenantId \
  --project Infrastructure \
  --startup-project Presentation \
  --context ApplicationDbContext \
  --output-dir Migrations/Identity
```

Review the generated migration in `Infrastructure/Migrations/Identity/`. You should see:
- A new `Tenants` table (Id, Name, Slug [unique index], BillingStatus, Plan, IsActive, CreatedAt, UpdatedAt)
- A new nullable `TenantId` column added to `AspNetUsers`

Then apply it:

```bash
dotnet ef database update \
  --project Infrastructure \
  --startup-project Presentation \
  --context ApplicationDbContext
```

## 2. Migration for `HistorialDbContext` (HistorialMedico DB — adds `TenantId` to all domain tables)

```bash
dotnet ef migrations add AddTenantIdToDomainEntities \
  --project Infrastructure \
  --startup-project Presentation \
  --context HistorialDbContext \
  --output-dir Migrations/Domain
```

Review the generated migration in `Infrastructure/Migrations/Domain/`. You should see
a required `TenantId` (Guid, `NOT NULL`) column + index added to: `Patients`,
`MedicalHistories`, `Attachments`, `AdditionalPhones`, `ExpedienteCounters`.

**Important — existing data:** because `TenantId` is required (`NOT NULL`) and your
`HistorialMedico` database likely already has rows (e.g. the seeded "Juan Carlos
Pérez López" patient, or your own data), the auto-generated migration will probably
fail to apply against a populated table (SQL Server can't add a `NOT NULL` column
without a default to a table with existing rows). Before running `database update`,
open the generated migration file and either:

- **Option A (recommended for a single-clinic dataset you want to keep):** add a
  default tenant ID and backfill, e.g. add the column as nullable first, run a raw
  SQL `UPDATE` to stamp existing rows with one chosen `Guid`, then alter the column
  to `NOT NULL`. Something like:
  ```csharp
  migrationBuilder.AddColumn<Guid>(
      name: "TenantId",
      table: "Patients",
      type: "uniqueidentifier",
      nullable: false,
      defaultValue: new Guid("PUT-A-FIXED-GUID-HERE"));
  ```
  Repeat the `defaultValue` for each affected table, **using the same GUID** for all
  of them. Then create a matching `Tenant` row in `AspnetUsers.Tenants` with that same
  `Id` (and likewise set that `Id` as the `TenantId` on whichever `User` rows should
  own this existing data) so the data isn't orphaned.

- **Option B (if you're fine wiping current dev data):** just delete the existing
  rows from `Patients`/`MedicalHistories`/etc. before running `database update`, or
  drop and recreate the `HistorialMedico` database. (`DatabaseSeed.Unseed` +
  `DatabaseSeed.Seed` already handle reseeding tenant-stamped data on next app start
  in Development.)

Then apply it:

```bash
dotnet ef database update \
  --project Infrastructure \
  --startup-project Presentation \
  --context HistorialDbContext
```

## 3. Smoke-test

1. Run the API (`dotnet run --project Presentation`, or your usual launch profile) in
   Development — this triggers `DatabaseSeed.Unseed`/`Seed`, which now provisions a
   "Clínica Demo" tenant and stamps the seeded admin/doctor/Pavel users and the sample
   patient with its `TenantId`.
2. Log in as `admin` / `Admin123!` (or `pavelarias` / `Geraldo123?`). Decode the
   returned JWT (e.g. on jwt.io) and confirm it contains a `"tenantId"` claim matching
   the seeded tenant's `Id`, and that the login response includes `tenantId` /
   `tenantName` ("Clínica Demo").
3. Call a patients endpoint with that token and confirm you only see patients
   belonging to that tenant (the global query filter in `HistorialDbContext` should
   transparently scope the results).
4. Try registering a brand-new account through the "create admin" flow with a
   `TenantName` — confirm it provisions a new `Tenant` row (with a generated `Slug`)
   and that the new admin only ever sees their own clinic's (empty) patient list.

## A note on code I couldn't compile-check

Since the sandbox has no .NET SDK, none of this was run through `dotnet build`. I
re-read every file I touched for syntax/type correctness, but if `dotnet build`
surfaces anything (most likely candidates: a namespace mismatch, or EF Core
complaining about the reflection-based query filter setup in
`HistorialDbContext.OnModelCreating`), let me know the exact error and I'll fix it
immediately.
