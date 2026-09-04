# RaceDay — Part 1: System Planning and Database

## Roles

Roles & Capabilities
Organiser: Creates, edits, and deletes events; manages event categories; captures participant results; views all enrolments for their events.
Participant: Registers an account; browses events; enrols in an event by selecting a category; views their own enrolments and personal race results.

## What's in this repository (Part 1)

All planning documents live in the [`/docs`](./docs) folder:
File & Description
`RaceDay_ERD.png`: Entity Relationship Diagram — 6 entities (Roles, Users, Events, Categories, Enrolments, Results), with primary keys, foreign keys, and relationship cardinality.
`RaceDay_API_Endpoint_Plan.md`: Full REST API endpoint specification covering authentication, user profiles, events, categories, event enrolments, and results — including HTTP method, route, description, required role, 
request body, and expected response for every endpoint.
`RaceDay_Database.sql`: SQL Server (SSMS) script that creates the full database schema matching the ERD, with primary/foreign keys, constraints, and seed data (2 Organisers, 3 Participants, 3 Events, 
categories per event, and sample enrolments/results).

## Design notes
- **Users** table holds both Organisers and Participants, differentiated by a `RoleID` foreign key into a `Roles` lookup table.
- **Enrolments** is the junction table implementing the many-to-many relationship between Participants (Users) and Categories.
- **Results** has a one-to-one relationship with Enrolments — one result per race entry, captured by the Organiser after the event.
- `Events.DistanceKm`, `Events.EventType`, and `Events.BannerImageURL` were added to support later requirements (event type filtering and Azure Blob Storage banner uploads in Parts 2/3). `Categories.DistanceKm` is nullable to support both distance-based categories (e.g. "10km") and age-based categories (e.g. "Under 20").
- `Users.ProfilePictureURL` supports the Part 3 requirement for Participants to upload a profile picture via Azure Blob Storage.

## Running the SQL script

1. Open SQL Server Management Studio (SSMS) and connect to a clean SQL Server instance.
2. Open `docs/RaceDay_Database.sql`.
3. Execute the script (F5). It will create a `RaceDayDB` database, all six tables, and seed sample data.
4. Verify with `SELECT * FROM dbo.Users;` (and similarly for the other tables).

## CI/CD

A GitHub Actions workflow (`.github/workflows/Part1CI.yml`) runs on every push and validates that:
- the `/docs` folder exists,
- it contains the ERD file, the endpoint plan, and the SQL script,
- `README.md` exists at the repository root.
<img width="1322" height="282" alt="image" src="https://github.com/user-attachments/assets/f3ad6d75-9c46-42ac-980b-25e9e7557a15" />



## Video walkthrough

**YouTube (unlisted):** `[Insert link here]`
The video walks through the planning documents, explains the ERD design decisions, the endpoint plan and runs the SQL script live in SSMS.

## Commit history
This part is built with commits (entity design, endpoint planning, schema iteration) rather than single-shot uploads.
