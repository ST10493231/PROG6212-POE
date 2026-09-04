-- RaceDay Database Schema
-- System Planning and Database - Part 1
-- Target: Microsoft SQL Server (SSMS)

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

