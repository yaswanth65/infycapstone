# Event Management System — Complete API Documentation & Frontend Developer Guide

**Base URL:** `http://localhost:5195`  
**API Specification Version:** `1.0.0` (100% Verified with C# DAL Repositories & Service Layer)

---

## Response Envelopes & Response Standard

In this backend, API responses fall into **two precise response structures**. The frontend must inspect the endpoint type to determine whether to unwrap an `ApiResponse<T>` envelope or consume a direct DTO.

### 1. Standard Generic Envelope (`ApiResponse<T>`)
Used by `AuthController`, `RoleController`, `EventManagerController`, `AdministratorController`, `AdminReportsController`, `AuditMonitoringController`, and `NotificationController`.

```typescript
interface ApiResponse<T> {
  success: boolean;       // e.g. true / false
  statusCode: number;     // e.g. 200, 201, 400, 404, 500
  message: string;        // Human-readable status message
  data: T | null;         // Response payload of type T
  errors: Record<string, string[]> | null; // Validation field errors dictionary
}
```

### 2. Standard Paginated Data Envelope (`PaginatedResponse<T>`)
Used inside `ApiResponse<PaginatedResponse<T>>` for paginated list endpoints (Users, Organizer Events, Registration Requests, Reports, Audit Logs, Notifications).

```typescript
interface PaginatedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
```

### 3. Direct DTO Responses (No Generic Envelope)
* **Public Events Controller (`/api/v1/public/events`)**: Returns `PublicEventDto` or `PublicEventDto[]` directly.
* **Attendee Registration Controller (`/api/v1/attendee/registrations`)**: Returns `RegistrationResponseDto`, `CancellationResponseDto`, or `MyEventsDto` directly with standard HTTP Status Codes (`201`, `202`, `200`, `409`, `400`, `404`).

---

## Table of Contents
1. [Authentication (`/api/v1/auth`)](#1-authentication)
2. [Roles (`/api/v1/roles`)](#2-roles)
3. [Public Events (`/api/v1/public/events`)](#3-public-events)
4. [Attendee Portal (`/api/v1/attendee`)](#4-attendee-portal)
5. [Event Manager Operations (`/api/v1/event-manager`)](#5-event-manager-operations)
6. [Administrator Operations (`/api/v1/admin`)](#6-administrator-operations)
7. [Notifications System (`/api/v1/notifications`)](#7-notifications-system)
8. [Business Management Portal (`/api/v1/business`)](#8-business-management-portal)

---

## 1. Authentication

### 1.1 Login User
Authenticate user credentials and receive JWT Bearer token and user details.

* **HTTP Method:** `POST`
* **Route:** `/api/v1/auth/login`
* **Authorization:** Anonymous
* **Request Headers:** `Content-Type: application/json`
* **Request Body (`LoginRequestDto`):**
  ```json
  {
    "emailOrUserName": "admin",
    "password": "Password@123"
  }
  ```
* **Response Payload (`200 OK` - Wrapped in `ApiResponse<LoginResponseDto>`):**
  ```json
  {
    "success": true,
    "statusCode": 200,
    "message": "Login successful.",
    "data": {
      "success": true,
      "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "expiresAtUtc": "2026-09-05T18:00:00Z",
      "userId": 1,
      "userName": "admin",
      "displayName": "System Administrator",
      "email": "admin@eventmanagement.com",
      "roleName": "Administrator",
      "message": "Login successful."
    },
    "errors": null
  }
  ```
* **Underlying Query / Logic:**
  ```sql
  SELECT TOP 1 u.UserId, u.UserName, u.DisplayName, u.Email, u.PasswordHash, u.RoleId, r.RoleName 
  FROM Users u 
  INNER JOIN Roles r ON u.RoleId = r.RoleId 
  WHERE u.IsActive = 1 AND u.DeactivatedAtUtc IS NULL 
    AND (u.Email = @EmailOrUserName OR u.UserName = @EmailOrUserName);
  ```
* **Curl Command:**
  ```bash
  curl.exe -s -X POST http://localhost:5195/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"emailOrUserName\":\"admin\",\"password\":\"Password@123\"}"
  ```

---

## 2. Roles

### 2.1 Get Active System Roles
Fetch list of active roles for selection dropdowns.

* **HTTP Method:** `GET`
* **Route:** `/api/v1/roles`
* **Authorization:** Authenticated (Bearer Token required)
* **Response Payload (`200 OK` - Wrapped in `ApiResponse<List<RoleResponseDto>>`):**
  ```json
  {
    "success": true,
    "statusCode": 200,
    "message": "Roles retrieved.",
    "data": [
      { "roleId": 1, "roleName": "Administrator", "isActive": true, "createdAtUtc": "2026-09-05T00:00:00Z" },
      { "roleId": 2, "roleName": "EventManager", "isActive": true, "createdAtUtc": "2026-09-05T00:00:00Z" },
      { "roleId": 3, "roleName": "BusinessManagement", "isActive": true, "createdAtUtc": "2026-09-05T00:00:00Z" },
      { "roleId": 4, "roleName": "Attendee", "isActive": true, "createdAtUtc": "2026-09-05T00:00:00Z" }
    ],
    "errors": null
  }
  ```
* **Underlying Query / Logic:**
  ```sql
  SELECT RoleId, RoleName, IsActive, CreatedAtUtc FROM Roles WHERE IsActive = 1 ORDER BY RoleId;
  ```
* **Curl Command:**
  ```bash
  curl.exe -s -X GET http://localhost:5195/api/v1/roles \
    -H "Authorization: Bearer <TOKEN>"
  ```

---

## 3. Public Events

### 3.1 Search & Filter Published Events
Returns published events matching keyword, date ranges, location, or available capacity.

* **HTTP Method:** `GET`
* **Route:** `/api/v1/public/events`
* **Authorization:** Anonymous
* **Query Parameters (`PublicEventQueryDto`):**
  * `keyword` (string, optional): Search title and description
  * `fromUtc` (datetime, optional): Start date filter
  * `toUtc` (datetime, optional): End date filter
  * `location` (string, optional): Search venue/location
  * `onlyAvailable` (boolean, optional): Only events where `AvailableCapacity > 0`
* **Response Payload (`200 OK` - Direct List `PublicEventDto[]`):**
  ```json
  [
    {
      "eventId": 1,
      "title": "Tech Conference 2026",
      "description": "Annual Tech Meetup",
      "venue": "Hall A",
      "startAtUtc": "2026-10-01T09:00:00Z",
      "endAtUtc": "2026-10-01T17:00:00Z",
      "registrationOpenAtUtc": "2026-09-01T00:00:00Z",
      "registrationCloseAtUtc": "2026-09-30T23:59:59Z",
      "capacity": 50,
      "confirmedCount": 10,
      "availableCapacity": 40,
      "waitlistCount": 0,
      "waitlistAvailable": false,
      "status": "Published",
      "waitlistAttendees": [],
      "capacityMessage": "Seats available (40 remaining)."
    }
  ]
  ```
* **Underlying Query / Logic:**
  ```sql
  SELECT e.*, 
    (SELECT COUNT(*) FROM Registrations r WHERE r.EventId = e.EventId AND r.RegistrationStatus = 'Confirmed') AS ConfirmedCount,
    (SELECT COUNT(*) FROM WaitlistEntries w WHERE w.EventId = e.EventId AND w.WaitlistStatus = 'Waiting') AS WaitlistCount
  FROM Events e 
  WHERE e.Status = 'Published'
  ORDER BY e.StartAtUtc ASC;
  ```
* **Curl Command:**
  ```bash
  curl.exe -s -X GET "http://localhost:5195/api/v1/public/events?keyword=Tech"
  ```

### 3.2 Get Public Event Details By ID
Get full details of a published event, including real-time waitlist queue and dynamic capacity message.

* **HTTP Method:** `GET`
* **Route:** `/api/v1/public/events/{eventId}`
* **Authorization:** Anonymous
* **Route Parameters:** `eventId` (long, required)
* **Response Payload (`200 OK` - Direct Object `PublicEventDto`):**
  ```json
  {
    "eventId": 1,
    "title": "Tech Conference 2026",
    "description": "Annual Tech Meetup",
    "venue": "Hall A",
    "startAtUtc": "2026-10-01T09:00:00Z",
    "endAtUtc": "2026-10-01T17:00:00Z",
    "registrationOpenAtUtc": "2026-09-01T00:00:00Z",
    "registrationCloseAtUtc": "2026-09-30T23:59:59Z",
    "capacity": 1,
    "confirmedCount": 1,
    "availableCapacity": 0,
    "waitlistCount": 1,
    "waitlistAvailable": true,
    "status": "Published",
    "waitlistAttendees": [
      {
        "attendeeUserId": 3,
        "displayName": "Attendee Test",
        "position": 1,
        "requestedAtUtc": "2026-09-05T08:00:00Z"
      }
    ],
    "capacityMessage": "Capacity is full. You will be added to the waitlist upon registration."
  }
  ```
* **Underlying Query / Logic:**
  ```sql
  SELECT e.*, w.AttendeeUserId, u.DisplayName, w.QueuedAtUtc
  FROM Events e
  LEFT JOIN WaitlistEntries w ON e.EventId = w.EventId AND w.WaitlistStatus = 'Waiting'
  LEFT JOIN Users u ON w.AttendeeUserId = u.UserId
  WHERE e.EventId = @EventId AND e.Status = 'Published'
  ORDER BY w.QueuedAtUtc ASC;
  ```
* **Curl Command:**
  ```bash
  curl.exe -s -X GET http://localhost:5195/api/v1/public/events/1
  ```

---

## 4. Attendee Portal

### 4.1 Register for Event (Self Registration)
Register for an active published event. If capacity is full, attendee is automatically queued on the waitlist.

* **HTTP Method:** `POST`
* **Route:** `/api/v1/attendee/registrations`
* **Authorization:** Role = `Attendee`
* **Request Headers:** `Authorization: Bearer <TOKEN>`, `Content-Type: application/json`
* **Request Body (`RegisterEventDto`):**
  ```json
  {
    "eventId": 1
  }
  ```
* **Response Payloads (`RegistrationResponseDto`):**
  * **Confirmed (`201 Created`):**
    ```json
    {
      "outcome": "Confirmed",
      "registrationId": 15,
      "waitlistEntryId": null,
      "waitlistPosition": null,
      "message": null
    }
    ```
  * **Waitlisted (`202 Accepted`):**
    ```json
    {
      "outcome": "Waitlisted",
      "registrationId": null,
      "waitlistEntryId": 4,
      "waitlistPosition": 1,
      "message": null
    }
    ```
  * **Duplicate Conflict (`409 Conflict`):**
    ```json
    {
      "outcome": "DuplicateRegistration",
      "registrationId": null,
      "waitlistEntryId": null,
      "waitlistPosition": null,
      "message": "Attendee is already registered for this event."
    }
    ```
* **Underlying Transaction Logic:**
  - Begins a `SERIALIZABLE` isolation transaction locking the event record (`WITH (UPDLOCK, HOLDLOCK)`).
  - Checks if user is already registered or waitlisted.
  - If `ConfirmedCount < Capacity`, inserts record into `Registrations` (`RegistrationStatus = 'Confirmed'`).
  - Else, calculates current waitlist position and inserts into `WaitlistEntries` (`WaitlistStatus = 'Waiting'`).
* **Curl Command:**
  ```bash
  curl.exe -s -X POST http://localhost:5195/api/v1/attendee/registrations \
    -H "Authorization: Bearer <ATTENDEE_TOKEN>" \
    -H "Content-Type: application/json" \
    -d "{\"eventId\":1}"
  ```

### 4.2 Cancel Registration (Triggers Auto-Promotion)
Cancel an existing event registration. If a waitlist exists for the event, the oldest waitlisted attendee is automatically promoted to `Confirmed` and notified.

* **HTTP Method:** `DELETE`
* **Route:** `/api/v1/attendee/registrations/{registrationId}`
* **Authorization:** Role = `Attendee`
* **Route Parameters:** `registrationId` (long, required)
* **Response Payload (`200 OK` - Direct `CancellationResponseDto`):**
  ```json
  {
    "cancelled": true,
    "reason": null,
    "promotedRegistrationId": 16,
    "promotedAttendeeUserId": 4
  }
  ```
* **Underlying Transaction Logic:**
  - Updates `Registrations` setting `RegistrationStatus = 'Cancelled'` and `CancelledAtUtc = GETUTCDATE()`.
  - Queries: `SELECT TOP 1 * FROM WaitlistEntries WHERE EventId = @EventId AND WaitlistStatus = 'Waiting' ORDER BY QueuedAtUtc ASC`.
  - If entry exists: Updates waitlist entry to `'Promoted'`, creates new confirmed `Registration`, and inserts a `Notification` for the promoted attendee.
* **Curl Command:**
  ```bash
  curl.exe -s -X DELETE http://localhost:5195/api/v1/attendee/registrations/15 \
    -H "Authorization: Bearer <ATTENDEE_TOKEN>"
  ```

### 4.3 Get Attendee "My Events" History
Retrieve all active and past registrations & waitlist entries for the logged-in attendee.

* **HTTP Method:** `GET`
* **Route:** `/api/v1/attendee/my-events`
* **Authorization:** Role = `Attendee`
* **Response Payload (`200 OK` - Direct `MyEventsDto`):**
  ```json
  {
    "active": [
      {
        "eventId": 1,
        "title": "Tech Conference 2026",
        "venue": "Hall A",
        "startAtUtc": "2026-10-01T09:00:00Z",
        "endAtUtc": "2026-10-01T17:00:00Z",
        "registrationId": 15,
        "waitlistEntryId": null,
        "itemType": "Registration",
        "status": "Confirmed",
        "waitlistPosition": null,
        "attendanceStatus": "Attended",
        "isPast": false
      }
    ],
    "past": []
  }
  ```
* **Curl Command:**
  ```bash
  curl.exe -s -X GET http://localhost:5195/api/v1/attendee/my-events \
    -H "Authorization: Bearer <ATTENDEE_TOKEN>"
  ```

---

## 5. Event Manager Operations

### 5.1 Create Event
* **HTTP Method:** `POST`
* **Route:** `/api/v1/event-manager/events`
* **Authorization:** Policy = `EventManager` (`EventManager` or `Administrator`)
* **Request Body (`EventCreateDto`):**
  ```json
  {
    "title": "Tech Summit 2026",
    "description": "Annual Technical Conference",
    "venue": "Convention Hall A",
    "startAtUtc": "2026-10-01T09:00:00Z",
    "endAtUtc": "2026-10-01T17:00:00Z",
    "registrationOpenAtUtc": "2026-09-01T00:00:00Z",
    "registrationCloseAtUtc": "2026-09-30T23:59:59Z",
    "capacity": 100
  }
  ```
* **Response Payload (`201 Created` - Wrapped in `ApiResponse<EventResponseDto>`):**
  ```json
  {
    "success": true,
    "statusCode": 201,
    "message": "Event created.",
    "data": {
      "eventId": 5,
      "title": "Tech Summit 2026",
      "description": "Annual Technical Conference",
      "venue": "Convention Hall A",
      "startAtUtc": "2026-10-01T09:00:00Z",
      "endAtUtc": "2026-10-01T17:00:00Z",
      "registrationOpenAtUtc": "2026-09-01T00:00:00Z",
      "registrationCloseAtUtc": "2026-09-30T23:59:59Z",
      "capacity": 100,
      "status": "Draft",
      "organizerUserId": 2,
      "organizerDisplayName": "Event Manager User",
      "publishedAtUtc": null,
      "closedAtUtc": null,
      "cancelledAtUtc": null,
      "createdAtUtc": "2026-09-05T10:00:00Z",
      "updatedAtUtc": null,
      "confirmedRegistrations": 0,
      "waitlistCount": 0
    },
    "errors": null
  }
  ```
* **Curl Command:**
  ```bash
  curl.exe -s -X POST http://localhost:5195/api/v1/event-manager/events \
    -H "Authorization: Bearer <MANAGER_TOKEN>" \
    -H "Content-Type: application/json" \
    -d "{\"title\":\"Tech Summit 2026\",\"description\":\"Annual Technical Conference\",\"venue\":\"Convention Hall A\",\"startAtUtc\":\"2026-10-01T09:00:00Z\",\"endAtUtc\":\"2026-10-01T17:00:00Z\",\"capacity\":100}"
  ```

### 5.2 Update Event Details
* **HTTP Method:** `PUT`
* **Route:** `/api/v1/event-manager/events`
* **Authorization:** Policy = `EventManager`
* **Request Body (`EventUpdateDto`):**
  ```json
  {
    "eventId": 5,
    "title": "Updated Tech Summit 2026",
    "description": "Updated venue and agenda details",
    "venue": "Convention Hall B",
    "startAtUtc": "2026-10-01T09:00:00Z",
    "endAtUtc": "2026-10-01T17:00:00Z",
    "registrationOpenAtUtc": "2026-09-01T00:00:00Z",
    "registrationCloseAtUtc": "2026-09-30T23:59:59Z",
    "capacity": 150
  }
  ```
* **Response Payload (`200 OK` - Wrapped in `ApiResponse<EventResponseDto>`)**

### 5.3 Event Life-Cycle Transitions (Publish / Close / Cancel)
* **Endpoints:**
  * `POST /api/v1/event-manager/events/{eventId}/publish`
  * `POST /api/v1/event-manager/events/{eventId}/close`
  * `POST /api/v1/event-manager/events/{eventId}/cancel`
* **Authorization:** Policy = `EventManager`
* **Request Body (`EventStatusTransitionDto` - Optional):**
  ```json
  {
    "remarks": "Publishing event after venue confirmation."
  }
  ```
* **Response Payload (`200 OK` - Wrapped in `ApiResponse<EventResponseDto>`)**

### 5.4 List Organizer Events (Paginated)
* **HTTP Method:** `GET`
* **Route:** `/api/v1/event-manager/events?status=Published&pageNumber=1&pageSize=20`
* **Authorization:** Policy = `EventManager`

### 5.5 Registration Requests (On-Behalf Registration)
* **Create Request:** `POST /api/v1/event-manager/registration-requests`
  * Body (`RegistrationRequestCreateDto`): `{"eventId": 5, "attendeeUserId": 3}`
* **List Requests:** `GET /api/v1/event-manager/registration-requests?eventId=5&pageNumber=1&pageSize=10`
* **Decide Request:** `POST /api/v1/event-manager/registration-requests/{requestId}/decide`
  * Body (`RegistrationRequestDecisionDto`): `{"decision": "Accepted", "responseComment": "Approved by manager."}`

### 5.6 Attendance Management & Audit Correction
* **Record Attendance:** `POST /api/v1/event-manager/attendance`
  * Body (`AttendanceRecordDto`): `{"registrationId": 15, "attendanceStatus": "Attended", "finalize": false}`
* **Correct Attendance Record:** `PUT /api/v1/event-manager/attendance/{registrationId}/correct`
  * Body (`AttendanceCorrectionDto`): `{"attendanceStatus": "Absent", "correctionReason": "Verified absent via roll call audit."}`
* **Get Attendance For Event:** `GET /api/v1/event-manager/attendance/event/{eventId}`
* **Get Attendance For Single Registration:** `GET /api/v1/event-manager/attendance/registration/{registrationId}`

---

## 6. Administrator Operations

### 6.1 Create New User
* **HTTP Method:** `POST`
* **Route:** `/api/v1/admin/users`
* **Authorization:** Role = `Administrator`
* **Request Body (`UserCreateDto`):**
  ```json
  {
    "email": "manager_test@example.com",
    "userName": "manager_test",
    "displayName": "Test Event Manager",
    "phoneNumber": "1234567890",
    "roleId": 2
  }
  ```
* **Response Payload (`201 Created` - Wrapped in `ApiResponse<UserResponseDto>`):**
  ```json
  {
    "success": true,
    "statusCode": 201,
    "message": "User created successfully.",
    "data": {
      "userId": 5,
      "email": "manager_test@example.com",
      "userName": "manager_test",
      "displayName": "Test Event Manager",
      "phoneNumber": "1234567890",
      "roleId": 2,
      "roleName": "EventManager",
      "isActive": true,
      "deactivatedAtUtc": null,
      "createdAtUtc": "2026-09-05T10:00:00Z",
      "updatedAtUtc": null
    },
    "errors": null
  }
  ```

### 6.2 Get Paginated System Users List
* **HTTP Method:** `GET`
* **Route:** `/api/v1/admin/users?pageNumber=1&pageSize=10`
* **Authorization:** Role = `Administrator`
* **Response Payload (`200 OK` - Wrapped in `ApiResponse<PaginatedResponse<UserResponseDto>>`)**

### 6.3 Get User By ID
* **HTTP Method:** `GET`
* **Route:** `/api/v1/admin/users/{userId}`
* **Authorization:** Role = `Administrator`

### 6.4 Update User Role
* **HTTP Method:** `PUT`
* **Route:** `/api/v1/admin/users/role`
* **Authorization:** Role = `Administrator`
* **Request Body (`RoleUpdateDto`):**
  ```json
  {
    "userId": 5,
    "roleId": 3
  }
  ```

### 6.5 Deactivate User
* **HTTP Method:** `POST`
* **Route:** `/api/v1/admin/users/{userId}/deactivate`
* **Authorization:** Role = `Administrator`

### 6.6 Admin Reports & Analytical Analytics
* `GET /api/v1/admin/reports/registrations?eventId=1&startDate=2026-09-01&endDate=2026-10-01&pageNumber=1&pageSize=20`
* `GET /api/v1/admin/reports/attendance?eventId=1&pageNumber=1&pageSize=20`
* `GET /api/v1/admin/reports/audit-trail?actorUserId=1&actionType=Login&pageNumber=1&pageSize=20`
* `GET /api/v1/admin/reports/registrations/export?format=excel` *(Downloads Excel `.xlsx` file stream)*
* `GET /api/v1/admin/reports/attendance/export?format=excel` *(Downloads Excel `.xlsx` file stream)*
* `GET /api/v1/admin/audit-logs?actorUserId=1&actionType=Login&pageNumber=1&pageSize=10`
* `GET /api/v1/admin/metrics` *(Returns system memory, active user count, 24-hr audit count, uptime)*

---

## 7. Notifications System

### 7.1 List User Notifications (Paginated)
* **HTTP Method:** `GET`
* **Route:** `/api/v1/notifications?unreadOnly=false&pageNumber=1&pageSize=10`
* **Authorization:** Authenticated (Any Role)
* **Response Payload (`200 OK` - Wrapped in `ApiResponse<PaginatedResponse<NotificationItemDto>>`):**
  ```json
  {
    "success": true,
    "statusCode": 200,
    "message": "Notifications retrieved.",
    "data": {
      "data": [
        {
          "notificationId": 10,
          "notificationType": "WaitlistPromotion",
          "title": "Promoted to Confirmed!",
          "message": "You have been moved from the waitlist to confirmed registration for Tech Conference 2026.",
          "relatedEventId": 1,
          "relatedRegistrationId": 16,
          "relatedRequestId": null,
          "deliveryStatus": "Sent",
          "sentAtUtc": "2026-09-05T09:30:00Z",
          "readAtUtc": null,
          "createdAtUtc": "2026-09-05T09:30:00Z"
        }
      ],
      "pageNumber": 1,
      "pageSize": 10,
      "totalRecords": 1,
      "totalPages": 1,
      "hasNextPage": false,
      "hasPreviousPage": false
    },
    "errors": null
  }
  ```

### 7.2 Get Unread Notification Count
* **HTTP Method:** `GET`
* **Route:** `/api/v1/notifications/unread-count`
* **Authorization:** Authenticated (Any Role)
* **Response Payload (`200 OK` - Wrapped in `ApiResponse<number>`):**
  ```json
  {
    "success": true,
    "statusCode": 200,
    "message": "Unread count retrieved.",
    "data": 1,
    "errors": null
  }
  ```

### 7.3 Mark Single Notification as Read
* **HTTP Method:** `PUT`
* **Route:** `/api/v1/notifications/{notificationId}/read`
* **Authorization:** Authenticated (Any Role)

### 7.4 Mark All Notifications as Read
* **HTTP Method:** `PUT`
* **Route:** `/api/v1/notifications/read-all`
* **Authorization:** Authenticated (Any Role)

---

## 8. Business Management Portal

### 8.1 Get Real-Time Business Dashboard Metrics & Trends
* **HTTP Method:** `GET`
* **Route:** `/api/v1/business/dashboard`
* **Authorization:** Policy = `BusinessManagement` (`BusinessManagement` or `Administrator`)
* **Response Payload (`200 OK` - Wrapped in `ApiResponse<DashboardMetricsDto>`):**
  ```json
  {
    "success": true,
    "statusCode": 200,
    "message": "Dashboard metrics retrieved successfully.",
    "data": {
      "capturedAtUtc": "2026-09-05T10:15:00Z",
      "totalEvents": 32,
      "activeEvents": 8,
      "totalRegistrations": 120,
      "totalConfirmedRegistrations": 105,
      "overallCapacityUtilizationRate": 85.5,
      "overallAttendanceRate": 92.3,
      "registrationTrends": [
        { "dateLabel": "2026-08-30", "count": 12 },
        { "dateLabel": "2026-08-31", "count": 18 },
        { "dateLabel": "2026-09-01", "count": 25 }
      ]
    },
    "errors": null
  }
  ```
* **Curl Command:**
  ```bash
  curl.exe -s -X GET http://localhost:5195/api/v1/business/dashboard \
    -H "Authorization: Bearer <BUSINESS_TOKEN>"
  ```

### 8.2 Summary-Level Registration Reports (Read-Only)
* **HTTP Method:** `GET`
* **Route:** `/api/v1/business/reports/registrations?startDate=2026-09-01&endDate=2026-10-01&pageNumber=1&pageSize=20`
* **Authorization:** Policy = `BusinessManagement`
* **Response Payload (`200 OK` - Wrapped in `ApiResponse<PaginatedResponse<EventSummaryDto>>`)**

### 8.3 Summary-Level Attendance Reports (Read-Only)
* **HTTP Method:** `GET`
* **Route:** `/api/v1/business/reports/attendance?startDate=2026-09-01&endDate=2026-10-01&pageNumber=1&pageSize=20`
* **Authorization:** Policy = `BusinessManagement`

### 8.4 Event Completion Reports (Read-Only)
* **HTTP Method:** `GET`
* **Route:** `/api/v1/business/reports/completion?pageNumber=1&pageSize=20`
* **Authorization:** Policy = `BusinessManagement`

