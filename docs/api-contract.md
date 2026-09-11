# SmartWaste — Public API Contract Specification

> **Status:** FROZEN SOURCE OF TRUTH  
> **Base Prefix:** `/api/v1`  
> **Protocol:** HTTPS / REST  
> **Format:** JSON (`camelCase` properties)  
> **Error Standard:** RFC 7807 `ProblemDetails` (`application/problem+json`)  
> **Authentication:** Bearer JWT in `Authorization` header (`Bearer <token>`)

This document defines the frozen RESTful API contract for the Smart Waste Management System. All endpoints exposed by the authoritative ASP.NET Core 8 Web API backend are documented below. 

> [!IMPORTANT]
> **Gateway Rule:** React and Flutter client applications interact **exclusively** with ASP.NET Core Web API at `/api/v1`. The Python FastAPI service is strictly an internal microservice and exposes no public client endpoints.
> 
> **Status Badges:**
> - `[Implemented]`: Fully built, tested, and operational in the codebase.
> - `[Planned]`: Frozen design contract; implementation pending in subsequent phases.

---

## 1. Global Conventions & Standards

### 1.1 JSON Formatting
All JSON request and response bodies use `camelCase` naming:
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fullName": "Kasun Perera",
  "isActive": true
}
```

### 1.2 Pagination Standard
List endpoints returning multiple records adhere to the standard query parameters and envelope:

#### Query Parameters
- `page` (integer, default: `1`, minimum: `1`)
- `pageSize` (integer, default: `20`, maximum: `100`)

#### Response Envelope (`PagedResult<T>`)
```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 85,
  "totalPages": 5
}
```

### 1.3 Error Handling (RFC 7807 `ProblemDetails`)
All error responses return standard `ProblemDetails`:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "License number already registered.",
  "instance": "/api/v1/drivers",
  "errors": {
    "licenseNumber": [
      "The license number must be unique."
    ]
  }
}
```

Common HTTP status codes:
- `200 OK`: Request succeeded.
- `201 Created`: Resource successfully created.
- `204 NoContent`: Operation completed without response body.
- `400 BadRequest`: Validation or malformed payload.
- `401 Unauthorized`: Missing or invalid JWT Bearer token.
- `403 Forbidden`: Authenticated user lacks the required role.
- `404 NotFound`: Requested resource or dev route not found.
- `409 Conflict`: Uniqueness constraint violation or invalid concurrent state.
- `503 ServiceUnavailable`: Downstream internal service (e.g., FastAPI AI service) unavailable.

---

## 2. Authentication & Diagnostic Endpoints

### 2.1 `POST /api/v1/auth/register` `[Implemented]`
Registers a new citizen account. Publicly accessible.

- **Access:** Public
- **Validation:** Enforces name, valid email, phone number, and password complexity. Role selection is strictly disallowed; always registers as `Citizen`.

#### Request Body
```json
{
  "fullName": "Kamal Silva",
  "email": "kamal@example.com",
  "phoneNumber": "+94771234567",
  "password": "Password123!"
}
```

#### Response `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-09-08T18:00:00Z",
  "user": {
    "id": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
    "fullName": "Kamal Silva",
    "email": "kamal@example.com",
    "role": "Citizen"
  }
}
```

---

### 2.2 `POST /api/v1/auth/login` `[Implemented]`
Authenticates an active user and returns an access token.

- **Access:** Public
- **Platform Enforcements:** Supports `clientType` (`"web"` | `"mobile"`).
  - Web client allows only `WasteOfficer` and `MunicipalManager`.
  - Mobile client allows only `Citizen` and `Driver`.
  - Incompatible platform login attempts are rejected with `403 Forbidden` (`ProblemDetails`) and issue no JWT.

#### Request Body
```json
{
  "email": "kamal@example.com",
  "password": "Password123!",
  "clientType": "web"
}
```

#### Response `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-09-08T18:00:00Z",
  "mustChangePassword": false,
  "user": {
    "id": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
    "fullName": "Kamal Silva",
    "email": "kamal@example.com",
    "role": "WasteOfficer",
    "mustChangePassword": false
  }
}
```

---

### 2.3 `POST /api/v1/auth/change-password` `[Implemented]`
Changes the authenticated user's password. Required when `MustChangePassword` is `true` before accessing business resources.

- **Access:** Authenticated
- **Validation:** Current password must be verified; new password must be at least 8 characters with uppercase, lowercase, digit, and non-alphanumeric character.

#### Request Body
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!"
}
```

#### Response `200 OK`
```json
{
  "message": "Password changed successfully. Please log in with your new password."
}
```

---

### 2.4 `GET /api/v1/auth/me` `[Implemented]`
Retrieves current profile information for the authenticated user.

- **Access:** Authenticated (`Citizen`, `WasteOfficer`, `Driver`, `MunicipalManager`)

#### Response `200 OK`
```json
{
  "id": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
  "fullName": "Kamal Silva",
  "email": "kamal@example.com",
  "phoneNumber": "+94771234567",
  "role": "WasteOfficer",
  "isActive": true,
  "mustChangePassword": false,
  "createdAt": "2026-09-08T07:30:00Z"
}
```

---

### 2.5 User Management Endpoints `[Implemented]`

Internal staff account administration managed exclusively by Municipal Managers.

#### `GET /api/v1/users`
Retrieves a paginated list of internal staff users.

- **Access:** `MunicipalManager`
- **Query Parameters:** `page` (default 1), `pageSize` (default 15), `search`, `role`, `isActive`
- **Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "a53e6cf1-d419-4f71-a0ea-439ff2e43486",
      "fullName": "Saman Driver",
      "email": "saman@smartwaste.local",
      "username": "saman_driver",
      "role": "Driver",
      "isActive": true,
      "mustChangePassword": true,
      "createdAt": "2026-09-10T06:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 15,
  "totalCount": 1,
  "totalPages": 1
}
```

#### `POST /api/v1/users`
Provisions an internal staff account (`Driver`, `WasteOfficer`, or `MunicipalManager`). Rejects `Citizen` role (public registration only). Cryptographically generates a secure 16-character temporary password returned strictly once in the creation response.

- **Access:** `MunicipalManager`
- **Request Body:**
```json
{
  "fullName": "Saman Perera",
  "email": "saman.driver@smartwaste.local",
  "username": "saman_driver",
  "role": "Driver"
}
```
- **Response `201 Created`:**
```json
{
  "user": {
    "id": "a53e6cf1-d419-4f71-a0ea-439ff2e43486",
    "fullName": "Saman Perera",
    "email": "saman.driver@smartwaste.local",
    "username": "saman_driver",
    "role": "Driver",
    "isActive": true,
    "mustChangePassword": true,
    "createdAt": "2026-09-10T06:00:00Z"
  },
  "temporaryPassword": "pW#9xK!2mQ$7vL@1"
}
```

#### `PATCH /api/v1/users/{id}/status`
Toggles account status (`isActive`). Prevents managers from deactivating their own account.

- **Access:** `MunicipalManager`
- **Request Body:**
```json
{
  "isActive": false
}
```
- **Response `200 OK`:** Updated `UserManagementDto` object.

---

### 2.6 `GET /api/v1/dev/ai-health` `[Implemented]`
Development-only diagnostic endpoint verifying ASP.NET Core → FastAPI AI service connectivity.

- **Access:** Public (Only active in `Development` environment; returns `404 NotFound` in `Production`).
- **Response `200 OK`:** `{"status":"healthy","service":"SmartWaste AI Service"}`
- **Response `503 Service Unavailable`:** RFC 7807 `ProblemDetails` when AI service is unreachable.

---

## 3. Component 1 — Waste Reporting & Citizen Management

### 3.1 `POST /api/v1/waste-reports` `[Planned]`
Submits a new waste report with description, location, and waste classification.

- **Access:** `Citizen`
- **Request Body:**
```json
{
  "description": "Large garbage heap overflowing near bus stand",
  "wasteType": "General",
  "latitude": 6.9271,
  "longitude": 79.8612,
  "addressText": "Main Street, Pettah",
  "attachmentUrls": [
    "https://storage.smartwaste.lk/reports/2026/09/dump1.jpg"
  ]
}
```
- **Response `201 Created`:** Full `WasteReportDto` in status `Submitted`.

---

### 3.2 `GET /api/v1/waste-reports` `[Planned]`
Lists waste reports with pagination and filtering.

- **Access:** Authenticated
  - `Citizen`: Filtered strictly to reports submitted by the caller.
  - `WasteOfficer`, `MunicipalManager`: Unrestricted access across all citizen reports.
- **Query Parameters:** `page`, `pageSize`, `status`, `wasteType`, `fromDate`, `toDate`.
- **Response `200 OK`:** `PagedResult<WasteReportSummaryDto>`.

---

### 3.3 `GET /api/v1/waste-reports/{id}` `[Planned]`
Retrieves full details of a specific report including attachments and current status.

- **Access:** `Citizen` (Owner only), `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** Full `WasteReportDetailDto`.

---

### 3.4 `PATCH /api/v1/waste-reports/{id}` `[Planned]`
Updates report metadata. Citizens can update only while report is in `Submitted` status. Officers can update priority or notes.

- **Access:** `Citizen` (Owner), `WasteOfficer`.
- **Response `200 OK`:** Updated `WasteReportDto`.

---

### 3.5 `POST /api/v1/waste-reports/{id}/verify` `[Planned]`
Officer verification of a submitted report. **Non-trivial operation:** Sets status to `Verified`, assigns priority, logs history, and marks report eligible for AI workflow planning.

- **Access:** `WasteOfficer`, `MunicipalManager` (Citizens **cannot** verify).
- **Request Body:**
```json
{
  "priority": "High",
  "notes": "Confirmed overflowing municipal skip bin requiring flatbed truck dispatch."
}
```
- **Response `200 OK`:** Updated `WasteReportDto` (`status: "Verified"`).

---

### 3.6 `POST /api/v1/waste-reports/{id}/reject` `[Planned]`
Rejects an invalid, duplicate, or out-of-boundary report.

- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Request Body:**
```json
{
  "reason": "Duplicate report already covered by scheduled pickup."
}
```
- **Response `200 OK`:** Updated `WasteReportDto` (`status: "Rejected"`).

---

### 3.7 `GET /api/v1/waste-reports/{id}/history` `[Planned]`
Returns the chronological status change history of a report.

- **Access:** `Citizen` (Owner), `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** Array of `WasteReportStatusHistoryDto`.

---

## 4. Component 2 — Waste Collection & Bin Management

### 4.1 Bins Endpoints

#### `GET /api/v1/bins` `[Planned]`
Lists municipal bins with filtering by zone, status, and fill-level threshold.
- **Access:** `WasteOfficer`, `MunicipalManager`, `Driver`.
- **Query Parameters:** `zoneId`, `status`, `minFillLevel`, `page`, `pageSize`.
- **Response `200 OK`:** `PagedResult<WasteBinDto>`.

#### `GET /api/v1/bins/{id}` `[Planned]`
Retrieves detailed bin status, coordinates, and maintenance history.
- **Access:** `WasteOfficer`, `MunicipalManager`, `Driver`.
- **Response `200 OK`:** `WasteBinDetailDto`.

#### `POST /api/v1/bins` `[Planned]`
Registers a new smart bin station.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Request Body:** `binCode`, `collectionZoneId`, `latitude`, `longitude`, `capacity`.
- **Response `201 Created`:** `WasteBinDto`.

#### `PUT /api/v1/bins/{id}` `[Planned]`
Updates bin operational metadata or maintenance state.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** `WasteBinDto`.

#### `PATCH /api/v1/bins/{id}/fill-level` `[Planned]`
Updates current fill level (simulated IoT sensor or officer field inspection).
- **Access:** `WasteOfficer`, `Driver`.
- **Request Body:** `{"currentFillLevel": 85.5}`
- **Response `200 OK`:** Updated `WasteBinDto` (transitions status to `Full` if $\ge 80\%$).

---

### 4.2 Collection Schedules Endpoints

#### `POST /api/v1/collection-schedules` `[Planned]`
Creates a collection schedule draft for a specific zone.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Request Body:** `collectionZoneId`, `name`, `scheduledDate`.
- **Response `201 Created`:** `CollectionScheduleDto`.

#### `GET /api/v1/collection-schedules` `[Planned]`
Lists schedules by zone, date range, or status.
- **Access:** `WasteOfficer`, `MunicipalManager`, `Driver`.
- **Response `200 OK`:** `PagedResult<CollectionScheduleDto>`.

#### `GET /api/v1/collection-schedules/{id}` `[Planned]`
Retrieves schedule details including assigned sequence of bins.
- **Access:** `WasteOfficer`, `MunicipalManager`, `Driver`.
- **Response `200 OK`:** `CollectionScheduleDetailDto`.

#### `POST /api/v1/collection-schedules/{id}/bins` `[Planned]`
Attaches a bin to the schedule with route sequence ordering.
- **Access:** `WasteOfficer`.
- **Request Body:** `{"wasteBinId": "...", "sequence": 1}`
- **Response `200 OK`:** Updated schedule bin manifest.

#### `DELETE /api/v1/collection-schedules/{id}/bins/{binId}` `[Planned]`
Removes a bin from the collection schedule.
- **Access:** `WasteOfficer`.
- **Response `204 NoContent`**.

#### `POST /api/v1/collection-schedules/{id}/generate-tasks` `[Planned]`
**Non-trivial business operation:** Converts a finalized schedule into dispatched `CollectionTask` and `CollectionTaskItem` records.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** `CollectionTaskDto` created.

---

### 4.3 Collection Tasks Endpoints

#### `GET /api/v1/collection-tasks` `[Planned]`
Lists collection tasks filtered by status (`Pending`, `Assigned`, `InProgress`, `Completed`).
- **Access:** `WasteOfficer`, `MunicipalManager`, `Driver`.
- **Response `200 OK`:** `PagedResult<CollectionTaskDto>`.

#### `GET /api/v1/collection-tasks/{id}` `[Planned]`
Retrieves task waypoints, stop items, and origin (report or schedule).
- **Access:** `WasteOfficer`, `MunicipalManager`, `Driver`.
- **Response `200 OK`:** `CollectionTaskDetailDto`.

#### `POST /api/v1/collection-tasks` `[Planned]`
Creates an ad-hoc collection task from a verified report or emergency request.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Response `201 Created`:** `CollectionTaskDto`.

#### `POST /api/v1/collection-tasks/{id}/status` `[Planned]`
Transitions task lifecycle status.
- **Access:** `WasteOfficer`, `Driver`.
- **Request Body:** `{"status": "InProgress", "notes": "Commencing zone sweep"}`
- **Response `200 OK`:** Updated `CollectionTaskDto`.

#### `GET /api/v1/collection-tasks/{id}/history` `[Planned]`
Audit history of task transitions.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** Array of `CollectionTaskStatusHistoryDto`.

---

## 5. Component 3 — Fleet, Driver & Route Management

### 5.1 Vehicles Endpoints

#### `GET /api/v1/vehicles` `[Planned]`
Lists fleet vehicles with status, type, and availability filters.
- **Access:** `WasteOfficer`, `MunicipalManager`, `Driver`.
- **Response `200 OK`:** `PagedResult<VehicleDto>`.

#### `GET /api/v1/vehicles/{id}` `[Planned]`
Vehicle details, load capacity, and active assignment.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** `VehicleDetailDto`.

#### `POST /api/v1/vehicles` `[Planned]`
Registers a new vehicle in the municipal fleet.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Request Body:** `registrationNumber`, `vehicleType`, `capacity`.
- **Response `201 Created`:** `VehicleDto`.

#### `PATCH /api/v1/vehicles/{id}/status` `[Planned]`
Updates vehicle operational status (`Available`, `Maintenance`, `Inactive`).
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** Updated `VehicleDto`.

---

### 5.2 Drivers Endpoints

#### `GET /api/v1/drivers` `[Planned]`
Lists municipal drivers and current availability status.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** `PagedResult<DriverSummaryDto>`.

#### `GET /api/v1/drivers/{id}` `[Planned]`
Retrieves driver profile, license details, and active assignment.
- **Access:** `WasteOfficer`, `MunicipalManager`, `Driver` (Self).
- **Response `200 OK`:** `DriverProfileDto`.

#### `PATCH /api/v1/drivers/{id}/availability` `[Planned]`
Toggles availability status (`Available`, `OffDuty`).
- **Access:** `Driver` (Self only), `WasteOfficer`.
- **Request Body:** `{"availabilityStatus": "Available"}`
- **Response `200 OK`:** Updated `DriverProfileDto`.

---

### 5.3 Collection Assignments Endpoints

#### `POST /api/v1/assignments` `[Planned]`
**Non-trivial business operation:** Binds a `CollectionTask`, a qualified `Driver`, and a suitable `Vehicle`. Performs strict deterministic validation preventing overlapping assignments for driver or vehicle.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Request Body:**
```json
{
  "collectionTaskId": "d3b07384-d113-4f44-8cc0-f3a763886561",
  "driverId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "vehicleId": "f9e8d7c6-b5a4-3210-fedc-ba0987654321"
}
```
- **Response `201 Created`:** `CollectionAssignmentDto`.

#### `GET /api/v1/assignments` `[Planned]`
Lists assignments. Drivers only see their own assignments; Officers/Managers see all.
- **Access:** `Driver`, `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** `PagedResult<CollectionAssignmentDto>`.

#### `GET /api/v1/assignments/{id}` `[Planned]`
Detailed assignment view including associated task items and route.
- **Access:** `Driver` (Assignee), `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** `CollectionAssignmentDetailDto`.

#### `POST /api/v1/assignments/{id}/accept` `[Planned]`
Driver accepts the assigned task via mobile application.
- **Access:** `Driver` (Assignee).
- **Response `200 OK`:** Updated assignment (`status: "Accepted"`).

#### `POST /api/v1/assignments/{id}/start` `[Planned]`
Driver starts navigation and collection.
- **Access:** `Driver` (Assignee).
- **Response `200 OK`:** Updated assignment (`status: "InProgress"`).

#### `POST /api/v1/assignments/{id}/complete` `[Planned]`
Driver marks collection duty completed.
- **Access:** `Driver` (Assignee).
- **Response `200 OK`:** Updated assignment (`status: "Completed"`).

---

### 5.4 Routes Endpoints

#### `GET /api/v1/routes/{id}` `[Planned]`
Retrieves calculated route geometry and ordered stops.
- **Access:** `Driver`, `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:**
```json
{
  "id": "e4b1a2c3-d4e5-6789-0123-abcdef456789",
  "estimatedDistance": 14250.0,
  "estimatedDuration": 3200.0,
  "status": "Active",
  "stops": [
    {
      "id": "11111111-2222-3333-4444-555555555555",
      "sequence": 1,
      "latitude": 6.9271,
      "longitude": 79.8612,
      "status": "Completed"
    }
  ]
}
```
*(Note: External routing engine credentials remain completely confidential and are never exposed).*

#### `PATCH /api/v1/routes/{id}/stops/{stopId}` `[Planned]`
Driver updates progress at an individual stop waypoint.
- **Access:** `Driver`.
- **Request Body:** `{"status": "Completed"}` (or `Skipped` with notes).
- **Response `200 OK`:** Updated `RouteStopDto`.

---

## 6. Component 4 — Operations, Complaints & Analytics

### 6.1 Complaints Endpoints

#### `POST /api/v1/complaints` `[Planned]`
Submits a service-quality grievance (missed pickup, spillage). Distinct from waste report.
- **Access:** `Citizen`.
- **Request Body:**
```json
{
  "subject": "Missed scheduled collection on Second Avenue",
  "description": "The green organic bin was placed by 6:00 AM but the truck passed without emptying it.",
  "wasteReportId": null
}
```
- **Response `201 Created`:** `ComplaintDto` in status `Open`.

#### `GET /api/v1/complaints` `[Planned]`
Lists complaints. Citizens see own complaints; Officers/Managers see all.
- **Access:** Authenticated.
- **Query Parameters:** `status`, `page`, `pageSize`.
- **Response `200 OK`:** `PagedResult<ComplaintSummaryDto>`.

#### `GET /api/v1/complaints/{id}` `[Planned]`
Retrieves full complaint investigation and notes.
- **Access:** `Citizen` (Owner), `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** `ComplaintDetailDto`.

#### `POST /api/v1/complaints/{id}/assign` `[Planned]`
Assigns investigating officer.
- **Access:** `MunicipalManager`, `WasteOfficer`.
- **Response `200 OK`:** Updated `ComplaintDto` (`status: "UnderReview"`).

#### `POST /api/v1/complaints/{id}/resolve` `[Planned]`
Resolves complaint with mandatory official explanation.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Request Body:** `{"resolutionNotes": "Truck returned at 2:30 PM and completed pickup. Driver counseled."}`
- **Response `200 OK`:** Updated `ComplaintDto` (`status: "Resolved"`).

#### `POST /api/v1/complaints/{id}/reject` `[Planned]`
Rejects unsubstantiated complaint.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Request Body:** `{"reason": "Bin was not presented at curbside during collection window."}`
- **Response `200 OK`:** Updated `ComplaintDto` (`status: "Rejected"`).

---

### 6.2 Operational Incidents Endpoints

#### `POST /api/v1/incidents` `[Planned]`
Logs field incident (mechanical breakdown, road blockage, access obstruction).
- **Access:** `Driver`, `WasteOfficer`.
- **Request Body:** `type`, `description`, `severity`, `collectionAssignmentId`.
- **Response `201 Created`:** `OperationalIncidentDto`.

#### `GET /api/v1/incidents` `[Planned]`
Lists open and resolved incidents.
- **Access:** `WasteOfficer`, `MunicipalManager`, `Driver`.
- **Response `200 OK`:** `PagedResult<OperationalIncidentDto>`.

#### `PATCH /api/v1/incidents/{id}/resolve` `[Planned]`
Marks incident resolved after replacement vehicle dispatched or road cleared.
- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** Updated `OperationalIncidentDto`.

---

### 6.3 Notifications Endpoints

#### `GET /api/v1/notifications` `[Planned]`
Retrieves user's notifications.
- **Access:** Authenticated (all roles).
- **Query Parameters:** `isRead`, `page`, `pageSize`.
- **Response `200 OK`:** `PagedResult<NotificationDto>`.

#### `PATCH /api/v1/notifications/{id}/read` `[Planned]`
Marks a specific notification as read.
- **Access:** Authenticated (Recipient only).
- **Response `204 NoContent`**.

#### `POST /api/v1/notifications/read-all` `[Planned]`
Marks all user notifications as read.
- **Access:** Authenticated (Recipient only).
- **Response `204 NoContent`**.

---

### 6.4 Analytics Endpoints (Read-Only Projections)

#### `GET /api/v1/analytics/dashboard` `[Planned]`
Executive dashboard summary.
- **Access:** `MunicipalManager`, `WasteOfficer`.
- **Response `200 OK`:**
```json
{
  "activeCollectionTasks": 12,
  "fleetUtilizationPercentage": 78.5,
  "pendingVerifiedReports": 4,
  "openComplaintsCount": 7,
  "averageResolutionHours": 4.2
}
```

#### `GET /api/v1/analytics/bins` `[Planned]`
Zone bin fullness distribution and overflow frequency.
- **Access:** `MunicipalManager`, `WasteOfficer`.
- **Response `200 OK`:** Bin telemetry and capacity distribution metrics.

#### `GET /api/v1/analytics/performance` `[Planned]`
Collection efficiency, fuel metrics, and SLA compliance.
- **Access:** `MunicipalManager`.
- **Response `200 OK`:** Historical aggregated performance trends.

---

## 7. Future Agentic AI Endpoints (Authoritative ASP.NET Core)

These endpoints belong **strictly** to ASP.NET Core. React and Flutter call these endpoints. ASP.NET Core internally forwards structured payloads to the Python FastAPI service, evaluates deterministic rules, and requires manager approval before committing any operational change.

### 7.1 `POST /api/v1/ai/workflows` `[Planned]`
Initiates an AI planning run for a verified report or schedule.

- **Access:** `WasteOfficer`, `MunicipalManager`
- **Request Body:**
```json
{
  "workflowType": "RouteOptimization",
  "objective": "Optimize Tuesday morning pickup route for Zone 1 after report verification",
  "relatedEntityType": "WasteReport",
  "relatedEntityId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```
- **Response `202 Accepted`:**
```json
{
  "workflowId": "7f8e9d0a-1b2c-3d4e-5f60-7a8b9c0d1e2f",
  "status": "Created",
  "message": "AI planning workflow initiated."
}
```

---

### 7.2 `GET /api/v1/ai/workflows/{id}` `[Planned]`
Retrieves current workflow status and proposed recommendation.

- **Access:** `WasteOfficer`, `MunicipalManager`
- **Response `200 OK`:**
```json
{
  "id": "7f8e9d0a-1b2c-3d4e-5f60-7a8b9c0d1e2f",
  "status": "AwaitingApproval",
  "objective": "Optimize Tuesday morning pickup route for Zone 1",
  "startedAt": "2026-09-08T08:00:00Z",
  "validationResults": [
    {
      "ruleName": "DriverAvailabilityCheck",
      "isValid": true,
      "message": "Driver is on duty and available."
    },
    {
      "ruleName": "VehicleCapacityCheck",
      "isValid": true,
      "message": "Compactor truck capacity exceeds estimated waste volume by 32%."
    }
  ],
  "proposedPlan": {
    "assignedDriverId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "assignedVehicleId": "f9e8d7c6-b5a4-3210-fedc-ba0987654321",
    "stopCount": 8,
    "estimatedDistanceMeters": 12400.0,
    "estimatedDurationSeconds": 2800.0
  }
}
```

---

### 7.3 `GET /api/v1/ai/workflows` `[Planned]`
Lists AI workflows with status and type filters.

- **Access:** `WasteOfficer`, `MunicipalManager`
- **Response `200 OK`:** `PagedResult<AiWorkflowSummaryDto>`

---

### 7.4 `GET /api/v1/ai/workflows/{id}/audit` `[Planned]`
Returns the full, transparent audit trace of agent actions and tool calls. Hidden chain-of-thought tokens are excluded; only structured inputs and outputs are returned.

- **Access:** `MunicipalManager`, `WasteOfficer`
- **Response `200 OK`:**
```json
{
  "workflowId": "7f8e9d0a-1b2c-3d4e-5f60-7a8b9c0d1e2f",
  "steps": [
    {
      "stepNumber": 1,
      "agentName": "PlannerAgent",
      "action": "Decompose route optimization objective",
      "status": "Completed",
      "resultSummary": "Identified 1 overflow report and 7 full bins in Zone 1"
    },
    {
      "stepNumber": 2,
      "agentName": "FleetRouteAgent",
      "action": "Calculate TSP waypoint ordering",
      "status": "Completed",
      "resultSummary": "Generated 8-waypoint path with 12.4km total distance"
    }
  ],
  "toolCalls": [
    {
      "toolName": "GetBinMetrics",
      "input": {"zoneId": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91"},
      "output": {"fullBinIds": ["..."]},
      "status": "Success"
    }
  ]
}
```

---

### 7.5 `POST /api/v1/ai/workflows/{id}/decision` `[Planned]`
**Human-in-the-Loop Approval:** Municipal Manager reviews and decides on the AI recommendation.

- **Access:** `MunicipalManager` only
- **Request Body:**
```json
{
  "decision": "Approved",
  "comments": "Plan approved for immediate dispatch."
}
```
- **Business Behavior on Approval:**
  1. ASP.NET Core executes an authoritative database transaction.
  2. Creates or updates `CollectionTask`, `CollectionAssignment`, and `Route` entities.
  3. Updates `WasteReport.Status` to `Scheduled`.
  4. Dispatches in-app notification to the assigned `Driver`.
  5. Records decision in `AiApproval` and `AuditLog`.
- **Response `200 OK`:**
```json
{
  "workflowId": "7f8e9d0a-1b2c-3d4e-5f60-7a8b9c0d1e2f",
  "decision": "Approved",
  "status": "Completed",
  "createdTaskId": "d3b07384-d113-4f44-8cc0-f3a763886561"
}
```
