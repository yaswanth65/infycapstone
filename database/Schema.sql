-- ============================================================
-- Event Management System - SQL Server Schema
-- This script creates the database schema matching the EF Core model.
-- Run against a SQL Server instance (e.g. LocalDB).
-- ============================================================

IF DB_ID(N'EventManagementDB') IS NULL
BEGIN
    CREATE DATABASE [EventManagementDB];
END
GO

USE [EventManagementDB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------------------------
-- Roles
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [RoleId]      INT             NOT NULL IDENTITY(1,1),
        [RoleName]    NVARCHAR(50)    NOT NULL,
        [IsActive]    BIT             NOT NULL CONSTRAINT [DF_Roles_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(0)   NOT NULL CONSTRAINT [DF_Roles_CreatedAtUtc] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_Roles] PRIMARY KEY ([RoleId])
    );
    CREATE UNIQUE INDEX [UQ_Roles_RoleName] ON [dbo].[Roles] ([RoleName]);
END
GO

-- ------------------------------------------------------------
-- Users
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Users] (
        [UserId]         BIGINT          NOT NULL IDENTITY(1,1),
        [Email]          NVARCHAR(256)   NOT NULL,
        [UserName]       NVARCHAR(100)   NOT NULL,
        [PasswordHash]   VARBINARY(256)  NOT NULL,
        [PasswordSalt]   VARBINARY(128)  NOT NULL,
        [DisplayName]    NVARCHAR(150)   NOT NULL,
        [PhoneNumber]    NVARCHAR(20)    NULL,
        [RoleId]         INT             NOT NULL,
        [IsActive]       BIT             NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
        [DeactivatedAtUtc] DATETIME2(0)  NULL,
        [CreatedAtUtc]   DATETIME2(0)    NOT NULL CONSTRAINT [DF_Users_CreatedAtUtc] DEFAULT (sysutcdatetime()),
        [UpdatedAtUtc]   DATETIME2(0)    NULL,
        [RowVersion]     ROWVERSION      NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_Users_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [UQ_Users_Email] ON [dbo].[Users] ([Email]);
    CREATE UNIQUE INDEX [UQ_Users_UserName] ON [dbo].[Users] ([UserName]);
    CREATE UNIQUE INDEX [UX_Users_Email_Active] ON [dbo].[Users] ([Email]) WHERE ([IsActive] = (1));
    CREATE INDEX [IX_Users_UserName] ON [dbo].[Users] ([UserName]);
    CREATE INDEX [IX_Users_RoleId_IsActive] ON [dbo].[Users] ([RoleId], [IsActive]);
END
GO

-- ------------------------------------------------------------
-- Events
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.Events', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Events] (
        [EventId]             BIGINT         NOT NULL IDENTITY(1,1),
        [Title]               NVARCHAR(200)  NOT NULL,
        [Description]         NVARCHAR(MAX)  NULL,
        [Venue]               NVARCHAR(250)  NOT NULL,
        [StartAtUtc]          DATETIME2(0)   NOT NULL,
        [EndAtUtc]            DATETIME2(0)   NOT NULL,
        [RegistrationOpenAtUtc]  DATETIME2(0) NULL,
        [RegistrationCloseAtUtc] DATETIME2(0) NULL,
        [Capacity]            INT            NOT NULL,
        [Status]              VARCHAR(20)    NOT NULL CONSTRAINT [DF_Events_Status] DEFAULT ('Draft'),
        [OrganizerUserId]     BIGINT         NOT NULL,
        [PublishedAtUtc]      DATETIME2(0)   NULL,
        [ClosedAtUtc]         DATETIME2(0)   NULL,
        [CancelledAtUtc]      DATETIME2(0)   NULL,
        [CreatedAtUtc]        DATETIME2(0)   NOT NULL CONSTRAINT [DF_Events_CreatedAtUtc] DEFAULT (sysutcdatetime()),
        [UpdatedAtUtc]        DATETIME2(0)   NULL,
        [RowVersion]          ROWVERSION     NOT NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([EventId]),
        CONSTRAINT [FK_Events_Organizer] FOREIGN KEY ([OrganizerUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Events_EndAtUtc] ON [dbo].[Events] ([EndAtUtc]);
    CREATE INDEX [IX_Events_StartAtUtc] ON [dbo].[Events] ([StartAtUtc]);
    CREATE INDEX [IX_Events_OrganizerUserId] ON [dbo].[Events] ([OrganizerUserId], [Status]);
    CREATE INDEX [IX_Events_Status_StartAtUtc] ON [dbo].[Events] ([Status], [StartAtUtc]);
END
GO

-- ------------------------------------------------------------
-- Registrations
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.Registrations', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Registrations] (
        [RegistrationId]       BIGINT        NOT NULL IDENTITY(1,1),
        [EventId]              BIGINT        NOT NULL,
        [AttendeeUserId]       BIGINT        NOT NULL,
        [RegistrationStatus]   VARCHAR(20)   NOT NULL CONSTRAINT [DF_Registrations_Status] DEFAULT ('Confirmed'),
        [Source]               VARCHAR(20)   NULL,
        [RegistrationRequestId] BIGINT       NULL,
        [RegisteredAtUtc]      DATETIME2(0)  NOT NULL CONSTRAINT [DF_Registrations_RegisteredAtUtc] DEFAULT (sysutcdatetime()),
        [CancelledAtUtc]       DATETIME2(0)  NULL,
        [CancelledByUserId]    BIGINT        NULL,
        [CancelReason]         NVARCHAR(300) NULL,
        [RowVersion]           ROWVERSION    NOT NULL,
        CONSTRAINT [PK_Registrations] PRIMARY KEY ([RegistrationId]),
        CONSTRAINT [FK_Registrations_Event] FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events] ([EventId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Registrations_Attendee] FOREIGN KEY ([AttendeeUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Registrations_CancelledBy] FOREIGN KEY ([CancelledByUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Registrations_AttendeeUserId] ON [dbo].[Registrations] ([AttendeeUserId], [RegistrationStatus]);
    CREATE INDEX [IX_Registrations_EventId_Status] ON [dbo].[Registrations] ([EventId], [RegistrationStatus]);
    CREATE INDEX [IX_Registrations_RegistrationStatus] ON [dbo].[Registrations] ([RegistrationStatus]);
    CREATE UNIQUE INDEX [UX_Registrations_Event_Attendee_Confirmed] ON [dbo].[Registrations] ([EventId], [AttendeeUserId]) WHERE ([RegistrationStatus] = 'Confirmed');
END
GO

-- ------------------------------------------------------------
-- WaitlistEntries
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.WaitlistEntries', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WaitlistEntries] (
        [WaitlistEntryId]       BIGINT       NOT NULL IDENTITY(1,1),
        [EventId]               BIGINT       NOT NULL,
        [AttendeeUserId]        BIGINT       NOT NULL,
        [WaitlistStatus]        VARCHAR(20)  NOT NULL CONSTRAINT [DF_Waitlist_Status] DEFAULT ('Waiting'),
        [QueuedAtUtc]           DATETIME2(0) NOT NULL CONSTRAINT [DF_Waitlist_QueuedAtUtc] DEFAULT (sysutcdatetime()),
        [PromotedAtUtc]         DATETIME2(0) NULL,
        [PromotedToRegistrationId] BIGINT     NULL,
        [RemovedAtUtc]          DATETIME2(0) NULL,
        [RemovedReason]         NVARCHAR(300) NULL,
        CONSTRAINT [PK_WaitlistEntries] PRIMARY KEY ([WaitlistEntryId]),
        CONSTRAINT [FK_Waitlist_Event] FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events] ([EventId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Waitlist_Attendee] FOREIGN KEY ([AttendeeUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Waitlist_Promoted] FOREIGN KEY ([PromotedToRegistrationId]) REFERENCES [dbo].[Registrations] ([RegistrationId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_WaitlistEntries_EventId_Status] ON [dbo].[WaitlistEntries] ([EventId], [WaitlistStatus]);
    CREATE UNIQUE INDEX [UX_WaitlistEntries_Event_Attendee_Waiting] ON [dbo].[WaitlistEntries] ([EventId], [AttendeeUserId]) WHERE ([WaitlistStatus] = 'Waiting');
END
GO

-- ------------------------------------------------------------
-- AttendanceRecords
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.AttendanceRecords', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AttendanceRecords] (
        [AttendanceRecordId]  BIGINT       NOT NULL IDENTITY(1,1),
        [RegistrationId]      BIGINT       NOT NULL,
        [AttendanceStatus]    VARCHAR(20)  NOT NULL,
        [RecordedByUserId]    BIGINT       NOT NULL,
        [RecordedAtUtc]       DATETIME2(0) NOT NULL CONSTRAINT [DF_Attendance_RecordedAtUtc] DEFAULT (sysutcdatetime()),
        [IsFinalized]         BIT          NOT NULL CONSTRAINT [DF_Attendance_IsFinalized] DEFAULT (0),
        [FinalizedAtUtc]      DATETIME2(0) NULL,
        [CorrectedByUserId]   BIGINT       NULL,
        [CorrectedAtUtc]      DATETIME2(0) NULL,
        [CorrectionReason]    NVARCHAR(400) NULL,
        [RevisionNo]          INT          NOT NULL CONSTRAINT [DF_Attendance_RevisionNo] DEFAULT (1),
        CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([AttendanceRecordId]),
        CONSTRAINT [FK_Attendance_Registration] FOREIGN KEY ([RegistrationId]) REFERENCES [dbo].[Registrations] ([RegistrationId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Attendance_RecordedBy] FOREIGN KEY ([RecordedByUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Attendance_CorrectedBy] FOREIGN KEY ([CorrectedByUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [UQ__Attendan__6EF58811CEEF5B67] ON [dbo].[AttendanceRecords] ([RegistrationId]);
END
GO

-- ------------------------------------------------------------
-- EventStatusHistory
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.EventStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EventStatusHistory] (
        [EventStatusHistoryId] BIGINT       NOT NULL IDENTITY(1,1),
        [EventId]              BIGINT       NOT NULL,
        [FromStatus]           VARCHAR(20)  NULL,
        [ToStatus]             VARCHAR(20)  NOT NULL,
        [ChangedByUserId]      BIGINT       NOT NULL,
        [ChangedAtUtc]         DATETIME2(0) NOT NULL CONSTRAINT [DF_EventStatusHistory_ChangedAtUtc] DEFAULT (sysutcdatetime()),
        [Remarks]              NVARCHAR(500) NULL,
        CONSTRAINT [PK_EventStatusHistory] PRIMARY KEY ([EventStatusHistoryId]),
        CONSTRAINT [FK_EventStatusHistory_Event] FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events] ([EventId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventStatusHistory_ChangedBy] FOREIGN KEY ([ChangedByUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_EventStatusHistory_ChangedAtUtc] ON [dbo].[EventStatusHistory] ([ChangedAtUtc] DESC);
    CREATE INDEX [IX_EventStatusHistory_EventId] ON [dbo].[EventStatusHistory] ([EventId]);
END
GO

-- ------------------------------------------------------------
-- RegistrationRequests
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.RegistrationRequests', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RegistrationRequests] (
        [RegistrationRequestId] BIGINT       NOT NULL IDENTITY(1,1),
        [EventId]               BIGINT       NOT NULL,
        [AttendeeUserId]        BIGINT       NOT NULL,
        [RequestedByUserId]     BIGINT       NOT NULL,
        [RequestType]           VARCHAR(20)  NOT NULL CONSTRAINT [DF_RegistrationRequests_RequestType] DEFAULT ('OnBehalf'),
        [RequestStatus]         VARCHAR(20)  NOT NULL,
        [RequestedAtUtc]        DATETIME2(0) NOT NULL CONSTRAINT [DF_RegistrationRequests_RequestedAtUtc] DEFAULT (sysutcdatetime()),
        [RespondedAtUtc]        DATETIME2(0) NULL,
        [ResponseComment]       NVARCHAR(500) NULL,
        [LinkedRegistrationId]  BIGINT       NULL,
        CONSTRAINT [PK_RegistrationRequests] PRIMARY KEY ([RegistrationRequestId]),
        CONSTRAINT [FK_RegistrationRequests_Event] FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events] ([EventId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RegistrationRequests_Attendee] FOREIGN KEY ([AttendeeUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RegistrationRequests_RequestedBy] FOREIGN KEY ([RequestedByUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_RegistrationRequests_EventId] ON [dbo].[RegistrationRequests] ([EventId], [RequestStatus]);
    CREATE INDEX [IX_RegistrationRequests_RequestStatus] ON [dbo].[RegistrationRequests] ([RequestStatus]);
    CREATE UNIQUE INDEX [UQ__Registra__DC196ACBDB11336B] ON [dbo].[RegistrationRequests] ([LinkedRegistrationId]) WHERE ([LinkedRegistrationId] IS NOT NULL);
    CREATE UNIQUE INDEX [UX_RegistrationRequests_Event_Attendee_Pending] ON [dbo].[RegistrationRequests] ([EventId], [AttendeeUserId]) WHERE ([RequestStatus] = 'Pending');
END
GO

-- Add LinkedRegistrationId FK after both tables exist
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RegistrationRequests_LinkedRegistration')
BEGIN
    ALTER TABLE [dbo].[RegistrationRequests]
        ADD CONSTRAINT [FK_RegistrationRequests_LinkedRegistration]
        FOREIGN KEY ([LinkedRegistrationId]) REFERENCES [dbo].[Registrations] ([RegistrationId]) ON DELETE NO ACTION;
END
GO

-- ------------------------------------------------------------
-- Notifications
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId]       BIGINT        NOT NULL IDENTITY(1,1),
        [RecipientUserId]      BIGINT        NOT NULL,
        [NotificationType]     VARCHAR(40)   NOT NULL,
        [Title]                NVARCHAR(200) NOT NULL,
        [Message]              NVARCHAR(1000) NOT NULL,
        [RelatedEventId]       BIGINT        NULL,
        [RelatedRegistrationId] BIGINT       NULL,
        [RelatedRequestId]     BIGINT        NULL,
        [DeliveryStatus]       VARCHAR(20)   NOT NULL CONSTRAINT [DF_Notifications_DeliveryStatus] DEFAULT ('Pending'),
        [ScheduledAtUtc]       DATETIME2(0)  NULL,
        [SentAtUtc]            DATETIME2(0)  NULL,
        [ReadAtUtc]            DATETIME2(0)  NULL,
        [CreatedAtUtc]         DATETIME2(0)  NOT NULL CONSTRAINT [DF_Notifications_CreatedAtUtc] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
        CONSTRAINT [FK_Notifications_Recipient] FOREIGN KEY ([RecipientUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Notifications_Event] FOREIGN KEY ([RelatedEventId]) REFERENCES [dbo].[Events] ([EventId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Notifications_Registration] FOREIGN KEY ([RelatedRegistrationId]) REFERENCES [dbo].[Registrations] ([RegistrationId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Notifications_Request] FOREIGN KEY ([RelatedRequestId]) REFERENCES [dbo].[RegistrationRequests] ([RegistrationRequestId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Notifications_RecipientUserId_DeliveryStatus_CreatedAtUtc] ON [dbo].[Notifications] ([RecipientUserId], [DeliveryStatus], [CreatedAtUtc] DESC);
END
GO

-- ------------------------------------------------------------
-- AuditRecords
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.AuditRecords', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AuditRecords] (
        [AuditRecordId]    BIGINT        NOT NULL IDENTITY(1,1),
        [ActorUserId]      BIGINT        NULL,
        [ActionType]       VARCHAR(40)   NOT NULL,
        [TargetEntity]     VARCHAR(50)   NOT NULL,
        [TargetEntityId]   BIGINT        NULL,
        [EventId]          BIGINT        NULL,
        [Outcome]          VARCHAR(20)   NOT NULL,
        [MetadataJson]     NVARCHAR(MAX) NULL,
        [CreatedAtUtc]     DATETIME2(0)  NOT NULL CONSTRAINT [DF_AuditRecords_CreatedAtUtc] DEFAULT (sysutcdatetime()),
        [IpAddress]        NVARCHAR(45)  NULL,
        CONSTRAINT [PK_AuditRecords] PRIMARY KEY ([AuditRecordId]),
        CONSTRAINT [FK_AuditRecords_Actor] FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditRecords_Event] FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events] ([EventId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_AuditRecords_ActorUserId] ON [dbo].[AuditRecords] ([ActorUserId], [CreatedAtUtc] DESC);
    CREATE INDEX [IX_AuditRecords_CreatedAtUtc] ON [dbo].[AuditRecords] ([CreatedAtUtc] DESC);
    CREATE INDEX [IX_AuditRecords_EventId] ON [dbo].[AuditRecords] ([EventId], [CreatedAtUtc] DESC);
END
GO

-- ------------------------------------------------------------
-- Seed data: System Roles
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [RoleName] = N'Administrator')
    INSERT INTO [dbo].[Roles] ([RoleName]) VALUES (N'Administrator');
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [RoleName] = N'EventManager')
    INSERT INTO [dbo].[Roles] ([RoleName]) VALUES (N'EventManager');
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [RoleName] = N'BusinessManagement')
    INSERT INTO [dbo].[Roles] ([RoleName]) VALUES (N'BusinessManagement');
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [RoleName] = N'Attendee')
    INSERT INTO [dbo].[Roles] ([RoleName]) VALUES (N'Attendee');
GO

-- ------------------------------------------------------------
-- Seed System Users (password for all: Password@123)
-- Uses CAST('Password@123' AS VARBINARY(256)) to store standard UTF-8 bytes matching backend auth service.
-- ------------------------------------------------------------

-- 1. System Administrator
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserName] = N'admin')
BEGIN
    DECLARE @adminRoleId INT = (SELECT TOP 1 [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = N'Administrator');
    INSERT INTO [dbo].[Users]
        ([Email], [UserName], [PasswordHash], [PasswordSalt], [DisplayName], [PhoneNumber], [RoleId], [IsActive], [CreatedAtUtc])
    VALUES
        (N'admin@eventmanagement.com', N'admin',
         CAST('Password@123' AS VARBINARY(256)),
         CAST(0x00000000000000000000000000000000 AS VARBINARY(128)),
         N'System Administrator', N'9998887770', @adminRoleId, 1, SYSUTCDATETIME());
END
GO

-- 2. Lead Event Manager
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserName] = N'manager')
BEGIN
    DECLARE @mgrRoleId INT = (SELECT TOP 1 [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = N'EventManager');
    INSERT INTO [dbo].[Users]
        ([Email], [UserName], [PasswordHash], [PasswordSalt], [DisplayName], [PhoneNumber], [RoleId], [IsActive], [CreatedAtUtc])
    VALUES
        (N'manager@eventmanagement.com', N'manager',
         CAST('Password@123' AS VARBINARY(256)),
         CAST(0x00000000000000000000000000000000 AS VARBINARY(128)),
         N'Lead Event Manager', N'9998887771', @mgrRoleId, 1, SYSUTCDATETIME());
END
GO

-- 3. Associate Event Manager
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserName] = N'manager2')
BEGIN
    DECLARE @mgr2RoleId INT = (SELECT TOP 1 [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = N'EventManager');
    INSERT INTO [dbo].[Users]
        ([Email], [UserName], [PasswordHash], [PasswordSalt], [DisplayName], [PhoneNumber], [RoleId], [IsActive], [CreatedAtUtc])
    VALUES
        (N'manager2@eventmanagement.com', N'manager2',
         CAST('Password@123' AS VARBINARY(256)),
         CAST(0x00000000000000000000000000000000 AS VARBINARY(128)),
         N'Sarah Manager', N'9998887779', @mgr2RoleId, 1, SYSUTCDATETIME());
END
GO

-- 4. Executive Business Director (Primary Business Management User)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserName] = N'business')
BEGIN
    DECLARE @bizRoleId INT = (SELECT TOP 1 [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = N'BusinessManagement');
    INSERT INTO [dbo].[Users]
        ([Email], [UserName], [PasswordHash], [PasswordSalt], [DisplayName], [PhoneNumber], [RoleId], [IsActive], [CreatedAtUtc])
    VALUES
        (N'business@eventmanagement.com', N'business',
         CAST('Password@123' AS VARBINARY(256)),
         CAST(0x00000000000000000000000000000000 AS VARBINARY(128)),
         N'Executive Business Director', N'9998887772', @bizRoleId, 1, SYSUTCDATETIME());
END
GO

-- 5. Business Operations Manager (Secondary Business Management User)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserName] = N'business2')
BEGIN
    DECLARE @biz2RoleId INT = (SELECT TOP 1 [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = N'BusinessManagement');
    INSERT INTO [dbo].[Users]
        ([Email], [UserName], [PasswordHash], [PasswordSalt], [DisplayName], [PhoneNumber], [RoleId], [IsActive], [CreatedAtUtc])
    VALUES
        (N'business2@eventmanagement.com', N'business2',
         CAST('Password@123' AS VARBINARY(256)),
         CAST(0x00000000000000000000000000000000 AS VARBINARY(128)),
         N'Carol Business Operations', N'9998887778', @biz2RoleId, 1, SYSUTCDATETIME());
END
GO

-- 6. Attendee User 1 (John Doe)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserName] = N'attendee1')
BEGIN
    DECLARE @attRoleId INT = (SELECT TOP 1 [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = N'Attendee');
    INSERT INTO [dbo].[Users]
        ([Email], [UserName], [PasswordHash], [PasswordSalt], [DisplayName], [PhoneNumber], [RoleId], [IsActive], [CreatedAtUtc])
    VALUES
        (N'attendee1@eventmanagement.com', N'attendee1',
         CAST('Password@123' AS VARBINARY(256)),
         CAST(0x00000000000000000000000000000000 AS VARBINARY(128)),
         N'John Doe Attendee', N'9998887773', @attRoleId, 1, SYSUTCDATETIME());
END
GO

-- 7. Attendee User 2 (Jane Smith)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserName] = N'attendee2')
BEGIN
    DECLARE @attRoleId2 INT = (SELECT TOP 1 [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = N'Attendee');
    INSERT INTO [dbo].[Users]
        ([Email], [UserName], [PasswordHash], [PasswordSalt], [DisplayName], [PhoneNumber], [RoleId], [IsActive], [CreatedAtUtc])
    VALUES
        (N'attendee2@eventmanagement.com', N'attendee2',
         CAST('Password@123' AS VARBINARY(256)),
         CAST(0x00000000000000000000000000000000 AS VARBINARY(128)),
         N'Jane Smith Attendee', N'9998887774', @attRoleId2, 1, SYSUTCDATETIME());
END
GO

-- 8. Attendee User 3 (Bob Brown)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserName] = N'attendee3')
BEGIN
    DECLARE @attRoleId3 INT = (SELECT TOP 1 [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = N'Attendee');
    INSERT INTO [dbo].[Users]
        ([Email], [UserName], [PasswordHash], [PasswordSalt], [DisplayName], [PhoneNumber], [RoleId], [IsActive], [CreatedAtUtc])
    VALUES
        (N'attendee3@eventmanagement.com', N'attendee3',
         CAST('Password@123' AS VARBINARY(256)),
         CAST(0x00000000000000000000000000000000 AS VARBINARY(128)),
         N'Bob Brown Attendee', N'9998887775', @attRoleId3, 1, SYSUTCDATETIME());
END
GO

-- 9. Attendee User 4 (Alice Johnson)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserName] = N'attendee4')
BEGIN
    DECLARE @attRoleId4 INT = (SELECT TOP 1 [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = N'Attendee');
    INSERT INTO [dbo].[Users]
        ([Email], [UserName], [PasswordHash], [PasswordSalt], [DisplayName], [PhoneNumber], [RoleId], [IsActive], [CreatedAtUtc])
    VALUES
        (N'attendee4@eventmanagement.com', N'attendee4',
         CAST('Password@123' AS VARBINARY(256)),
         CAST(0x00000000000000000000000000000000 AS VARBINARY(128)),
         N'Alice Johnson Attendee', N'9998887776', @attRoleId4, 1, SYSUTCDATETIME());
END
GO

-- ------------------------------------------------------------
-- Seed Public & Managed Events (Including Single Capacity Test Cases)
-- ------------------------------------------------------------

-- Event 1: Global Tech Summit 2026 (Published, Upcoming, Capacity 100)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Events] WHERE [Title] = N'Global Tech Summit 2026')
BEGIN
    DECLARE @organizerId BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager');
    IF @organizerId IS NULL SET @organizerId = 1;

    INSERT INTO [dbo].[Events]
        ([Title], [Description], [Venue], [StartAtUtc], [EndAtUtc], [RegistrationOpenAtUtc], [RegistrationCloseAtUtc], [Capacity], [Status], [OrganizerUserId], [PublishedAtUtc], [CreatedAtUtc])
    VALUES
        (N'Global Tech Summit 2026', N'Annual Developer Conference covering Cloud, AI, and DevOps.', N'Convention Hall A',
         DATEADD(DAY, 30, SYSUTCDATETIME()), DATEADD(DAY, 31, SYSUTCDATETIME()),
         SYSUTCDATETIME(), DATEADD(DAY, 29, SYSUTCDATETIME()),
         100, 'Published', @organizerId, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

-- Event 2: AI & Cloud Innovation Summit (Published, Upcoming, Capacity 2 - FULL)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Events] WHERE [Title] = N'AI & Cloud Innovation Summit')
BEGIN
    DECLARE @organizerIdA BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager');
    INSERT INTO [dbo].[Events]
        ([Title], [Description], [Venue], [StartAtUtc], [EndAtUtc], [RegistrationOpenAtUtc], [RegistrationCloseAtUtc], [Capacity], [Status], [OrganizerUserId], [PublishedAtUtc], [CreatedAtUtc])
    VALUES
        (N'AI & Cloud Innovation Summit', N'Hands-on sessions on AI workloads and cloud-native architecture.', N'Innovation Wing – CISCO Hall',
         DATEADD(DAY, 15, SYSUTCDATETIME()), DATEADD(DAY, 16, SYSUTCDATETIME()),
         SYSUTCDATETIME(), DATEADD(DAY, 14, SYSUTCDATETIME()),
         2, 'Published', @organizerIdA, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

-- Event 3: Quantum Computing & Encryption 2026 (SINGLE CAPACITY EVENT - Capacity 1 - FULL)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Events] WHERE [Title] = N'Quantum Computing & Encryption 2026')
BEGIN
    DECLARE @organizerIdQ BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager');
    INSERT INTO [dbo].[Events]
        ([Title], [Description], [Venue], [StartAtUtc], [EndAtUtc], [RegistrationOpenAtUtc], [RegistrationCloseAtUtc], [Capacity], [Status], [OrganizerUserId], [PublishedAtUtc], [CreatedAtUtc])
    VALUES
        (N'Quantum Computing & Encryption 2026', N'Exclusive single-seat hands-on quantum simulator workshop.', N'Quantum Lab 101',
         DATEADD(DAY, 20, SYSUTCDATETIME()), DATEADD(DAY, 21, SYSUTCDATETIME()),
         SYSUTCDATETIME(), DATEADD(DAY, 19, SYSUTCDATETIME()),
         1, 'Published', @organizerIdQ, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

-- Event 4: Executive AI Strategy Forum (SINGLE CAPACITY EVENT - Capacity 1 - FULL)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Events] WHERE [Title] = N'Executive AI Strategy Forum')
BEGIN
    DECLARE @organizerIdE BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager2');
    IF @organizerIdE IS NULL SET @organizerIdE = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager');
    INSERT INTO [dbo].[Events]
        ([Title], [Description], [Venue], [StartAtUtc], [EndAtUtc], [RegistrationOpenAtUtc], [RegistrationCloseAtUtc], [Capacity], [Status], [OrganizerUserId], [PublishedAtUtc], [CreatedAtUtc])
    VALUES
        (N'Executive AI Strategy Forum', N'High-level 1-on-1 enterprise strategy alignment roundtable.', N'Executive Suite 500',
         DATEADD(DAY, 12, SYSUTCDATETIME()), DATEADD(DAY, 13, SYSUTCDATETIME()),
         SYSUTCDATETIME(), DATEADD(DAY, 11, SYSUTCDATETIME()),
         1, 'Published', @organizerIdE, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

-- Event 5: Enterprise Microservices & Cloud-Native (Published, Upcoming, Capacity 150)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Events] WHERE [Title] = N'Enterprise Microservices & Cloud-Native')
BEGIN
    DECLARE @organizerIdM BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager');
    INSERT INTO [dbo].[Events]
        ([Title], [Description], [Venue], [StartAtUtc], [EndAtUtc], [RegistrationOpenAtUtc], [RegistrationCloseAtUtc], [Capacity], [Status], [OrganizerUserId], [PublishedAtUtc], [CreatedAtUtc])
    VALUES
        (N'Enterprise Microservices & Cloud-Native', N'Deep dive into Kubernetes, gRPC, and service mesh architectures.', N'Grand Ballroom B',
         DATEADD(DAY, 45, SYSUTCDATETIME()), DATEADD(DAY, 46, SYSUTCDATETIME()),
         SYSUTCDATETIME(), DATEADD(DAY, 44, SYSUTCDATETIME()),
         150, 'Published', @organizerIdM, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

-- Event 6: Product Leadership Conference 2026 (Published, Upcoming, Capacity 75)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Events] WHERE [Title] = N'Product Leadership Conference 2026')
BEGIN
    DECLARE @organizerIdP BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager2');
    IF @organizerIdP IS NULL SET @organizerIdP = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager');
    INSERT INTO [dbo].[Events]
        ([Title], [Description], [Venue], [StartAtUtc], [EndAtUtc], [RegistrationOpenAtUtc], [RegistrationCloseAtUtc], [Capacity], [Status], [OrganizerUserId], [PublishedAtUtc], [CreatedAtUtc])
    VALUES
        (N'Product Leadership Conference 2026', N'Strategy, metrics, and growth frameworks for product executives.', N'Auditorium East',
         DATEADD(DAY, 50, SYSUTCDATETIME()), DATEADD(DAY, 51, SYSUTCDATETIME()),
         SYSUTCDATETIME(), DATEADD(DAY, 49, SYSUTCDATETIME()),
         75, 'Published', @organizerIdP, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

-- Event 7: DevOps Masterclass Bootcamp (Closed, Past Event, Capacity 50)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Events] WHERE [Title] = N'DevOps Masterclass Bootcamp')
BEGIN
    DECLARE @organizerIdD BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager');
    INSERT INTO [dbo].[Events]
        ([Title], [Description], [Venue], [StartAtUtc], [EndAtUtc], [RegistrationOpenAtUtc], [RegistrationCloseAtUtc], [Capacity], [Status], [OrganizerUserId], [PublishedAtUtc], [ClosedAtUtc], [CreatedAtUtc])
    VALUES
        (N'DevOps Masterclass Bootcamp', N'CI/CD pipelines, container orchestration, and SRE practices.', N'Engineering Lab 3',
         DATEADD(DAY, -10, SYSUTCDATETIME()), DATEADD(DAY, -9, SYSUTCDATETIME()),
         DATEADD(DAY, -40, SYSUTCDATETIME()), DATEADD(DAY, -11, SYSUTCDATETIME()),
         50, 'Closed', @organizerIdD, DATEADD(DAY, -35, SYSUTCDATETIME()), DATEADD(DAY, -8, SYSUTCDATETIME()), DATEADD(DAY, -45, SYSUTCDATETIME()));
END
GO

-- Event 8: UX/UI Design Workshop (Draft, Upcoming, Capacity 30)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Events] WHERE [Title] = N'UX/UI Design Workshop')
BEGIN
    DECLARE @organizerIdU BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager');
    INSERT INTO [dbo].[Events]
        ([Title], [Description], [Venue], [StartAtUtc], [EndAtUtc], [RegistrationOpenAtUtc], [RegistrationCloseAtUtc], [Capacity], [Status], [OrganizerUserId], [CreatedAtUtc])
    VALUES
        (N'UX/UI Design Workshop', N'Design thinking, prototyping, and accessibility fundamentals.', N'Design Studio B',
         DATEADD(DAY, 40, SYSUTCDATETIME()), DATEADD(DAY, 41, SYSUTCDATETIME()),
         DATEADD(DAY, 25, SYSUTCDATETIME()), DATEADD(DAY, 39, SYSUTCDATETIME()),
         30, 'Draft', @organizerIdU, DATEADD(DAY, -3, SYSUTCDATETIME()));
END
GO

-- Event 9: Agile Leadership Roundtable (Cancelled, Past Event, Capacity 25)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Events] WHERE [Title] = N'Agile Leadership Roundtable')
BEGIN
    DECLARE @organizerIdR BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager');
    INSERT INTO [dbo].[Events]
        ([Title], [Description], [Venue], [StartAtUtc], [EndAtUtc], [RegistrationOpenAtUtc], [RegistrationCloseAtUtc], [Capacity], [Status], [OrganizerUserId], [PublishedAtUtc], [CancelledAtUtc], [CreatedAtUtc])
    VALUES
        (N'Agile Leadership Roundtable', N'Forum for scrum masters and engineering leads to share practices.', N'Board Room 2',
         DATEADD(DAY, -5, SYSUTCDATETIME()), DATEADD(DAY, -4, SYSUTCDATETIME()),
         DATEADD(DAY, -30, SYSUTCDATETIME()), DATEADD(DAY, -6, SYSUTCDATETIME()),
         25, 'Cancelled', @organizerIdR, DATEADD(DAY, -28, SYSUTCDATETIME()), DATEADD(DAY, -3, SYSUTCDATETIME()), DATEADD(DAY, -35, SYSUTCDATETIME()));
END
GO

-- Event 10: Cybersecurity Best Practices 2026 (Published, Upcoming, Capacity 50)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Events] WHERE [Title] = N'Cybersecurity Best Practices 2026')
BEGIN
    DECLARE @organizerId2 BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager2');
    IF @organizerId2 IS NULL SET @organizerId2 = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager');

    INSERT INTO [dbo].[Events]
        ([Title], [Description], [Venue], [StartAtUtc], [EndAtUtc], [RegistrationOpenAtUtc], [RegistrationCloseAtUtc], [Capacity], [Status], [OrganizerUserId], [PublishedAtUtc], [CreatedAtUtc])
    VALUES
        (N'Cybersecurity Best Practices 2026', N'Zero Trust architecture, threat modeling, and modern defense.', N'Auditorium C',
         DATEADD(DAY, 25, SYSUTCDATETIME()), DATEADD(DAY, 26, SYSUTCDATETIME()),
         SYSUTCDATETIME(), DATEADD(DAY, 24, SYSUTCDATETIME()),
         50, 'Published', @organizerId2, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

-- ------------------------------------------------------------
-- Confirmed Registrations
-- ------------------------------------------------------------
DECLARE
    @att1Id  BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee1'),
    @att2Id  BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee2'),
    @att3Id  BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee3'),
    @att4Id  BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee4'),
    @globalId  BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Global Tech Summit 2026'),
    @aiId      BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'AI & Cloud Innovation Summit'),
    @quantumId BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Quantum Computing & Encryption 2026'),
    @execAiId  BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Executive AI Strategy Forum'),
    @devopsId  BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'DevOps Masterclass Bootcamp'),
    @cyberId   BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Cybersecurity Best Practices 2026');

-- Registrations for Global Tech Summit 2026 (Cap 100)
IF @att1Id IS NOT NULL AND @globalId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Registrations] WHERE [EventId] = @globalId AND [AttendeeUserId] = @att1Id AND [RegistrationStatus] = 'Confirmed')
    INSERT INTO [dbo].[Registrations] ([EventId], [AttendeeUserId], [RegistrationStatus], [Source], [RegisteredAtUtc])
    VALUES (@globalId, @att1Id, 'Confirmed', 'Self', DATEADD(DAY, -4, SYSUTCDATETIME()));

IF @att2Id IS NOT NULL AND @globalId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Registrations] WHERE [EventId] = @globalId AND [AttendeeUserId] = @att2Id AND [RegistrationStatus] = 'Confirmed')
    INSERT INTO [dbo].[Registrations] ([EventId], [AttendeeUserId], [RegistrationStatus], [Source], [RegisteredAtUtc])
    VALUES (@globalId, @att2Id, 'Confirmed', 'Self', DATEADD(DAY, -3, SYSUTCDATETIME()));

IF @att3Id IS NOT NULL AND @globalId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Registrations] WHERE [EventId] = @globalId AND [AttendeeUserId] = @att3Id AND [RegistrationStatus] = 'Confirmed')
    INSERT INTO [dbo].[Registrations] ([EventId], [AttendeeUserId], [RegistrationStatus], [Source], [RegisteredAtUtc])
    VALUES (@globalId, @att3Id, 'Confirmed', 'OnBehalf', DATEADD(DAY, -1, SYSUTCDATETIME()));

-- Registrations for AI Summit (Cap 2 -> Filled 2/2 by attendee1 & attendee4)
IF @att1Id IS NOT NULL AND @aiId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Registrations] WHERE [EventId] = @aiId AND [AttendeeUserId] = @att1Id AND [RegistrationStatus] = 'Confirmed')
    INSERT INTO [dbo].[Registrations] ([EventId], [AttendeeUserId], [RegistrationStatus], [Source], [RegisteredAtUtc])
    VALUES (@aiId, @att1Id, 'Confirmed', 'Self', DATEADD(DAY, -5, SYSUTCDATETIME()));

IF @att4Id IS NOT NULL AND @aiId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Registrations] WHERE [EventId] = @aiId AND [AttendeeUserId] = @att4Id AND [RegistrationStatus] = 'Confirmed')
    INSERT INTO [dbo].[Registrations] ([EventId], [AttendeeUserId], [RegistrationStatus], [Source], [RegisteredAtUtc])
    VALUES (@aiId, @att4Id, 'Confirmed', 'Self', DATEADD(DAY, -4, SYSUTCDATETIME()));

-- Registrations for Single Capacity Event 1: Quantum Computing (Cap 1 -> Filled 1/1 by attendee1)
IF @att1Id IS NOT NULL AND @quantumId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Registrations] WHERE [EventId] = @quantumId AND [AttendeeUserId] = @att1Id AND [RegistrationStatus] = 'Confirmed')
    INSERT INTO [dbo].[Registrations] ([EventId], [AttendeeUserId], [RegistrationStatus], [Source], [RegisteredAtUtc])
    VALUES (@quantumId, @att1Id, 'Confirmed', 'Self', DATEADD(DAY, -6, SYSUTCDATETIME()));

-- Registrations for Single Capacity Event 2: Executive AI Strategy (Cap 1 -> Filled 1/1 by attendee2)
IF @att2Id IS NOT NULL AND @execAiId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Registrations] WHERE [EventId] = @execAiId AND [AttendeeUserId] = @att2Id AND [RegistrationStatus] = 'Confirmed')
    INSERT INTO [dbo].[Registrations] ([EventId], [AttendeeUserId], [RegistrationStatus], [Source], [RegisteredAtUtc])
    VALUES (@execAiId, @att2Id, 'Confirmed', 'Self', DATEADD(DAY, -5, SYSUTCDATETIME()));

-- Registrations for DevOps Masterclass Bootcamp (Closed Event)
IF @att1Id IS NOT NULL AND @devopsId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Registrations] WHERE [EventId] = @devopsId AND [AttendeeUserId] = @att1Id AND [RegistrationStatus] = 'Confirmed')
    INSERT INTO [dbo].[Registrations] ([EventId], [AttendeeUserId], [RegistrationStatus], [Source], [RegisteredAtUtc])
    VALUES (@devopsId, @att1Id, 'Confirmed', 'Self', DATEADD(DAY, -20, SYSUTCDATETIME()));

IF @att2Id IS NOT NULL AND @devopsId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Registrations] WHERE [EventId] = @devopsId AND [AttendeeUserId] = @att2Id AND [RegistrationStatus] = 'Confirmed')
    INSERT INTO [dbo].[Registrations] ([EventId], [AttendeeUserId], [RegistrationStatus], [Source], [RegisteredAtUtc])
    VALUES (@devopsId, @att2Id, 'Confirmed', 'Self', DATEADD(DAY, -18, SYSUTCDATETIME()));

IF @att3Id IS NOT NULL AND @devopsId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Registrations] WHERE [EventId] = @devopsId AND [AttendeeUserId] = @att3Id AND [RegistrationStatus] = 'Confirmed')
    INSERT INTO [dbo].[Registrations] ([EventId], [AttendeeUserId], [RegistrationStatus], [Source], [RegisteredAtUtc])
    VALUES (@devopsId, @att3Id, 'Confirmed', 'Self', DATEADD(DAY, -17, SYSUTCDATETIME()));

-- Registration for Cybersecurity Summit
IF @att2Id IS NOT NULL AND @cyberId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Registrations] WHERE [EventId] = @cyberId AND [AttendeeUserId] = @att2Id AND [RegistrationStatus] = 'Confirmed')
    INSERT INTO [dbo].[Registrations] ([EventId], [AttendeeUserId], [RegistrationStatus], [Source], [RegisteredAtUtc])
    VALUES (@cyberId, @att2Id, 'Confirmed', 'Self', DATEADD(DAY, -1, SYSUTCDATETIME()));
GO

-- ------------------------------------------------------------
-- Waitlist Entries (Test Cases for Single & Low Capacity Events)
-- ------------------------------------------------------------
DECLARE
    @att1IdW BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee1'),
    @att2IdW BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee2'),
    @att3IdW BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee3'),
    @att4IdW BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee4'),
    @aiIdW      BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'AI & Cloud Innovation Summit'),
    @quantumIdW BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Quantum Computing & Encryption 2026'),
    @execAiIdW  BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Executive AI Strategy Forum');

-- AI Summit Waitlist (Cap 2 - 2/2 confirmed)
IF @att2IdW IS NOT NULL AND @aiIdW IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[WaitlistEntries] WHERE [EventId] = @aiIdW AND [AttendeeUserId] = @att2IdW AND [WaitlistStatus] = 'Waiting')
    INSERT INTO [dbo].[WaitlistEntries] ([EventId], [AttendeeUserId], [WaitlistStatus], [QueuedAtUtc])
    VALUES (@aiIdW, @att2IdW, 'Waiting', DATEADD(HOUR, -5, SYSUTCDATETIME()));

IF @att3IdW IS NOT NULL AND @aiIdW IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[WaitlistEntries] WHERE [EventId] = @aiIdW AND [AttendeeUserId] = @att3IdW AND [WaitlistStatus] = 'Waiting')
    INSERT INTO [dbo].[WaitlistEntries] ([EventId], [AttendeeUserId], [WaitlistStatus], [QueuedAtUtc])
    VALUES (@aiIdW, @att3IdW, 'Waiting', DATEADD(HOUR, -2, SYSUTCDATETIME()));

-- Quantum Computing Waitlist (Single Capacity 1 - 1/1 confirmed by attendee1)
-- attendee2 is Position #1, attendee3 is Position #2, attendee4 is Position #3
IF @att2IdW IS NOT NULL AND @quantumIdW IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[WaitlistEntries] WHERE [EventId] = @quantumIdW AND [AttendeeUserId] = @att2IdW AND [WaitlistStatus] = 'Waiting')
    INSERT INTO [dbo].[WaitlistEntries] ([EventId], [AttendeeUserId], [WaitlistStatus], [QueuedAtUtc])
    VALUES (@quantumIdW, @att2IdW, 'Waiting', DATEADD(HOUR, -10, SYSUTCDATETIME()));

IF @att3IdW IS NOT NULL AND @quantumIdW IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[WaitlistEntries] WHERE [EventId] = @quantumIdW AND [AttendeeUserId] = @att3IdW AND [WaitlistStatus] = 'Waiting')
    INSERT INTO [dbo].[WaitlistEntries] ([EventId], [AttendeeUserId], [WaitlistStatus], [QueuedAtUtc])
    VALUES (@quantumIdW, @att3IdW, 'Waiting', DATEADD(HOUR, -6, SYSUTCDATETIME()));

IF @att4IdW IS NOT NULL AND @quantumIdW IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[WaitlistEntries] WHERE [EventId] = @quantumIdW AND [AttendeeUserId] = @att4IdW AND [WaitlistStatus] = 'Waiting')
    INSERT INTO [dbo].[WaitlistEntries] ([EventId], [AttendeeUserId], [WaitlistStatus], [QueuedAtUtc])
    VALUES (@quantumIdW, @att4IdW, 'Waiting', DATEADD(HOUR, -1, SYSUTCDATETIME()));

-- Executive AI Strategy Waitlist (Single Capacity 1 - 1/1 confirmed by attendee2)
-- attendee1 is Position #1, attendee3 is Position #2
IF @att1IdW IS NOT NULL AND @execAiIdW IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[WaitlistEntries] WHERE [EventId] = @execAiIdW AND [AttendeeUserId] = @att1IdW AND [WaitlistStatus] = 'Waiting')
    INSERT INTO [dbo].[WaitlistEntries] ([EventId], [AttendeeUserId], [WaitlistStatus], [QueuedAtUtc])
    VALUES (@execAiIdW, @att1IdW, 'Waiting', DATEADD(HOUR, -8, SYSUTCDATETIME()));

IF @att3IdW IS NOT NULL AND @execAiIdW IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[WaitlistEntries] WHERE [EventId] = @execAiIdW AND [AttendeeUserId] = @att3IdW AND [WaitlistStatus] = 'Waiting')
    INSERT INTO [dbo].[WaitlistEntries] ([EventId], [AttendeeUserId], [WaitlistStatus], [QueuedAtUtc])
    VALUES (@execAiIdW, @att3IdW, 'Waiting', DATEADD(HOUR, -3, SYSUTCDATETIME()));
GO

-- ------------------------------------------------------------
-- On-Behalf Registration Requests
-- ------------------------------------------------------------
DECLARE
    @mgrIdR   BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager'),
    @att3IdR  BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee3'),
    @att2IdR  BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee2'),
    @globalR  BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Global Tech Summit 2026'),
    @uxR      BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'UX/UI Design Workshop');

IF @att3IdR IS NOT NULL AND @globalR IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[RegistrationRequests] WHERE [EventId] = @globalR AND [AttendeeUserId] = @att3IdR AND [RequestStatus] = 'Accepted')
    INSERT INTO [dbo].[RegistrationRequests] ([EventId], [AttendeeUserId], [RequestedByUserId], [RequestType], [RequestStatus], [RequestedAtUtc], [RespondedAtUtc], [ResponseComment])
    VALUES (@globalR, @att3IdR, @mgrIdR, 'OnBehalf', 'Accepted', DATEADD(DAY, -2, SYSUTCDATETIME()), DATEADD(DAY, -1, SYSUTCDATETIME()), N'Confirmed VIP on-behalf registration.');

IF @att2IdR IS NOT NULL AND @uxR IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[RegistrationRequests] WHERE [EventId] = @uxR AND [AttendeeUserId] = @att2IdR AND [RequestStatus] = 'Pending')
    INSERT INTO [dbo].[RegistrationRequests] ([EventId], [AttendeeUserId], [RequestedByUserId], [RequestType], [RequestStatus], [RequestedAtUtc])
    VALUES (@uxR, @att2IdR, @mgrIdR, 'OnBehalf', 'Pending', DATEADD(DAY, -1, SYSUTCDATETIME()));
GO

-- Link Accepted Requests
DECLARE
    @globalIdL  BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Global Tech Summit 2026'),
    @att3IdL    BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee3');

DECLARE
    @onBehalfRegId BIGINT = (SELECT TOP 1 [RegistrationId] FROM [dbo].[Registrations] WHERE [EventId] = @globalIdL AND [AttendeeUserId] = @att3IdL AND [RegistrationStatus] = 'Confirmed'),
    @reqIdL     BIGINT = (SELECT TOP 1 [RegistrationRequestId] FROM [dbo].[RegistrationRequests] WHERE [EventId] = @globalIdL AND [AttendeeUserId] = @att3IdL AND [RequestStatus] = 'Accepted');

IF @onBehalfRegId IS NOT NULL AND @reqIdL IS NOT NULL
BEGIN
    UPDATE [dbo].[Registrations] SET [RegistrationRequestId] = @reqIdL WHERE [RegistrationId] = @onBehalfRegId;
    UPDATE [dbo].[RegistrationRequests] SET [LinkedRegistrationId] = @onBehalfRegId WHERE [RegistrationRequestId] = @reqIdL;
END
GO

-- ------------------------------------------------------------
-- Attendance Records
-- ------------------------------------------------------------
DECLARE
    @mgrIdAtt    BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager'),
    @att1IdAtt   BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee1'),
    @att2IdAtt   BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee2'),
    @att3IdAtt   BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee3'),
    @devopsIdAtt BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'DevOps Masterclass Bootcamp'),
    @globalIdAtt BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Global Tech Summit 2026');

DECLARE @reg1Dev BIGINT = (SELECT TOP 1 [RegistrationId] FROM [dbo].[Registrations] WHERE [EventId] = @devopsIdAtt AND [AttendeeUserId] = @att1IdAtt AND [RegistrationStatus] = 'Confirmed');
DECLARE @reg2Dev BIGINT = (SELECT TOP 1 [RegistrationId] FROM [dbo].[Registrations] WHERE [EventId] = @devopsIdAtt AND [AttendeeUserId] = @att2IdAtt AND [RegistrationStatus] = 'Confirmed');
DECLARE @reg3Dev BIGINT = (SELECT TOP 1 [RegistrationId] FROM [dbo].[Registrations] WHERE [EventId] = @devopsIdAtt AND [AttendeeUserId] = @att3IdAtt AND [RegistrationStatus] = 'Confirmed');
DECLARE @reg1Global BIGINT = (SELECT TOP 1 [RegistrationId] FROM [dbo].[Registrations] WHERE [EventId] = @globalIdAtt AND [AttendeeUserId] = @att1IdAtt AND [RegistrationStatus] = 'Confirmed');

IF @reg1Dev IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AttendanceRecords] WHERE [RegistrationId] = @reg1Dev)
    INSERT INTO [dbo].[AttendanceRecords] ([RegistrationId], [AttendanceStatus], [RecordedByUserId], [RecordedAtUtc], [IsFinalized], [FinalizedAtUtc], [RevisionNo])
    VALUES (@reg1Dev, 'Attended', @mgrIdAtt, DATEADD(DAY, -8, SYSUTCDATETIME()), 1, DATEADD(DAY, -8, SYSUTCDATETIME()), 1);

IF @reg2Dev IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AttendanceRecords] WHERE [RegistrationId] = @reg2Dev)
    INSERT INTO [dbo].[AttendanceRecords] ([RegistrationId], [AttendanceStatus], [RecordedByUserId], [RecordedAtUtc], [IsFinalized], [FinalizedAtUtc], [RevisionNo])
    VALUES (@reg2Dev, 'Absent', @mgrIdAtt, DATEADD(DAY, -8, SYSUTCDATETIME()), 1, DATEADD(DAY, -8, SYSUTCDATETIME()), 1);

IF @reg3Dev IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AttendanceRecords] WHERE [RegistrationId] = @reg3Dev)
    INSERT INTO [dbo].[AttendanceRecords] ([RegistrationId], [AttendanceStatus], [RecordedByUserId], [RecordedAtUtc], [IsFinalized], [FinalizedAtUtc], [RevisionNo])
    VALUES (@reg3Dev, 'Attended', @mgrIdAtt, DATEADD(DAY, -8, SYSUTCDATETIME()), 1, DATEADD(DAY, -8, SYSUTCDATETIME()), 1);

IF @reg1Global IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AttendanceRecords] WHERE [RegistrationId] = @reg1Global)
    INSERT INTO [dbo].[AttendanceRecords] ([RegistrationId], [AttendanceStatus], [RecordedByUserId], [RecordedAtUtc], [IsFinalized], [RevisionNo])
    VALUES (@reg1Global, 'Attended', @mgrIdAtt, DATEADD(HOUR, -2, SYSUTCDATETIME()), 0, 1);
GO

-- ------------------------------------------------------------
-- Event Status History Timeline
-- ------------------------------------------------------------
DECLARE
    @mgrIdH   BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager'),
    @mgr2IdH  BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager2'),
    @globalH  BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Global Tech Summit 2026'),
    @aiH      BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'AI & Cloud Innovation Summit'),
    @quantumH BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Quantum Computing & Encryption 2026'),
    @execAiH  BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Executive AI Strategy Forum'),
    @devopsH  BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'DevOps Masterclass Bootcamp'),
    @uxH      BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'UX/UI Design Workshop'),
    @agileH   BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Agile Leadership Roundtable'),
    @cyberH   BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Cybersecurity Best Practices 2026');

IF @globalH IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventStatusHistory] WHERE [EventId] = @globalH AND [ToStatus] = 'Published')
    INSERT INTO [dbo].[EventStatusHistory] ([EventId], [FromStatus], [ToStatus], [ChangedByUserId], [ChangedAtUtc], [Remarks]) VALUES (@globalH, 'Draft', 'Published', @mgrIdH, DATEADD(DAY, -28, SYSUTCDATETIME()), N'Opened for registration.');

IF @aiH IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventStatusHistory] WHERE [EventId] = @aiH AND [ToStatus] = 'Published')
    INSERT INTO [dbo].[EventStatusHistory] ([EventId], [FromStatus], [ToStatus], [ChangedByUserId], [ChangedAtUtc], [Remarks]) VALUES (@aiH, 'Draft', 'Published', @mgrIdH, DATEADD(DAY, -9, SYSUTCDATETIME()), N'Published.');

IF @quantumH IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventStatusHistory] WHERE [EventId] = @quantumH AND [ToStatus] = 'Published')
    INSERT INTO [dbo].[EventStatusHistory] ([EventId], [FromStatus], [ToStatus], [ChangedByUserId], [ChangedAtUtc], [Remarks]) VALUES (@quantumH, 'Draft', 'Published', @mgrIdH, DATEADD(DAY, -7, SYSUTCDATETIME()), N'Quantum Computing event published.');

IF @execAiH IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventStatusHistory] WHERE [EventId] = @execAiH AND [ToStatus] = 'Published')
    INSERT INTO [dbo].[EventStatusHistory] ([EventId], [FromStatus], [ToStatus], [ChangedByUserId], [ChangedAtUtc], [Remarks]) VALUES (@execAiH, 'Draft', 'Published', ISNULL(@mgr2IdH, @mgrIdH), DATEADD(DAY, -6, SYSUTCDATETIME()), N'Executive AI Strategy Forum published.');

IF @devopsH IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventStatusHistory] WHERE [EventId] = @devopsH AND [ToStatus] = 'Closed')
    INSERT INTO [dbo].[EventStatusHistory] ([EventId], [FromStatus], [ToStatus], [ChangedByUserId], [ChangedAtUtc], [Remarks]) VALUES (@devopsH, 'Published', 'Closed', @mgrIdH, DATEADD(DAY, -8, SYSUTCDATETIME()), N'Event completed and closed.');

IF @agileH IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventStatusHistory] WHERE [EventId] = @agileH AND [ToStatus] = 'Cancelled')
    INSERT INTO [dbo].[EventStatusHistory] ([EventId], [FromStatus], [ToStatus], [ChangedByUserId], [ChangedAtUtc], [Remarks]) VALUES (@agileH, 'Published', 'Cancelled', @mgrIdH, DATEADD(DAY, -3, SYSUTCDATETIME()), N'Event cancelled by organizer.');

IF @cyberH IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventStatusHistory] WHERE [EventId] = @cyberH AND [ToStatus] = 'Published')
    INSERT INTO [dbo].[EventStatusHistory] ([EventId], [FromStatus], [ToStatus], [ChangedByUserId], [ChangedAtUtc], [Remarks]) VALUES (@cyberH, 'Draft', 'Published', ISNULL(@mgr2IdH, @mgrIdH), DATEADD(DAY, -1, SYSUTCDATETIME()), N'Cybersecurity Summit published.');
GO

-- ------------------------------------------------------------
-- Notifications (Descriptive human-readable text)
-- ------------------------------------------------------------
DECLARE
    @mgrN      BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager'),
    @bizN      BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'business'),
    @att1N     BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee1'),
    @att2N     BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee2'),
    @att3N     BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee3'),
    @att4N     BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee4'),
    @globalN   BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Global Tech Summit 2026'),
    @aiN       BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'AI & Cloud Innovation Summit'),
    @quantumN  BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Quantum Computing & Encryption 2026'),
    @execAiN   BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Executive AI Strategy Forum');

-- Waitlist Notifications
IF @quantumN IS NOT NULL AND @att2N IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Notifications] WHERE [RecipientUserId] = @att2N AND [NotificationType] = 'WaitlistAdded' AND [RelatedEventId] = @quantumN)
    INSERT INTO [dbo].[Notifications] ([RecipientUserId], [NotificationType], [Title], [Message], [RelatedEventId], [DeliveryStatus], [SentAtUtc])
    VALUES (@att2N, 'WaitlistAdded', N'Added to Waitlist', N'Quantum Computing & Encryption 2026 (#' + CAST(@quantumN AS NVARCHAR(20)) + N') is at full capacity (1/1). You are at Waitlist Position #1.', @quantumN, 'Sent', DATEADD(HOUR, -10, SYSUTCDATETIME()));

IF @quantumN IS NOT NULL AND @att3N IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Notifications] WHERE [RecipientUserId] = @att3N AND [NotificationType] = 'WaitlistAdded' AND [RelatedEventId] = @quantumN)
    INSERT INTO [dbo].[Notifications] ([RecipientUserId], [NotificationType], [Title], [Message], [RelatedEventId], [DeliveryStatus], [SentAtUtc])
    VALUES (@att3N, 'WaitlistAdded', N'Added to Waitlist', N'Quantum Computing & Encryption 2026 (#' + CAST(@quantumN AS NVARCHAR(20)) + N') is at full capacity (1/1). You are at Waitlist Position #2.', @quantumN, 'Sent', DATEADD(HOUR, -6, SYSUTCDATETIME()));

IF @execAiN IS NOT NULL AND @att1N IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Notifications] WHERE [RecipientUserId] = @att1N AND [NotificationType] = 'WaitlistAdded' AND [RelatedEventId] = @execAiN)
    INSERT INTO [dbo].[Notifications] ([RecipientUserId], [NotificationType], [Title], [Message], [RelatedEventId], [DeliveryStatus], [SentAtUtc])
    VALUES (@att1N, 'WaitlistAdded', N'Added to Waitlist', N'Executive AI Strategy Forum (#' + CAST(@execAiN AS NVARCHAR(20)) + N') is at full capacity (1/1). You are at Waitlist Position #1.', @execAiN, 'Sent', DATEADD(HOUR, -8, SYSUTCDATETIME()));

-- Business Management Notification
IF @bizN IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Notifications] WHERE [RecipientUserId] = @bizN AND [NotificationType] = 'BusinessReportGenerated')
    INSERT INTO [dbo].[Notifications] ([RecipientUserId], [NotificationType], [Title], [Message], [DeliveryStatus], [SentAtUtc])
    VALUES (@bizN, 'BusinessReportGenerated', N'Weekly Analytics Summary', N'System-wide metrics updated: 10 total events (6 published, 1 closed, 1 draft, 1 cancelled) with active attendee engagement.', 'Sent', DATEADD(HOUR, -1, SYSUTCDATETIME()));
GO

-- ------------------------------------------------------------
-- Audit Trail Log Records
-- ------------------------------------------------------------
DECLARE
    @adminA  BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'admin'),
    @mgrA    BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager'),
    @bizA    BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'business'),
    @globalA BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Global Tech Summit 2026'),
    @quantumA BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Quantum Computing & Encryption 2026');

IF @bizA IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AuditRecords] WHERE [ActorUserId] = @bizA AND [ActionType] = 'DashboardViewed')
    INSERT INTO [dbo].[AuditRecords] ([ActorUserId], [ActionType], [TargetEntity], [Outcome], [IpAddress], [CreatedAtUtc])
    VALUES (@bizA, 'DashboardViewed', 'BusinessMetrics', 'Success', N'127.0.0.1', DATEADD(HOUR, -2, SYSUTCDATETIME()));

IF @quantumA IS NOT NULL AND @mgrA IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AuditRecords] WHERE [ActorUserId] = @mgrA AND [ActionType] = 'EventCreated' AND [EventId] = @quantumA)
    INSERT INTO [dbo].[AuditRecords] ([ActorUserId], [ActionType], [TargetEntity], [TargetEntityId], [EventId], [Outcome], [IpAddress], [CreatedAtUtc])
    VALUES (@mgrA, 'EventCreated', 'Event', @quantumA, @quantumA, 'Success', N'127.0.0.1', DATEADD(DAY, -7, SYSUTCDATETIME()));

PRINT 'EventManagementDB schema and enhanced logical seed data created successfully.';

';
