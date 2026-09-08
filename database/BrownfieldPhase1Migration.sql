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

-- ------------------------------------------------------------
-- Brownfield Phase 1 Seed Data: Categories, Venues, Series, Approvals, Alerts, Feedbacks, Interests
-- ------------------------------------------------------------
DECLARE
    @adminB      BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'admin'),
    @mgrB        BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager'),
    @mgr2B       BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'manager2'),
    @att1B       BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee1'),
    @att2B       BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee2'),
    @att3B       BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee3'),
    @att4B       BIGINT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [UserName] = N'attendee4'),
    @globalEvB   BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Global Tech Summit 2026'),
    @aiEvB       BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'AI & Cloud Innovation Summit'),
    @quantumEvB  BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Quantum Computing & Encryption 2026'),
    @devopsEvB   BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'DevOps Masterclass Bootcamp'),
    @cyberEvB    BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'Cybersecurity Best Practices 2026'),
    @uxEvB       BIGINT = (SELECT TOP 1 [EventId] FROM [dbo].[Events] WHERE [Title] = N'UX/UI Design Workshop');

-- 1. Seed Categories
IF NOT EXISTS (SELECT 1 FROM [dbo].[EventCategories] WHERE [CategoryName] = N'Artificial Intelligence & ML')
    INSERT INTO [dbo].[EventCategories] ([CategoryName], [Description], [IsActive], [CreatedByUserId])
    VALUES (N'Artificial Intelligence & ML', N'Machine learning, neural networks, LLMs, and generative intelligence applications.', 1, @adminB);

IF NOT EXISTS (SELECT 1 FROM [dbo].[EventCategories] WHERE [CategoryName] = N'Cloud & DevOps')
    INSERT INTO [dbo].[EventCategories] ([CategoryName], [Description], [IsActive], [CreatedByUserId])
    VALUES (N'Cloud & DevOps', N'Container orchestration, Kubernetes, serverless architectures, and CI/CD automation.', 1, @adminB);

IF NOT EXISTS (SELECT 1 FROM [dbo].[EventCategories] WHERE [CategoryName] = N'Cybersecurity & Privacy')
    INSERT INTO [dbo].[EventCategories] ([CategoryName], [Description], [IsActive], [CreatedByUserId])
    VALUES (N'Cybersecurity & Privacy', N'Zero-trust security models, cryptography, ethical hacking, and data defense protocols.', 1, @adminB);

IF NOT EXISTS (SELECT 1 FROM [dbo].[EventCategories] WHERE [CategoryName] = N'Product & Design')
    INSERT INTO [dbo].[EventCategories] ([CategoryName], [Description], [IsActive], [CreatedByUserId])
    VALUES (N'Product & Design', N'UI/UX design thinking, design systems, usability research, and product strategy.', 1, @adminB);

IF NOT EXISTS (SELECT 1 FROM [dbo].[EventCategories] WHERE [CategoryName] = N'Quantum & Deep Tech')
    INSERT INTO [dbo].[EventCategories] ([CategoryName], [Description], [IsActive], [CreatedByUserId])
    VALUES (N'Quantum & Deep Tech', N'Quantum computing algorithms, encryption hardware, and emerging breakthrough tech.', 1, @adminB);

-- 2. Seed Venues
IF NOT EXISTS (SELECT 1 FROM [dbo].[Venues] WHERE [Name] = N'Convention Hall A - Silicon Arena')
    INSERT INTO [dbo].[Venues] ([Name], [Address], [Capacity], [ContactDetails], [IsActive])
    VALUES (N'Convention Hall A - Silicon Arena', N'Building 4, Silicon Boulevard, Tech District', 150, N'facilities@techsummit.com', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Venues] WHERE [Name] = N'Innovation Wing - CISCO Hall')
    INSERT INTO [dbo].[Venues] ([Name], [Address], [Capacity], [ContactDetails], [IsActive])
    VALUES (N'Innovation Wing - CISCO Hall', N'Tower B, 2nd Floor, Innovation Campus', 50, N'campus-events@cisco-hall.com', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Venues] WHERE [Name] = N'Quantum Research Lab 101')
    INSERT INTO [dbo].[Venues] ([Name], [Address], [Capacity], [ContactDetails], [IsActive])
    VALUES (N'Quantum Research Lab 101', N'Annex 7, Advanced Science Center', 10, N'lab-coordinator@quantum.edu', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Venues] WHERE [Name] = N'Global Virtual Stage (Teams / Zoom)')
    INSERT INTO [dbo].[Venues] ([Name], [Address], [Capacity], [ContactDetails], [IsActive])
    VALUES (N'Global Virtual Stage (Teams / Zoom)', N'Cloud Broadcast Streaming Endpoint', 1000, N'webinars@eventhub.live', 1);

-- 3. Link Events to Categories
DECLARE
    @catAi INT = (SELECT TOP 1 [CategoryId] FROM [dbo].[EventCategories] WHERE [CategoryName] = N'Artificial Intelligence & ML'),
    @catCloud INT = (SELECT TOP 1 [CategoryId] FROM [dbo].[EventCategories] WHERE [CategoryName] = N'Cloud & DevOps'),
    @catCyber INT = (SELECT TOP 1 [CategoryId] FROM [dbo].[EventCategories] WHERE [CategoryName] = N'Cybersecurity & Privacy'),
    @catDesign INT = (SELECT TOP 1 [CategoryId] FROM [dbo].[EventCategories] WHERE [CategoryName] = N'Product & Design'),
    @catQuantum INT = (SELECT TOP 1 [CategoryId] FROM [dbo].[EventCategories] WHERE [CategoryName] = N'Quantum & Deep Tech'),
    @venueConv BIGINT = (SELECT TOP 1 [VenueId] FROM [dbo].[Venues] WHERE [Name] = N'Convention Hall A - Silicon Arena'),
    @venueCisco BIGINT = (SELECT TOP 1 [VenueId] FROM [dbo].[Venues] WHERE [Name] = N'Innovation Wing - CISCO Hall'),
    @venueLab BIGINT = (SELECT TOP 1 [VenueId] FROM [dbo].[Venues] WHERE [Name] = N'Quantum Research Lab 101');

IF @globalEvB IS NOT NULL AND @catCloud IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventCategoryMappings] WHERE [EventId] = @globalEvB AND [CategoryId] = @catCloud)
    INSERT INTO [dbo].[EventCategoryMappings] ([EventId], [CategoryId]) VALUES (@globalEvB, @catCloud);

IF @globalEvB IS NOT NULL AND @catAi IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventCategoryMappings] WHERE [EventId] = @globalEvB AND [CategoryId] = @catAi)
    INSERT INTO [dbo].[EventCategoryMappings] ([EventId], [CategoryId]) VALUES (@globalEvB, @catAi);

IF @aiEvB IS NOT NULL AND @catAi IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventCategoryMappings] WHERE [EventId] = @aiEvB AND [CategoryId] = @catAi)
    INSERT INTO [dbo].[EventCategoryMappings] ([EventId], [CategoryId]) VALUES (@aiEvB, @catAi);

IF @quantumEvB IS NOT NULL AND @catQuantum IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventCategoryMappings] WHERE [EventId] = @quantumEvB AND [CategoryId] = @catQuantum)
    INSERT INTO [dbo].[EventCategoryMappings] ([EventId], [CategoryId]) VALUES (@quantumEvB, @catQuantum);

IF @devopsEvB IS NOT NULL AND @catCloud IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventCategoryMappings] WHERE [EventId] = @devopsEvB AND [CategoryId] = @catCloud)
    INSERT INTO [dbo].[EventCategoryMappings] ([EventId], [CategoryId]) VALUES (@devopsEvB, @catCloud);

IF @cyberEvB IS NOT NULL AND @catCyber IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventCategoryMappings] WHERE [EventId] = @cyberEvB AND [CategoryId] = @catCyber)
    INSERT INTO [dbo].[EventCategoryMappings] ([EventId], [CategoryId]) VALUES (@cyberEvB, @catCyber);

-- 4. Update Events with Venue & Virtual details
IF @globalEvB IS NOT NULL AND @venueConv IS NOT NULL
    UPDATE [dbo].[Events] SET [VenueId] = @venueConv, [ApprovalStatus] = 'Approved', [IsVirtual] = 1, [VirtualMeetingUrl] = N'https://teams.microsoft.com/l/meetup-join/techsummit2026' WHERE [EventId] = @globalEvB;

IF @aiEvB IS NOT NULL AND @venueCisco IS NOT NULL
    UPDATE [dbo].[Events] SET [VenueId] = @venueCisco, [ApprovalStatus] = 'Approved' WHERE [EventId] = @aiEvB;

IF @quantumEvB IS NOT NULL AND @venueLab IS NOT NULL
    UPDATE [dbo].[Events] SET [VenueId] = @venueLab, [ApprovalStatus] = 'Approved' WHERE [EventId] = @quantumEvB;

-- 5. Seed Approval Workflow (Draft event pending approval)
IF @uxEvB IS NOT NULL AND @mgrB IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventApprovalRequests] WHERE [EventId] = @uxEvB)
BEGIN
    UPDATE [dbo].[Events] SET [ApprovalStatus] = 'PendingApproval' WHERE [EventId] = @uxEvB;
    INSERT INTO [dbo].[EventApprovalRequests] ([EventId], [RequestedByUserId], [Status], [Remarks], [RequestedAtUtc])
    VALUES (@uxEvB, @mgrB, 'Pending', N'Draft event ready for leadership review and scheduling approval.', DATEADD(DAY, -1, SYSUTCDATETIME()));
END

-- 6. Seed Capacity Alerts
IF @globalEvB IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventCapacityAlertConfigs] WHERE [EventId] = @globalEvB AND [ThresholdPercentage] = 75)
    INSERT INTO [dbo].[EventCapacityAlertConfigs] ([EventId], [ThresholdPercentage], [IsTriggered])
    VALUES (@globalEvB, 75, 0);

IF @aiEvB IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventCapacityAlertConfigs] WHERE [EventId] = @aiEvB AND [ThresholdPercentage] = 90)
    INSERT INTO [dbo].[EventCapacityAlertConfigs] ([EventId], [ThresholdPercentage], [IsTriggered], [TriggeredAtUtc])
    VALUES (@aiEvB, 90, 1, DATEADD(DAY, -2, SYSUTCDATETIME()));

-- 7. Seed Feedback & Ratings (Completed event DevOps Masterclass)
IF @devopsEvB IS NOT NULL AND @att1B IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventFeedbacks] WHERE [EventId] = @devopsEvB AND [AttendeeUserId] = @att1B)
    INSERT INTO [dbo].[EventFeedbacks] ([EventId], [AttendeeUserId], [Rating], [Comments], [CreatedAtUtc])
    VALUES (@devopsEvB, @att1B, 5, N'Exceptional deep-dive on Kubernetes CI/CD pipelines! Hands-on labs were flawless.', DATEADD(DAY, -7, SYSUTCDATETIME()));

IF @devopsEvB IS NOT NULL AND @att2B IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[EventFeedbacks] WHERE [EventId] = @devopsEvB AND [AttendeeUserId] = @att2B)
    INSERT INTO [dbo].[EventFeedbacks] ([EventId], [AttendeeUserId], [Rating], [Comments], [CreatedAtUtc])
    VALUES (@devopsEvB, @att2B, 4, N'Great speakers and pacing. Would love even more advanced security topics next time.', DATEADD(DAY, -6, SYSUTCDATETIME()));

-- 8. Seed Attendee Category Interests (Personalized Recommendations)
IF @att1B IS NOT NULL AND @catAi IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AttendeeInterests] WHERE [UserId] = @att1B AND [CategoryId] = @catAi)
    INSERT INTO [dbo].[AttendeeInterests] ([UserId], [CategoryId], [Weight]) VALUES (@att1B, @catAi, 1.0);

IF @att1B IS NOT NULL AND @catCloud IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AttendeeInterests] WHERE [UserId] = @att1B AND [CategoryId] = @catCloud)
    INSERT INTO [dbo].[AttendeeInterests] ([UserId], [CategoryId], [Weight]) VALUES (@att1B, @catCloud, 0.8);

IF @att2B IS NOT NULL AND @catCyber IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AttendeeInterests] WHERE [UserId] = @att2B AND [CategoryId] = @catCyber)
    INSERT INTO [dbo].[AttendeeInterests] ([UserId], [CategoryId], [Weight]) VALUES (@att2B, @catCyber, 1.0);

IF @att3B IS NOT NULL AND @catDesign IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[AttendeeInterests] WHERE [UserId] = @att3B AND [CategoryId] = @catDesign)
    INSERT INTO [dbo].[AttendeeInterests] ([UserId], [CategoryId], [Weight]) VALUES (@att3B, @catDesign, 1.0);

PRINT 'Brownfield Phase 1 tables and enhanced seed data applied successfully.';
GO
