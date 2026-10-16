# RaceDay Part 1 & 2

## Roles
Roles & Capabilities
Organiser: Creates, edits and deletes events; manages event categories; captures participant results; views all enrolments for their events.
Participant: Registers an account; browses events; enrols in an event by selecting a category; views their own enrolments and personal race results.

# Part 1: System Planning and Database

## Contents of this repo (Part 1)
All planning documents are in the [`/docs`](./docs) folder:
File & Description
`RaceDay ERD.png`: Entity Relationship Diagram — 6 entities (Roles, Users, Events, Categories, Enrolments, Results), with primary keys, foreign keys, and relationship cardinality.
`RaceDay API Endpoint Plan.pdf`: Full REST API endpoint specification covering authentication, user profiles, events, categories, event enrolments, and results — including HTTP method, route, description, required role, 
request body, and expected response for every endpoint.
`RaceDaySQLQuery.sql`: SQL Server (SSMS) script that creates the full database schema matching the ERD, with primary/foreign keys, constraints, and seed data (2 Organisers, 3 Participants, 3 Events, 
categories per event, and sample enrolments/results).

## Design notes
- **Users** table holds both Organisers and Participants, differentiated by a `RoleID` foreign key into a `Roles` lookup table.
- **Enrolments** is the junction table implementing the many-to-many relationship between Participants (Users) and Categories.
- **Results** has a one-to-one relationship with Enrolments — one result per race entry, captured by the Organiser after the event.

## Running the SQL script
1. Open SQL Server Management Studio (SSMS) and connect to a clean SQL Server instance.
2. Open `docs/RaceDaySQLQuery.sql`.
3. Execute the script. It will create a `RaceDayDB` database, all six tables, and seed sample data.
4. Verify with `SELECT * FROM Users;` etc.

## Part 1 CI/CD
A GitHub Actions workflow (`.github/workflows/Part 1 CI.yml`) runs on every push and validates that:
- the `/docs` folder exists,
- it contains the ERD file, the endpoint plan and the SQL script,
- `README.md` exists at the repository root.
<img width="1322" height="282" alt="image" src="https://github.com/user-attachments/assets/f3ad6d75-9c46-42ac-980b-25e9e7557a15" />

<img width="630" height="119" alt="Screenshot 2026-09-04 230437" src="https://github.com/user-attachments/assets/883c0397-ca0c-4cff-8e7c-97b7e051300c" />


## Part 1 Video walkthrough
**YouTube (unlisted):** `(https://youtu.be/sLPNnJ_g7q4?si=_8_nj2le3DNoH5yX)`
The video walks through the planning documents, explains the ERD design decisions, the endpoint plan and runs the SQL script live in SSMS.

## Commit history
This part is built with commits (entity design, endpoint planning, schema iteration) rather than single uploads.


# Part 2: RESTful API Development

## Overview
`RaceDay.Api` is an ASP.NET Core Web API (net8.0) that implements every endpoint from the Part 1 plan. It uses Entity Framework Core (Code-First) against SQL Server, JWT bearer authentication for session/role management, and Swagger for live endpoint testing.

## Project structure
```
RaceDay.sln
RaceDay.Api/              ← the Web API
  Models/                 ← Role, User, Event, Category, Enrolment, Result
  Data/RaceDayDbContext.cs
  DTOs/                   ← request/response shapes per feature area
  Controllers/            ← AuthController, UsersController, EventsController,
                             CategoriesController, EnrolmentsController
  Services/TokenService.cs
  Program.cs
RaceDay.Api.Tests/        ← xUnit test project
docs/                     ← Part 1 planning documents
.github/workflows/        ← Part 1 CI.yml, Part 2 CI.yml
```

## Setup and run
1. **Clone the repo** and open `RaceDay.sln` in Visual Studio 2022 (17.8+, with the ASP.NET and web development workload).
2. **Set the connection string.** `RaceDay.Api/appsettings.json` ships with a LocalDB connection string that works out of the box:
   ```
   Server=(localdb)\mssqllocaldb;Database=RaceDayDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
   ```
   
3. **Create the database with EF Core migrations** (Code-First, per the requirement). Open the Package Manager Console with `RaceDay.Api` as the default project and run:
   ```powershell
   Add-Migration InitialCreate -Project RaceDay.Api
   Update-Database -Project RaceDay.Api
   ```
   Or from a terminal in the `RaceDay.Api` folder:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
   This creates `RaceDayDB` with a schema matching the Part 1 ERD/SQL script exactly, and seeds the two roles (Organiser, Participant).
4. **Run the project** (F5, or `dotnet run` from `RaceDay.Api/`). It launches straight into Swagger at `/swagger`. No signing key or secret setup is required — session state is managed entirely server-side (see below).

## Authentication and role-based access
Authentication uses **ASP.NET Core server-side session state**, per the Part 2 brief's "session management to maintain the user's authenticated state and role" requirement:

- `POST /api/auth/register` creates a user as either `Organiser` or `Participant`. Passwords are hashed with ASP.NET Core's `PasswordHasher<User>` — never stored in plain text.
- `POST /api/auth/login` verifies credentials, then stores the user's **ID and role server-side** in ASP.NET Core session state (`HttpContext.Session`, backed by `AddDistributedMemoryCache()` — swap in `AddStackExchangeRedisCache` etc. for a real multi-instance deployment). The client only ever receives an opaque, `HttpOnly` session-ID cookie (`RaceDay.Session`) — it cannot read or forge its own role from the client side.
- A custom `SessionAuthenticationHandler` (`RaceDay.Api/Authentication/SessionAuthenticationHandler.cs`) reads that server-side session on every request and builds the `ClaimsPrincipal` from it, so standard `[Authorize]` / `[Authorize(Roles = "Organiser")]` / `[Authorize(Roles = "Participant")]` attributes work exactly as normal.
- `POST /api/auth/logout` clears the session server-side, ending the authenticated state immediately — verified by a unit test that confirms a protected call fails right after logout.
- Ownership is checked in code on top of role checks — e.g. an Organiser can only edit/delete their own events, not another Organiser's.

### Testing auth in Swagger
1. Open `/swagger` in a browser.
2. Use **Try it out** on `POST /api/auth/register`, then on `POST /api/auth/login` with the same credentials, and execute.
3. That's it — the session cookie set on the login response is stored by the browser and sent automatically with every further request made from that Swagger page. No "Authorize" button or manual header needed; just call any protected endpoint next and it will already recognise you.
4. Call `POST /api/auth/logout` to end the session and confirm protected endpoints then return 401.

## Endpoints implemented
All endpoints from `docs/RaceDay API Endpoint Plan.pdf` are implemented — authentication (including logout), user profile (including picture upload), events (CRUD + banner upload), categories (CRUD), event enrolments (enrol/list/cancel), and results (capture/view). Every endpoint carries an XML `<summary>` doc comment, surfaced in Swagger UI via `IncludeXmlComments` (see `RaceDay.Api.csproj`'s `GenerateDocumentationFile` and `Program.cs`), so each one describes its purpose, expected input, and response directly in `/swagger` with no additional tooling.

> **Note on image uploads:** `POST /api/users/me/profile-picture` and `POST /api/events/{id}/banner` currently save files to `wwwroot/uploads` and return a local URL. Part 3 replaces this storage layer with Azure Blob Storage — the routes and response shapes stay the same, so the MVC app built in Part 3 won't need to change.

## Unit tests
`RaceDay.Api.Tests` uses `WebApplicationFactory<Program>` with EF Core's InMemory provider, so tests run through the real HTTP pipeline — including actual session-cookie authentication and `[Authorize(Roles = ...)]` enforcement, not just direct controller calls. Each test identity uses its own `HttpClient` (own cookie jar) so concurrent sessions never collide. Coverage includes:
- **Registration and login** — valid registration, duplicate email, invalid role, valid login, wrong password, unknown email, session cookie is actually set, session persists across requests.
- **Event management** — Organiser can create/update/delete their own event; Participant is rejected with 403; unauthenticated requests are rejected with 401; an Organiser cannot edit another Organiser's event; logout ends the session (a protected call right after logout returns 401).
- **Categories** — owning Organiser can create/delete; a different Organiser and a Participant are both rejected with 403; public listing works unauthenticated.
- **Event enrolments** — Participant can enrol and the enrolment is correctly recorded and retrievable; Organiser is rejected with 403; duplicate enrolment returns 409; a Participant cannot cancel another Participant's enrolment.
- **Results** — owning Organiser can capture a result; a Participant is rejected with 403; capturing twice returns 409; a Participant can see the result in their own history; an unrelated Participant cannot view someone else's result (403).

Run the tests:
```bash
dotnet test RaceDay.sln
```

## Part 2 CI/CD
A GitHub Actions workflow (`.github/workflows/Part 2 CI.yml`) runs on every push and pull request: it restores, builds the solution in Release configuration, and runs the full `dotnet test` suite, uploading the test results as a build artifact.

**Screenshot of a successful green build:**

`[Insert screenshot here after your first successful Part 2 workflow run]`

## Part 2 video walkthrough
**YouTube (unlisted):** `[Insert link here]`

The video demonstrates the running API, explains the code structure, endpoints, authentication, and unit tests.

## Commit history
Parts 1 and 2 are built with commits reflecting real, incremental work (entity design, endpoint planning, schema iteration, controller implementation, test coverage) rather than single uploads.
