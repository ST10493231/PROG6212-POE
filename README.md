# RaceDay — Part 1 & Part 2

## Roles
Roles & Capabilities
Organiser: Creates, edits and deletes events; manages event categories; captures participant results; views all enrolments for their events.
Participant: Registers an account; browses events; enrols in an event by selecting a category; views their own enrolments and personal race results.

---

# Part 1: System Planning and Database

## Contents of this repository (Part 1)
All planning documents are in the [`/docs`](./docs) folder:
File & Description
`RaceDay ERD.pdf`: Entity Relationship Diagram — 6 entities (Roles, Users, Events, Categories, Enrolments, Results), with primary keys, foreign keys, and relationship cardinality.
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


## Part 1 video walkthrough
**YouTube (unlisted):** [`(https://youtu.be/sLPNnJ_g7q4?si=_8_nj2le3DNoH5yX)`]
The video walks through the planning documents, explains the ERD design decisions, the endpoint plan and runs the SQL script live in SSMS.

---

# Part 2: RESTful API Development

## Overview
`RaceDay` is an ASP.NET Core Web API (net8.0) that implements every endpoint from the Part 1 plan. It uses Entity Framework Core (Code-First) against SQL Server, **server-side session authentication** for login state and roles, and Swagger for live endpoint testing.

## Repository structure
```
RaceDay.sln
README.md
docs/                      <- Part 1 planning documents (ERD, endpoint plan, SQL script)
.github/workflows/         <- Part 1 CI.yml, Part 2 CI.yml
RaceDay/                   <- the Web API
  Authentication/          <- SessionAuthenticationHandler
  Common/                  <- ApiError
  Controllers/             <- Authentication, Users, Events, Categories, Enrolments
  Database/                <- RaceDayDbContext
  DTOs/                    <- request/response shapes per feature area
  Extensions/              <- ClaimsPrincipalExtensions
  Migrations/              <- EF Core Code-First migrations
  Models/                  <- Role, User, Event, Category, Enrolment, Result
  Program.cs
RaceDay.Tests/             <- xUnit test project
  AuthenticationTests/  CategoriesTests/  EnrolmentsTests/
  EventsTests/  ResultsTests/  TestHelpers/
```

## The two roles
- **Organiser** - creates, updates and deletes their own events; defines categories (e.g. Under 20, Senior, 10km, 21km) for their events; views all enrolments for their events; captures finish times and positions for participants.
- **Participant** - registers an account; browses events and categories; enrols in an event by selecting a category; cancels their own enrolment; views their own results.

Both roles can register, log in, log out and view/update their own profile.

## Setup and run
**Prerequisites:** .NET 8 SDK, Visual Studio 2022 (ASP.NET workload), SQL Server or SQL Server LocalDB, and the EF Core CLI (`dotnet tool install --global dotnet-ef`).

1. **Clone** the repository and open `RaceDay.sln` in Visual Studio.
2. **Check the connection string** in `RaceDay/appsettings.json` (`DefaultConnection`). Point it at your own SQL Server / LocalDB instance if needed.
3. **Create the database** from the committed migrations. In Package Manager Console (default project: `RaceDay`):
   ```powershell
   Update-Database
   ```
   or from the repository root in a terminal:
   ```bash
   dotnet ef database update --project RaceDay
   ```
   This creates `RaceDayDB` matching the Part 1 ERD and seeds the two roles (Organiser, Participant).
4. **Run** `RaceDay` (F5 or `dotnet run --project RaceDay`). It opens Swagger at `/swagger`.

## Authentication and role-based access
Authentication uses **ASP.NET Core server-side session state**:

- `POST /api/auth/register` creates a user as `Organiser` or `Participant`. Passwords are hashed with ASP.NET Core's `PasswordHasher<User>` and never stored in plain text.
- `POST /api/auth/login` verifies the credentials and stores the user's ID and role server-side in `HttpContext.Session`. The client only receives an opaque, `HttpOnly` session cookie (`RaceDay.Session`).
- A custom `SessionAuthenticationHandler` rebuilds the `ClaimsPrincipal` from the session on every request, so `[Authorize]`, `[Authorize(Roles = "Organiser")]` and `[Authorize(Roles = "Participant")]` work as normal.
- `POST /api/auth/logout` clears the session; a unit test confirms protected calls then return 401.
- Ownership is also checked in code, e.g. an Organiser can only change their own events and categories.

### Testing auth in Swagger
1. Open `/swagger`, use **Try it out** on `POST /api/auth/register`, then `POST /api/auth/login`.
2. The browser stores the session cookie and sends it automatically with later requests from the Swagger page - no Authorize button needed.
3. Call `POST /api/auth/logout` and confirm protected endpoints return 401.

## Endpoints
| Area | Endpoints |
|---|---|
| Authentication | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout` |
| Users | `GET /api/users/me`, `PUT /api/users/me`, `POST /api/users/me/profile-picture` |
| Events | `GET /api/events`, `GET /api/events/{id}` (public); `POST`, `PUT /{id}`, `DELETE /{id}`, `POST /{id}/banner`, `GET /{id}/enrolments` (Organiser) |
| Categories | `GET /api/events/{eventId}/categories` (public); `POST /api/events/{eventId}/categories`, `PUT`/`DELETE /api/categories/{id}` (Organiser) |
| Enrolments | `POST /api/categories/{categoryId}/enrolments`, `GET /api/users/me/enrolments`, `DELETE /api/enrolments/{id}` (Participant) |
| Results | `POST /api/enrolments/{enrolmentId}/result` (Organiser); `GET /api/enrolments/{enrolmentId}/result` (authorised user); `GET /api/users/me/results` (Participant) |

Every endpoint has an XML `<summary>` doc comment shown in Swagger UI (purpose, expected input, response).


## Unit tests
`RaceDay.Tests` uses `WebApplicationFactory<Program>` with the EF Core InMemory provider, so tests go through the real HTTP pipeline, including session cookies and role enforcement. Each test identity uses its own `HttpClient` (own cookie jar). 29 tests cover:
- **Registration and login** - valid registration, duplicate email, invalid role, valid login, wrong password, unknown email, session persists across requests.
- **Event management** - Organiser can create and delete events; Participant gets 403; unauthenticated gets 401; another Organiser cannot update the event; logout ends the session.
- **Categories** - owning Organiser can create/delete; non-owning Organiser and Participant get 403; public listing works.
- **Enrolments** - Participant can enrol and the enrolment is recorded; Organiser gets 403; duplicate enrolment gets 409; unauthenticated gets 401; another Participant cannot cancel it.
- **Results** - owning Organiser can capture a result; Participant gets 403; duplicate capture gets 409; Participant sees their own result; unrelated Participant gets 403.

Run them:
```bash
dotnet test RaceDay.sln
```

## Part 2 CI/CD
The GitHub Actions workflow `.github/workflows/Part 2 CI.yml` runs on every push and pull request. It validates the repository structure, restores, builds in Release configuration and runs all unit tests, then uploads the results as an artifact.

**Screenshot of a successful green build:** (<img width="629" height="164" alt="image" src="https://github.com/user-attachments/assets/c7dbb78f-5ea1-42e4-8892-adfac7cb3881" />)

## Part 2 video walkthrough
**YouTube (unlisted):** [`https://youtu.be/AcpJlanLyYE?si=BG9-Rns0u8ZWUDgA`]

The video demonstrates the running API, and explains the code structure, endpoints, authentication, Swagger and the unit tests.
