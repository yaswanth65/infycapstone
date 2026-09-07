-- ============================================================
-- Event Management System - Phase 1 Brownfield Migration Script
-- Tables: EventCategories, EventCategoryMappings, Venues,
--         EventSeries, EventApprovalRequests, EventCapacityAlertConfigs,
--         EventFeedbacks, AttendeeInterests
-- Alterations: Events table new columns (VenueId, SeriesId, IsVirtual, VirtualMeetingUrl, ApprovalStatus, RejectionReason)
-- ============================================================

USE [EventManagementDB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. EventCategories
IF OBJECT_ID(N'dbo.EventCategories', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EventCategories] (
        [CategoryId]     INT            NOT NULL IDENTITY(1,1),
        [CategoryName]   NVARCHAR(100)  NOT NULL,
        [Description]    NVARCHAR(500)  NULL,
        [IsActive]       BIT            NOT NULL CONSTRAINT [DF_EventCategories_IsActive] DEFAULT (1),
        [CreatedAtUtc]   DATETIME2(0)   NOT NULL CONSTRAINT [DF_EventCategories_CreatedAtUtc] DEFAULT (sysutcdatetime()),
        [CreatedByUserId] BIGINT        NULL,
        CONSTRAINT [PK_EventCategories] PRIMARY KEY ([CategoryId]),
        CONSTRAINT [FK_EventCategories_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX [UQ_EventCategories_CategoryName] ON [dbo].[EventCategories] ([CategoryName]);
END
GO

-- 2. Venues
IF OBJECT_ID(N'dbo.Venues', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Venues] (
        [VenueId]        BIGINT         NOT NULL IDENTITY(1,1),
        [Name]           NVARCHAR(200)  NOT NULL,
        [Address]        NVARCHAR(500)  NULL,
        [Capacity]       INT            NOT NULL,
        [ContactDetails] NVARCHAR(200)  NULL,
        [IsActive]       BIT            NOT NULL CONSTRAINT [DF_Venues_IsActive] DEFAULT (1),
        [CreatedAtUtc]   DATETIME2(0)   NOT NULL CONSTRAINT [DF_Venues_CreatedAtUtc] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_Venues] PRIMARY KEY ([VenueId])
    );
    CREATE UNIQUE INDEX [UQ_Venues_Name] ON [dbo].[Venues] ([Name]);
END
GO

-- 3. EventSeries (Recurring Events)
IF OBJECT_ID(N'dbo.EventSeries', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EventSeries] (
        [SeriesId]              BIGINT         NOT NULL IDENTITY(1,1),
        [OrganizerUserId]       BIGINT         NOT NULL,
        [RecurrencePattern]     NVARCHAR(50)   NOT NULL,
        [RecurrenceInterval]    INT            NOT NULL CONSTRAINT [DF_EventSeries_Interval] DEFAULT (1),
        [DaysOfWeekMask]        INT            NULL,
        [RecurrenceEndDateUtc]  DATETIME2(0)   NOT NULL,
        [CreatedAtUtc]          DATETIME2(0)   NOT NULL CONSTRAINT [DF_EventSeries_CreatedAtUtc] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_EventSeries] PRIMARY KEY ([SeriesId]),
        CONSTRAINT [FK_EventSeries_OrganizerUser] FOREIGN KEY ([OrganizerUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_EventSeries_OrganizerUserId] ON [dbo].[EventSeries] ([OrganizerUserId]);
END
GO

-- Alter Events table with Brownfield columns
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Events') AND name = N'VenueId')
BEGIN
    ALTER TABLE [dbo].[Events] ADD [VenueId] BIGINT NULL CONSTRAINT [FK_Events_Venue] FOREIGN KEY REFERENCES [dbo].[Venues] ([VenueId]) ON DELETE SET NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Events') AND name = N'SeriesId')
BEGIN
    ALTER TABLE [dbo].[Events] ADD [SeriesId] BIGINT NULL CONSTRAINT [FK_Events_Series] FOREIGN KEY REFERENCES [dbo].[EventSeries] ([SeriesId]) ON DELETE SET NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Events') AND name = N'IsVirtual')
BEGIN
    ALTER TABLE [dbo].[Events] ADD [IsVirtual] BIT NOT NULL CONSTRAINT [DF_Events_IsVirtual] DEFAULT (0);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Events') AND name = N'VirtualMeetingUrl')
BEGIN
    ALTER TABLE [dbo].[Events] ADD [VirtualMeetingUrl] NVARCHAR(1000) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Events') AND name = N'ApprovalStatus')
BEGIN
    ALTER TABLE [dbo].[Events] ADD [ApprovalStatus] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Events_ApprovalStatus] DEFAULT ('Approved');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Events') AND name = N'RejectionReason')
BEGIN
    ALTER TABLE [dbo].[Events] ADD [RejectionReason] NVARCHAR(1000) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Events_VenueId_StartEnd' AND object_id = OBJECT_ID(N'dbo.Events'))
BEGIN
    CREATE INDEX [IX_Events_VenueId_StartEnd] ON [dbo].[Events] ([VenueId], [StartAtUtc], [EndAtUtc]);
END
GO

-- 4. EventCategoryMappings
IF OBJECT_ID(N'dbo.EventCategoryMappings', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EventCategoryMappings] (
        [EventId]       BIGINT        NOT NULL,
        [CategoryId]    INT           NOT NULL,
        [AssignedAtUtc] DATETIME2(0)  NOT NULL CONSTRAINT [DF_EventCategoryMappings_AssignedAtUtc] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_EventCategoryMappings] PRIMARY KEY ([EventId], [CategoryId]),
        CONSTRAINT [FK_EventCategoryMappings_Event] FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events] ([EventId]) ON DELETE CASCADE,
        CONSTRAINT [FK_EventCategoryMappings_Category] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[EventCategories] ([CategoryId]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_EventCategoryMappings_CategoryId] ON [dbo].[EventCategoryMappings] ([CategoryId]);
END
GO

-- 5. EventApprovalRequests
IF OBJECT_ID(N'dbo.EventApprovalRequests', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EventApprovalRequests] (
        [ApprovalRequestId] BIGINT        NOT NULL IDENTITY(1,1),
        [EventId]           BIGINT        NOT NULL,
        [RequestedByUserId] BIGINT        NOT NULL,
        [ReviewedByUserId]  BIGINT        NULL,
        [Status]            NVARCHAR(20)  NOT NULL CONSTRAINT [DF_EventApprovalRequests_Status] DEFAULT ('Pending'),
        [Remarks]           NVARCHAR(1000) NULL,
        [RequestedAtUtc]    DATETIME2(0)  NOT NULL CONSTRAINT [DF_EventApprovalRequests_RequestedAtUtc] DEFAULT (sysutcdatetime()),
        [ReviewedAtUtc]     DATETIME2(0)  NULL,
        CONSTRAINT [PK_EventApprovalRequests] PRIMARY KEY ([ApprovalRequestId]),
        CONSTRAINT [FK_EventApprovalRequests_Event] FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events] ([EventId]) ON DELETE CASCADE,
        CONSTRAINT [FK_EventApprovalRequests_RequestedByUser] FOREIGN KEY ([RequestedByUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventApprovalRequests_ReviewedByUser] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_EventApprovalRequests_EventId] ON [dbo].[EventApprovalRequests] ([EventId]);
    CREATE INDEX [IX_EventApprovalRequests_Status] ON [dbo].[EventApprovalRequests] ([Status]);
END
GO

-- 6. EventCapacityAlertConfigs
IF OBJECT_ID(N'dbo.EventCapacityAlertConfigs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EventCapacityAlertConfigs] (
        [AlertConfigId]        BIGINT        NOT NULL IDENTITY(1,1),
        [EventId]              BIGINT        NOT NULL,
        [ThresholdPercentage]  INT           NOT NULL,
        [IsTriggered]          BIT           NOT NULL CONSTRAINT [DF_CapacityAlerts_IsTriggered] DEFAULT (0),
        [TriggeredAtUtc]       DATETIME2(0)  NULL,
        [CreatedAtUtc]         DATETIME2(0)  NOT NULL CONSTRAINT [DF_CapacityAlerts_CreatedAtUtc] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_EventCapacityAlertConfigs] PRIMARY KEY ([AlertConfigId]),
        CONSTRAINT [FK_EventCapacityAlertConfigs_Event] FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events] ([EventId]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [UQ_EventCapacityAlertConfigs_Event_Threshold] ON [dbo].[EventCapacityAlertConfigs] ([EventId], [ThresholdPercentage]);
END
GO

-- 7. EventFeedbacks
IF OBJECT_ID(N'dbo.EventFeedbacks', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EventFeedbacks] (
        [FeedbackId]     BIGINT         NOT NULL IDENTITY(1,1),
        [EventId]        BIGINT         NOT NULL,
        [AttendeeUserId] BIGINT         NOT NULL,
        [Rating]         INT            NOT NULL CHECK ([Rating] BETWEEN 1 AND 5),
        [Comments]       NVARCHAR(1000) NULL,
        [CreatedAtUtc]   DATETIME2(0)   NOT NULL CONSTRAINT [DF_EventFeedbacks_CreatedAtUtc] DEFAULT (sysutcdatetime()),
        [IsFlagged]      BIT            NOT NULL CONSTRAINT [DF_EventFeedbacks_IsFlagged] DEFAULT (0),
        CONSTRAINT [PK_EventFeedbacks] PRIMARY KEY ([FeedbackId]),
        CONSTRAINT [FK_EventFeedbacks_Event] FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events] ([EventId]) ON DELETE CASCADE,
        CONSTRAINT [FK_EventFeedbacks_AttendeeUser] FOREIGN KEY ([AttendeeUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [UQ_EventFeedbacks_Event_Attendee] ON [dbo].[EventFeedbacks] ([EventId], [AttendeeUserId]);
    CREATE INDEX [IX_EventFeedbacks_EventId] ON [dbo].[EventFeedbacks] ([EventId]);
END
GO

-- 8. AttendeeInterests
IF OBJECT_ID(N'dbo.AttendeeInterests', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AttendeeInterests] (
        [InterestId]     BIGINT         NOT NULL IDENTITY(1,1),
        [UserId]         BIGINT         NOT NULL,
        [CategoryId]     INT            NOT NULL,
        [Weight]         DECIMAL(3,2)   NOT NULL CONSTRAINT [DF_AttendeeInterests_Weight] DEFAULT (1.0),
        [UpdatedAtUtc]   DATETIME2(0)   NOT NULL CONSTRAINT [DF_AttendeeInterests_UpdatedAtUtc] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_AttendeeInterests] PRIMARY KEY ([InterestId]),
        CONSTRAINT [FK_AttendeeInterests_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_AttendeeInterests_Category] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[EventCategories] ([CategoryId]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [UQ_AttendeeInterests_User_Category] ON [dbo].[AttendeeInterests] ([UserId], [CategoryId]);
END
GO
