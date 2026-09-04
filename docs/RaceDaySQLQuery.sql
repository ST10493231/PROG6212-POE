-- RaceDay Database Schema
-- System Planning and Database - Part 1

CREATE DATABASE RaceDayDB;

USE RaceDayDB;

-- Table: Roles
-- Lookup table for the two system roles

CREATE TABLE dbo.Roles (
    RoleID      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    VARCHAR(20) NOT NULL UNIQUE
);

-- Table: Users
-- Both Organisers and Participants live here, differentiated
-- by RoleID

CREATE TABLE dbo.Users (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        VARCHAR(100) NOT NULL,
    Email           VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NOT NULL,
    RoleID          INT NOT NULL,
    ProfilePictureURL VARCHAR(500) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID)
);

-- Table: Events
-- Created and owned by an Organiser (Users)

CREATE TABLE dbo.Events (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    Name            VARCHAR(150) NOT NULL,
    Description     VARCHAR(500) NULL,
    EventDate       DATETIME NOT NULL,
    Location        VARCHAR(150) NOT NULL,
    DistanceKm      DECIMAL(5,2) NOT NULL,
    EventType       VARCHAR(10) NOT NULL,
    BannerImageURL  VARCHAR(500) NULL,
    OrganiserID     INT NOT NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle'))
);

-- Table: Categories
-- Distance/race categories that belong to an Event
-- e.g. 5km, 10km, 21km

CREATE TABLE dbo.Categories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT NOT NULL,
    Name            VARCHAR(100) NOT NULL,
    DistanceKm      DECIMAL(5,2) NULL, -- NULL for age-based categories (e.g. 'Under 20', 'Senior')
    MaxParticipants INT NOT NULL DEFAULT 100,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);

-- Table: Enrolments
-- Junction table: a Participant (Users) enrolling in a
-- Category. Implements the many-to-many relationship between
-- Users and Categories.

CREATE TABLE dbo.Enrolments (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    UserID          INT NOT NULL,
    CategoryID      INT NOT NULL,
    EnrolmentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Status          VARCHAR(20) NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) REFERENCES dbo.Categories(CategoryID),
    CONSTRAINT UQ_Enrolments_User_Category UNIQUE (UserID, CategoryID)
);

-- Table: Results
-- One result per Enrolment (1-to-1), captured by the Organiser
-- after the race

CREATE TABLE dbo.Results (
    ResultID        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID     INT NOT NULL UNIQUE,
    FinishTime      TIME NULL,
    Position        INT NULL,
    Status          VARCHAR(20) NOT NULL DEFAULT 'Finished',
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID)
);

-- Roles
INSERT INTO dbo.Roles (RoleName) VALUES ('Organiser'), ('Participant');

-- Users: 2 Organisers, 3 Participants (extra participant so enrolment counts work cleanly)
INSERT INTO dbo.Users (FullName, Email, PasswordHash, RoleID, ProfilePictureURL)
VALUES
    ('Thabo Nkosi',     'thabo.nkosi@raceday.co.za',     'hashed_pw_1', 1, NULL), -- Organiser
    ('Lindiwe Dube',     'lindiwe.dube@raceday.co.za',    'hashed_pw_2', 1, NULL), -- Organiser
    ('Sipho Mokoena',    'sipho.mokoena@example.com',     'hashed_pw_3', 2, NULL), -- Participant
    ('Anika van Wyk',    'anika.vanwyk@example.com',      'hashed_pw_4', 2, NULL), -- Participant
    ('Naledi Molefe',    'naledi.molefe@example.com',     'hashed_pw_5', 2, NULL); -- Participant

-- Events: 3 events
INSERT INTO dbo.Events (Name, Description, EventDate, Location, DistanceKm, EventType, BannerImageURL, OrganiserID)
VALUES
    ('Johannesburg City Run',   'Annual road running event through the Johannesburg CBD.',        '2026-11-14 07:00:00', 'Johannesburg, Gauteng',   10.0,  'Run',   NULL, 1),
    ('Cape Town Cycle Tour',    'Scenic cycling event around the Cape Peninsula.',                  '2026-03-08 06:30:00', 'Cape Town, Western Cape', 109.0, 'Cycle', NULL, 2),
    ('Durban Beachfront Walk',  'Family-friendly charity walk along the Durban beachfront.',        '2026-09-20 08:00:00', 'Durban, KwaZulu-Natal',   3.0,   'Walk',  NULL, 1);

-- Categories: at least one per event (mix of distance-based and age-based)
INSERT INTO dbo.Categories (EventID, Name, DistanceKm, MaxParticipants)
VALUES
    (1, '5km Fun Run',   5.0,  500),
    (1, '10km Road Race',10.0, 300),
    (2, '109km Cycle',   109.0,1000),
    (2, '55km Cycle',    55.0, 800),
    (3, '3km Walk',      3.0,  400),
    (3, 'Under 20',      NULL, 150);

-- Enrolments: participants enrolled in categories
INSERT INTO dbo.Enrolments (UserID, CategoryID, Status)
VALUES
    (3, 1, 'Confirmed'), -- Sipho -> 5km Fun Run (Joburg)
    (4, 2, 'Confirmed'), -- Anika -> 10km Road Race (Joburg)
    (5, 3, 'Confirmed'), -- Naledi -> 109km Cycle (Cape Town)
    (3, 5, 'Confirmed'); -- Sipho -> 3km Walk (Durban)
