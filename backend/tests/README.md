# Backend tests

`EnglishLearning.Tests` (xUnit v3) has two kinds of tests:

- **Unit** (`Unit/`): pure domain rules such as `VocabularyQuality`. They need no database.
- **Integration** (`Integration/`, trait `Category=Integration`): the real API hosted by
  `WebApplicationFactory<Program>` in the `Testing` environment, running against a throwaway
  PostgreSQL database.

## Running

Unit tests only (no database needed):

```bash
dotnet test backend/EnglishLearning.slnx --filter "Category!=Integration"
```

Everything (the tests use the local dev PostgreSQL server):

```bash
# bash
TEST_POSTGRES='Host=localhost;Port=5432;Username=postgres;Password=<password>;Database=postgres' \
  dotnet test backend/EnglishLearning.slnx
```

```powershell
# PowerShell
$env:TEST_POSTGRES = 'Host=localhost;Port=5432;Username=postgres;Password=<password>;Database=postgres'
dotnet test backend/EnglishLearning.slnx
```

## TEST_POSTGRES

`TEST_POSTGRES` is an **admin** connection string to the `postgres` maintenance database. The role must
be allowed to `CREATE DATABASE`. If the variable is unset, every integration test is reported as
skipped. Unit tests still run.

For each xUnit collection the fixture:

1. creates `el_test_<guid>`,
2. starts the API with `ConnectionStrings:Postgres` pointing at it and a random `Jwt:SigningKey`,
3. applies all EF Core migrations, then seeds collection-specific data,
4. terminates connections and drops the database when the collection finishes.

The collections are `QuizDb`, `QuizScarcityDb`, `ContentDb`, `MissionDb` and `PublishCapDb`. Publish-cap tests run in
their own database because they change the global published count. Tests create their own users
directly in the database and get tokens from the app's `ITokenService`, so they never call
`/auth/login` (which has a rate limiter). Terms and e-mails use GUID suffixes, so tests in the same
collection don't collide.

If a run is killed before cleanup, remove leftover databases manually:

```sql
SELECT datname FROM pg_database WHERE datname LIKE 'el_test_%';
DROP DATABASE "el_test_...";
```
