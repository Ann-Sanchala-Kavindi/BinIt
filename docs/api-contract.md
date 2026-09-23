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
Submits a new waste report with description, waste type, and geospatial coordinates.

- **Access:** `Citizen` only
- **Validation Rules:**
  - `description`: Required, 10 to 1,000 characters.
  - `wasteType`: Required, must be a valid `WasteType` enum value (`General`, `Organic`, `Recyclable`, `Hazardous`, `Bulky`, `Other`).
  - `latitude`: Required, range `-90.0` to `90.0`.
  - `longitude`: Required, range `-180.0` to `180.0`.
  - `addressText`: Optional, maximum 500 characters.
- **Server-Controlled Fields (Clients MUST NOT provide):**
  - `citizenId`: Extracted authoritatively by backend from authenticated JWT claims (`sub`/`NameIdentifier`).
  - `status`: Set authoritatively to `Submitted`.
  - `priority`: Formalized as nullable enum `WasteReportPriority?` (`Low`, `Medium`, `High`, `Urgent`). Defaults to `null`. Priority remains null during Component 1 verification and may later be assigned only through authoritative ASP.NET business logic after operational/AI-assisted planning (AI may recommend but never persist it directly).
  - `verifiedByUserId`, `verifiedAt`: Default to `null`.
  - `attachmentUrls`: Attachments are uploaded via dedicated `POST /api/v1/waste-reports/{id}/attachments`.
- **Atomic Transaction:**
  1. Inserts `WasteReport` in `Submitted` status.
  2. Inserts initial `WasteReportStatusHistory` (`fromStatus = null`, `toStatus = "Submitted"`, `changedByUserId = CitizenId`, `changedAt = CreatedAt`).

#### Request Body
```json
{
  "description": "Large garbage heap overflowing near bus stand",
  "wasteType": "General",
  "latitude": 6.9271,
  "longitude": 79.8612,
  "addressText": "Main Street, Pettah"
}
```

#### Response `201 Created`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "citizenId": "1fa85f64-5717-4562-b3fc-2c963f66afa1",
  "description": "Large garbage heap overflowing near bus stand",
  "wasteType": "General",
  "latitude": 6.9271,
  "longitude": 79.8612,
  "addressText": "Main Street, Pettah",
  "status": "Submitted",
  "priority": null,
  "verifiedByUserId": null,
  "verifiedByUserName": null,
  "verifiedAt": null,
  "attachments": [],
  "createdAt": "2026-09-15T08:30:00Z",
  "updatedAt": null
}
```

---

### 3.2 `GET /api/v1/waste-reports` `[Planned]`
Lists waste reports with pagination, search, domain filtering, and sorting.

- **Access:** Authenticated
  - `Citizen`: Scoped strictly to reports submitted by the caller (`CitizenId == currentUserId`). Query parameters cannot circumvent this boundary.
  - `WasteOfficer`, `MunicipalManager`: Unrestricted operational scope across all citizen reports.
  - `Driver`: `403 Forbidden`.
- **Query Parameters:**
  - `page` (int, default: `1`, minimum: `1`)
  - `pageSize` (int, default: `20`, maximum: `100`)
  - `status` (string, optional): Filter by `WasteReportStatus`.
  - `wasteType` (string, optional): Filter by `WasteType`.
  - `search` (string, optional): Case-insensitive substring search matching `description` or `addressText`.
  - `sortBy` (string, optional, default: `"createdAt"`): Allowed values: `"createdAt"`, `"updatedAt"`.
  - `sortDirection` (string, optional, default: `"desc"`): Allowed values: `"asc"`, `"desc"`.

#### Response `200 OK` (`PagedResult<WasteReportSummaryDto>`)
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "description": "Large garbage heap overflowing near bus stand",
      "wasteType": "General",
      "status": "Submitted",
      "priority": null,
      "addressText": "Main Street, Pettah",
      "latitude": 6.9271,
      "longitude": 79.8612,
      "citizenId": "1fa85f64-5717-4562-b3fc-2c963f66afa1",
      "citizenName": "Kamal Perera",
      "createdAt": "2026-09-15T08:30:00Z",
      "updatedAt": null
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```
*(Note: `citizenId` and `citizenName` are exposed in summary items only to `WasteOfficer` and `MunicipalManager`; for `Citizen` callers, these fields are omitted or match own identity).*

---

### 3.3 `GET /api/v1/waste-reports/{id}` `[Planned]`
Retrieves full details of a specific report including photographic attachments and verification metadata.

- **Access:**
  - `Citizen`: Report owner only (`CitizenId == currentUserId`). Access attempts to reports belonging to other citizens return `404 NotFound`.
  - `WasteOfficer`, `MunicipalManager`: Any report across municipal jurisdiction.
  - `Driver`: `403 Forbidden`.

#### Response `200 OK` (`WasteReportDetailDto`)
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "citizenId": "1fa85f64-5717-4562-b3fc-2c963f66afa1",
  "citizenName": "Kamal Perera",
  "description": "Large garbage heap overflowing near bus stand",
  "wasteType": "General",
  "latitude": 6.9271,
  "longitude": 79.8612,
  "addressText": "Main Street, Pettah",
  "status": "Submitted",
  "priority": null,
  "verifiedByUserId": null,
  "verifiedByUserName": null,
  "verifiedAt": null,
  "attachments": [
    {
      "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
      "wasteReportId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fileUrl": "https://storage.smartwaste.local/reports/3fa85f64-5717-4562-b3fc-2c963f66afa6/dump1.jpg?token=...",
      "fileType": "image/jpeg",
      "createdAt": "2026-09-15T08:31:00Z"
    }
  ],
  "createdAt": "2026-09-15T08:30:00Z",
  "updatedAt": null
}
```

---

### 3.4 `PATCH /api/v1/waste-reports/{id}` `[Planned]`
Updates citizen-submitted evidence. Editable **strictly** by the owner Citizen and **only** while the report remains in `Submitted` status.

- **Access:** `Citizen` (Owner only). `WasteOfficer`, `MunicipalManager`, and `Driver` receive `403 Forbidden`.
- **Precondition:** Report `Status` must be `Submitted`. Once status transitions to `UnderReview` or later, evidence is permanently locked; attempts return `409 Conflict`.
- **Allowed Editable Fields:** `description`, `wasteType`, `latitude`, `longitude`, `addressText`.
- **Minimum Field Requirement:** At least one editable field must be provided. An entirely empty payload `{}` or payload with all fields null is rejected with `400 BadRequest`.
- **Partial Update (`addressText`) Contract:**
  - Omitted or `null`: No change to the existing address.
  - Empty string `""`: Explicit request to clear the existing address (the application service normalizes `""` to `null` before persistence). Counts as an explicit update field.
  - Non-empty string: Updates address text (maximum 500 characters).
- **Forbidden Server Fields:** Clients cannot modify `status`, `priority`, `citizenId`, `verifiedByUserId`, or `verifiedAt`.
- **Validation:** Same rules as creation for supplied fields (description 10-1000 chars, valid wasteType enum, latitude -90 to 90, longitude -180 to 180).

#### Request Body (All fields optional; at least one must be provided)
```json
{
  "description": "Updated: garbage heap now extending into street lane",
  "wasteType": "General",
  "latitude": 6.9272,
  "longitude": 79.8614,
  "addressText": "Main Street, near shelter"
}
```

#### Response `200 OK`
Updated `WasteReportDetailDto`.

---

### 3.5 `DELETE /api/v1/waste-reports/{id}` `[Planned]`
Cancels a waste report prior to review initiation. **Business cancellation — NOT a physical database deletion.**

- **Access:** `Citizen` (Owner only).
- **Precondition:** Report `Status` must be `Submitted`. If status is `UnderReview`, `Verified`, `Rejected`, or later, returns `409 Conflict`.
- **Request Body:** No request body required. No citizen-supplied cancellation reason is required.
- **Atomic Transaction:**
  1. Updates `WasteReport.Status` to `Cancelled`.
  2. Sets `WasteReport.UpdatedAt = DateTime.UtcNow`.
  3. Inserts `WasteReportStatusHistory` (`fromStatus = "Submitted"`, `toStatus = "Cancelled"`, `changedByUserId = CitizenId`, `notes = "Cancelled by citizen"`).

#### Response `200 OK`
```json
{
  "message": "Waste report cancelled successfully.",
  "status": "Cancelled"
}
```

---

### 3.6 `POST /api/v1/waste-reports/{id}/start-review` `[Planned]`
Initiates official officer review of a submitted report, locking citizen modifications. Viewing a report does **NOT** automatically initiate review.

- **Access:** `WasteOfficer` only. `MunicipalManager` is read-only in Component 1 and receives `403 Forbidden`.
- **Precondition:** Report `Status` must be `Submitted`. Reports already `UnderReview`, `Verified`, `Rejected`, or `Cancelled` return `409 Conflict`.
- **Request Body:** No request body required (no request DTO).
- **Atomic Transaction:**
  1. Updates `WasteReport.Status` to `UnderReview`.
  2. Sets `WasteReport.UpdatedAt = DateTime.UtcNow`.
  3. Inserts `WasteReportStatusHistory` (`fromStatus = "Submitted"`, `toStatus = "UnderReview"`, `changedByUserId = currentUserId`, `notes = "Officer started review"`).

#### Response `200 OK`
Updated `WasteReportDetailDto` (`status: "UnderReview"`).

---

### 3.7 `POST /api/v1/waste-reports/{id}/verify` `[Planned]`
Waste Officer confirms validity of an inspected report. **Enables report eligibility for later AI planning.**

- **Access:** `WasteOfficer` only. (`MunicipalManager` is read-only for Component 1 and receives `403 Forbidden`).
- **Precondition:** Report `Status` must be `UnderReview`. Reports in `Submitted` (review not started) or any other status return `409 Conflict`.
- **Request Body:** `{ "priority": "High" }`. `priority` is required and must be `Low`, `Medium`, `High`, or `Urgent`.
- **Atomic Transaction:**
  1. Sets `WasteReport.Status = "Verified"`.
  2. Persists the selected `WasteReport.Priority`.
  3. Sets `WasteReport.VerifiedByUserId = currentUserId`.
  4. Sets `WasteReport.VerifiedAt = DateTime.UtcNow`.
  5. Sets `WasteReport.UpdatedAt = DateTime.UtcNow`.
  6. Inserts `WasteReportStatusHistory` (`fromStatus = "UnderReview"`, `toStatus = "Verified"`, `changedByUserId = currentUserId`, `notes = null`).

#### Response `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "citizenId": "1fa85f64-5717-4562-b3fc-2c963f66afa1",
  "citizenName": "Kamal Perera",
  "description": "Large garbage heap overflowing near bus stand",
  "wasteType": "General",
  "latitude": 6.9271,
  "longitude": 79.8612,
  "addressText": "Main Street, Pettah",
  "status": "Verified",
  "priority": null,
  "verifiedByUserId": "2fa85f64-5717-4562-b3fc-2c963f66afa2",
  "verifiedByUserName": "Officer Silva",
  "verifiedAt": "2026-09-15T09:45:00Z",
  "attachments": [],
  "createdAt": "2026-09-15T08:30:00Z",
  "updatedAt": "2026-09-15T09:45:00Z"
}
```

---

### 3.8 `POST /api/v1/waste-reports/{id}/reject` `[Planned]`
Rejects an invalid, duplicate, or out-of-jurisdiction waste report.

- **Access:** `WasteOfficer` only. (`MunicipalManager` receives `403 Forbidden`).
- **Precondition:** Report `Status` must be `UnderReview`. Reports in `Submitted` (review not started) or any other status return `409 Conflict`.
- **Validation Rules:**
  - `reason`: Required, 5 to 500 characters (`RejectWasteReportRequest`).
- **Atomic Transaction:**
  1. Sets `WasteReport.Status = "Rejected"`.
  2. Sets `WasteReport.UpdatedAt = DateTime.UtcNow`.
  3. Inserts `WasteReportStatusHistory` (`fromStatus = "UnderReview"`, `toStatus = "Rejected"`, `changedByUserId = currentUserId`, `notes = request.reason`). Note: Rejection reason is stored authoritatively in history `notes`; no separate column exists on `WasteReport`.

#### Request Body (`RejectWasteReportRequest`)
```json
{
  "reason": "Duplicate report already covered by scheduled pickup route."
}
```

#### Response `200 OK`
Updated `WasteReportDetailDto` (`status: "Rejected"`).

---

### 3.9 `GET /api/v1/waste-reports/{id}/history` `[Planned]`
Retrieves chronological audit trail of all state transitions for a report.

- **Access:** `Citizen` (Owner only), `WasteOfficer`, `MunicipalManager`. `Driver` receives `403 Forbidden`.

#### Response `200 OK`
```json
[
  {
    "id": "7fa85f64-5717-4562-b3fc-2c963f66afa9",
    "wasteReportId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fromStatus": null,
    "toStatus": "Submitted",
    "changedByUserId": "1fa85f64-5717-4562-b3fc-2c963f66afa1",
    "changedByUserName": "Kamal Perera",
    "notes": "Initial report submission",
    "changedAt": "2026-09-15T08:30:00Z"
  },
  {
    "id": "8fa85f64-5717-4562-b3fc-2c963f66afa0",
    "wasteReportId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fromStatus": "Submitted",
    "toStatus": "UnderReview",
    "changedByUserId": "2fa85f64-5717-4562-b3fc-2c963f66afa2",
    "changedByUserName": "Officer Silva",
    "notes": "Officer started review",
    "changedAt": "2026-09-15T09:15:00Z"
  },
  {
    "id": "9fa85f64-5717-4562-b3fc-2c963f66afa3",
    "wasteReportId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fromStatus": "UnderReview",
    "toStatus": "Verified",
    "changedByUserId": "2fa85f64-5717-4562-b3fc-2c963f66afa2",
    "changedByUserName": "Officer Silva",
    "notes": null,
    "changedAt": "2026-09-15T09:45:00Z"
  }
]
```

---

### 3.10 `POST /api/v1/waste-reports/{id}/attachments` `[Planned]`
Uploads a photographic evidence image for a waste report using `multipart/form-data`.

- **Access:** `Citizen` (Owner only).
- **Precondition:** Report `Status` must be `Submitted`. If `UnderReview` or later, returns `409 Conflict`.
- **Content-Type:** `multipart/form-data` (form field: `file`).
- **Validation Rules & Constraints:**
  - `file`: Required, non-empty binary.
  - Allowed MIME types: `image/jpeg`, `image/png`, `image/webp`.
  - File size: Maximum 5 MB (5,242,880 bytes).
  - Maximum count: 3 attachments per report. If report already has 3 attachments, returns `400 BadRequest`.
- **Cloud Storage Architecture & Persistence:**
  - File binary is uploaded to provider-independent cloud object storage via the `IFileStorageService` abstraction.
  - PostgreSQL persists only the `StorageKey` (e.g. `waste-reports/{reportId}/{uniqueId}.ext`) and `FileType`. No binary blobs or permanent public URLs are stored in the database.
  - The returned `fileUrl` is strictly an API/presentation concern (e.g. short-lived signed URL or authorized backend stream generated by `IFileStorageService`). Knowing a `StorageKey` does not grant access. Zero cloud credentials or secrets are exposed to client applications.

#### Response `201 Created` (`ReportAttachmentDto`)
```json
{
  "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
  "wasteReportId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileUrl": "https://storage.smartwaste.local/reports/3fa85f64-5717-4562-b3fc-2c963f66afa6/4fa85f64.jpg?token=...",
  "fileType": "image/jpeg",
  "createdAt": "2026-09-15T08:31:00Z"
}
```

---

### 3.11 `DELETE /api/v1/waste-reports/{id}/attachments/{attachmentId}` `[Planned]`
Removes an uploaded photographic attachment.

- **Access:** `Citizen` (Owner only).
- **Precondition:** Report `Status` must be `Submitted`. If `UnderReview` or later, returns `409 Conflict`.
- **Side effects:** Deletes database `ReportAttachment` record and calls `IFileStorageService.DeleteAsync(storageKey)` to remove the object from managed cloud storage.

#### Response `200 OK`
```json
{
  "message": "Attachment removed successfully."
}
```

## 4. Component 2 — Waste Collection & Bin Management

### 4.0 Component 2 Architectural & Routing Conventions
Component 2 endpoints follow the authoritative ASP.NET Core Clean Architecture patterns:
- Base path prefix: `/api/v1`
- JSON casing: `camelCase`
- Route constraints: Entity ID parameters enforce Guid format (`{id:guid}`) to disambiguate literal sub-routes (e.g., `/api/v1/bins/public` vs `/api/v1/bins/{id:guid}`).
- Role-based authorization: Enforced via `[Authorize(Roles = ...)]` using `AppRoles` constants (`Citizen`, `WasteOfficer`, `Driver`, `MunicipalManager`).
- Standard envelope: List endpoints return `PagedResult<T>` (`items`, `page`, `pageSize`, `totalCount`, `totalPages`).
- Error reporting: RFC 7807 `ProblemDetails` (`400 BadRequest`, `401 Unauthorized`, `403 Forbidden`, `404 NotFound`, `409 Conflict`).

---

### 4.1 Public Bin Discovery Endpoints (Citizen)

#### 4.1.1 `GET /api/v1/bins/public` `[Planned]`
Retrieves a paginated list of registered public roadside bins with computed public availability, coordinates, and optional proximity filtering.

- **Access:** `Citizen` (Authenticated).
- **Query Parameters:**
  - `latitude` (double, optional): User's current latitude (-90.0 to 90.0). Required if `radiusKm` is provided.
  - `longitude` (double, optional): User's current longitude (-180.0 to 180.0). Required if `radiusKm` is provided.
  - `radiusKm` (double, optional, min: 0.1, max: 50.0): Search radius in kilometers.
  - `wasteType` (string, optional): Filter by accepted waste type (`General`, `Organic`, `Recyclable`, `Hazardous`, `Bulky`, `Other`).
  - `page` (int, default: 1, min: 1).
  - `pageSize` (int, default: 20, min: 1, max: 50).
- **Public Data Minimization Guard:** Excludes internal officer notes, `RecordedByUserId`, maintenance logs, and driver information.
- **Response `200 OK`:** `PagedResult<PublicWasteBinDto>`
```json
{
  "items": [
    {
      "id": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
      "binCode": "BIN-COL-0042",
      "latitude": 6.9271,
      "longitude": 79.8612,
      "addressText": "Main Street, Pettah (Near Central Bus Stand)",
      "capacityLiters": 660,
      "acceptedWasteTypes": ["General", "Recyclable"],
      "publicAvailability": "Usable",
      "lastObservedAt": "2026-09-21T08:30:00Z",
      "distanceMeters": 350.5
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```
- **Error Responses:**
  - `400 BadRequest`: Invalid query parameters or invalid coordinate range.
  - `401 Unauthorized`: Missing or expired JWT.
  - `403 Forbidden`: Non-Citizen actor.

#### 4.1.2 `GET /api/v1/bins/public/{id:guid}` `[Planned]`
Retrieves detailed public operational information for a specific roadside bin.

- **Access:** `Citizen` (Authenticated).
- **Response `200 OK`:** `PublicWasteBinDetailDto`
```json
{
  "id": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
  "binCode": "BIN-COL-0042",
  "latitude": 6.9271,
  "longitude": 79.8612,
  "addressText": "Main Street, Pettah (Near Central Bus Stand)",
  "capacityLiters": 660,
  "acceptedWasteTypes": ["General", "Recyclable"],
  "publicAvailability": "Usable",
  "lastObservedAt": "2026-09-21T08:30:00Z",
  "isCollectionScheduled": false
}
```
- **Error Responses:**
  - `404 NotFound`: Bin not found.
  - `403 Forbidden`: Non-Citizen actor.

---

### 4.2 Internal Bin Management Endpoints (Staff)

#### 4.2.1 `GET /api/v1/bins` `[Planned]`
Lists registered bins with full administrative filters for WasteOfficers and MunicipalManagers.

- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Query Parameters:**
  - `status` (string, optional): Filter by `Active`, `OutOfService`, `Retired`.
  - `wasteType` (string, optional): Filter by accepted waste type.
  - `condition` (string, optional): Filter by latest condition (`Good`, `Damaged`, `Blocked`, `Missing`).
  - `minFillLevel` (int, optional): Minimum fill level percent (`0`, `25`, `50`, `75`, `100`).
  - `search` (string, optional): Search term matching `binCode` or `addressText`.
  - `page` (int, default: 1, min: 1).
  - `pageSize` (int, default: 20, min: 1, max: 100).
- **Response `200 OK`:** `PagedResult<WasteBinSummaryDto>`
```json
{
  "items": [
    {
      "id": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
      "binCode": "BIN-COL-0042",
      "latitude": 6.9271,
      "longitude": 79.8612,
      "addressText": "Main Street, Pettah",
      "capacityLiters": 660,
      "administrativeStatus": "Active",
      "acceptedWasteTypes": ["General", "Recyclable"],
      "collectionWeekdays": [1, 4],
      "latestFillLevelPercent": 75,
      "latestCondition": "Good",
      "latestObservationAt": "2026-09-21T08:30:00Z",
      "hasActiveTask": false,
      "lastCollectedAt": "2026-09-18T14:20:00Z",
      "createdAt": "2026-09-01T10:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

#### 4.2.2 `GET /api/v1/bins/{id:guid}` `[Planned]`
Retrieves full internal details of a specific roadside bin.

- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** `WasteBinDetailDto`
```json
{
  "id": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
  "binCode": "BIN-COL-0042",
  "latitude": 6.9271,
  "longitude": 79.8612,
  "addressText": "Main Street, Pettah",
  "capacityLiters": 660,
  "administrativeStatus": "Active",
  "acceptedWasteTypes": ["General", "Recyclable"],
  "collectionWeekdays": [1, 4],
  "lastCollectedAt": "2026-09-18T14:20:00Z",
  "latestObservation": {
    "id": "d2a839e1-1b3c-4d5e-6f70-8a9b0c1d2e3f",
    "fillLevelPercent": 75,
    "condition": "Good",
    "notes": "Routine morning check",
    "recordedByUserId": "2fa85f64-5717-4562-b3fc-2c963f66afa2",
    "recordedByUserName": "Officer Silva",
    "recordedAt": "2026-09-21T08:30:00Z"
  },
  "hasActiveTask": false,
  "activeTaskId": null,
  "createdAt": "2026-09-01T10:00:00Z",
  "updatedAt": "2026-09-10T11:00:00Z"
}
```

#### 4.2.3 `POST /api/v1/bins` `[Planned]`
Registers a new municipal public roadside bin.

- **Access:** `WasteOfficer` only.
- **Request Body (`CreateWasteBinRequest`):**
```json
{
  "binCode": "BIN-COL-0043",
  "latitude": 6.9312,
  "longitude": 79.8504,
  "addressText": "Galle Face Green Promenade",
  "capacityLiters": 1100,
  "acceptedWasteTypes": ["General", "Organic"],
  "collectionWeekdays": [1, 3, 5]
}
```
- **Validation Rules:**
  - `binCode`: Required, 3 to 50 characters, alphanumeric with hyphens. Must be unique.
  - `latitude`: Required, -90.0 to 90.0.
  - `longitude`: Required, -180.0 to 180.0.
  - `capacityLiters`: Required, > 0.
  - `acceptedWasteTypes`: Required, non-empty list of valid `WasteType` strings.
  - `collectionWeekdays`: List of unique integers 1–7 (ISO 8601: 1=Mon .. 7=Sun). Empty list allowed.
- **Response `201 Created`:** Returns `WasteBinDetailDto` with `Location` header.
- **Error Responses:**
  - `400 BadRequest`: Validation failure (empty waste types, invalid coordinates).
  - `409 Conflict`: `binCode` already in use (`IX_WasteBins_BinCode`).

#### 4.2.4 `PUT /api/v1/bins/{id:guid}` `[Planned]`
Updates operational metadata of a registered bin.

- **Access:** `WasteOfficer` only.
- **Request Body (`UpdateWasteBinRequest`):**
```json
{
  "latitude": 6.9315,
  "longitude": 79.8506,
  "addressText": "Galle Face Green Promenade (North Pavilion)",
  "capacityLiters": 1100,
  "acceptedWasteTypes": ["General", "Organic", "Recyclable"],
  "collectionWeekdays": [1, 3, 5]
}
```
- **Response `200 OK`:** Updated `WasteBinDetailDto`.
- **Error Responses:**
  - `400 BadRequest`: Validation failure.
  - `404 NotFound`: Bin does not exist.
  - `409 Conflict`: Bin is in `Retired` status.

#### 4.2.5 `POST /api/v1/bins/{id:guid}/deactivate` `[Planned]`
Changes the administrative status of a bin to `OutOfService` (temporary repair/relocation) or `Retired` (permanent decommissioning).

- **Access:** `WasteOfficer` only.
- **Request Body (`DeactivateWasteBinRequest`):**
```json
{
  "targetStatus": "OutOfService",
  "reason": "Damaged hinge undergoing depot repair."
}
```
- **Validation Rules:**
  - `targetStatus`: Required, must be `OutOfService` or `Retired`.
  - `reason`: Optional, max 500 characters.
- **Business Preconditions:**
  - Cannot deactivate a bin that is already `Retired` (409 Conflict).
  - If target has an active task (`Scheduled`, `Assigned`, `InProgress`), the officer is warned or deactivation cancels unstarted tasks.
- **Response `200 OK`:** Updated `WasteBinDetailDto`.

#### 4.2.6 `POST /api/v1/bins/{id:guid}/observations` `[Planned]`
Records an append-only manual field observation for a roadside bin.

- **Access:** `WasteOfficer` (now) and `Driver` (in future C3).
- **Request Body (`RecordBinObservationRequest`):**
```json
{
  "fillLevelPercent": 100,
  "condition": "Good",
  "notes": "Severe overflow observed due to weekend market crowd."
}
```
- **Validation Rules:**
  - `fillLevelPercent`: Required integer, must be one of `0`, `25`, `50`, `75`, `100`.
  - `condition`: Required string, must be one of `Good`, `Damaged`, `Blocked`, `Missing`.
  - `notes`: Optional string, max 500 characters.
- **Business Behavior:**
  - Inserts immutable `BinObservation` record with server-generated `RecordedAt = DateTime.UtcNow` and `RecordedByUserId = actorUserId`.
  - If `fillLevelPercent == 100` or `condition == 'Blocked'`, the bin is flagged for collection in `GET /api/v1/collection-needs`.
  - If `condition ∈ {'Damaged', 'Missing'}`, triggers a maintenance alert in staff views.
  - Does NOT automatically schedule a `CollectionTask`.
- **Response `201 Created`:** `BinObservationDto`
```json
{
  "id": "e3b940f2-2c4d-5e6f-7a8b-9c0d1e2f3a4b",
  "wasteBinId": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
  "fillLevelPercent": 100,
  "condition": "Good",
  "notes": "Severe overflow observed due to weekend market crowd.",
  "recordedByUserId": "2fa85f64-5717-4562-b3fc-2c963f66afa2",
  "recordedByUserName": "Officer Silva",
  "recordedAt": "2026-09-21T09:15:00Z"
}
```

#### 4.2.7 `GET /api/v1/bins/{id:guid}/observations` `[Planned]`
Retrieves chronological observation history for a bin.

- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Query Parameters:** `page` (default 1), `pageSize` (default 20, max 100).
- **Response `200 OK`:** `PagedResult<BinObservationDto>`.

---

### 4.3 Collection Needs Queue Endpoints (Staff)

#### 4.3.1 `GET /api/v1/collection-needs` `[Planned]`
Retrieves the unified derived read queue of outstanding collection needs across the municipality.

- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Query Parameters:**
  - `targetType` (string, optional): `"Report"` or `"Bin"`.
  - `collectionReason` (string, optional): `"VerifiedReport"`, `"FullOrBlockedBin"`, `"RoutineCollection"`.
  - `wasteType` (string, optional): Filter by waste type.
  - `search` (string, optional): Search text in address or description.
  - `page` (int, default: 1, min: 1).
  - `pageSize` (int, default: 20, min: 1, max: 100).
- **Response `200 OK`:** `PagedResult<CollectionNeedItemDto>`
```json
{
  "items": [
    {
      "id": "5ac553cf-967f-4115-91f5-7f947b55802c",
      "targetType": "Report",
      "collectionReason": "VerifiedReport",
      "title": "Verified Report: Main Street, Pettah",
      "latitude": 6.9351,
      "longitude": 79.8512,
      "addressText": "Main Street, Pettah",
      "wasteTypes": ["General"],
      "urgency": "High",
      "triggerDate": "2026-09-20T14:30:00Z",
      "attachmentCount": 2,
      "binDetails": null
    },
    {
      "id": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
      "targetType": "Bin",
      "collectionReason": "FullOrBlockedBin",
      "title": "BIN-COL-0042 (100% Full)",
      "latitude": 6.9271,
      "longitude": 79.8612,
      "addressText": "Main Street, Pettah (Near Central Bus Stand)",
      "wasteTypes": ["General", "Recyclable"],
      "urgency": "High",
      "triggerDate": "2026-09-21T08:30:00Z",
      "attachmentCount": 0,
      "binDetails": {
        "binCode": "BIN-COL-0042",
        "capacityLiters": 660,
        "latestFillLevelPercent": 100,
        "latestCondition": "Good",
        "observationAgeHours": 1.2
      }
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 2,
  "totalPages": 1
}
```

---

### 4.4 Collection Tasks Endpoints (Staff)

#### 4.4.1 `GET /api/v1/collection-tasks` `[Planned]`
Lists collection tasks with operational filters.

- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Query Parameters:**
  - `status` (string, optional): `Scheduled`, `Assigned`, `InProgress`, `Completed`, `Failed`, `Cancelled`.
  - `targetType` (string, optional): `"Report"` or `"Bin"`.
  - `collectionReason` (string, optional): Filter by reason.
  - `dateFrom` (DateTime, optional, UTC).
  - `dateTo` (DateTime, optional, UTC).
  - `page` (int, default: 1).
  - `pageSize` (int, default: 20, max: 100).
- **Response `200 OK`:** `PagedResult<CollectionTaskSummaryDto>`
```json
{
  "items": [
    {
      "id": "f4c051a3-3d5e-6f7a-8b9c-0d1e2f3a4b5c",
      "taskCode": "TSK-20260921-0012",
      "targetType": "Bin",
      "wasteReportId": null,
      "wasteBinId": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
      "targetReference": "BIN-COL-0042",
      "collectionReason": "FullOrBlockedBin",
      "status": "Scheduled",
      "scheduledAt": "2026-09-21T14:00:00Z",
      "creationMethod": "Manual",
      "createdByUserId": "2fa85f64-5717-4562-b3fc-2c963f66afa2",
      "createdByUserName": "Officer Silva",
      "createdAt": "2026-09-21T09:30:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

#### 4.4.2 `GET /api/v1/collection-tasks/{id:guid}` `[Planned]`
Retrieves detailed collection task information including target details, location, status history, and reschedule history.

- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** `CollectionTaskDetailDto`
```json
{
  "id": "f4c051a3-3d5e-6f7a-8b9c-0d1e2f3a4b5c",
  "taskCode": "TSK-20260921-0012",
  "targetType": "Bin",
  "wasteReportId": null,
  "wasteBinId": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
  "targetSummary": {
    "identifier": "BIN-COL-0042",
    "latitude": 6.9271,
    "longitude": 79.8612,
    "addressText": "Main Street, Pettah",
    "capacityLiters": 660,
    "wasteTypes": ["General", "Recyclable"],
    "latestFillLevelPercent": 100
  },
  "collectionReason": "FullOrBlockedBin",
  "status": "Scheduled",
  "scheduledAt": "2026-09-21T14:00:00Z",
  "handlingNotes": "Compactor vehicle required.",
  "schedulingReason": null,
  "createdByUserId": "2fa85f64-5717-4562-b3fc-2c963f66afa2",
  "createdByUserName": "Officer Silva",
  "creationMethod": "Manual",
  "createdAt": "2026-09-21T09:30:00Z",
  "updatedAt": null,
  "statusHistory": [
    {
      "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
      "fromStatus": null,
      "toStatus": "Scheduled",
      "changedByUserId": "2fa85f64-5717-4562-b3fc-2c963f66afa2",
      "changedByUserName": "Officer Silva",
      "notes": "Task manually created",
      "changedAt": "2026-09-21T09:30:00Z"
    }
  ],
  "scheduleHistory": []
}
```

#### 4.4.3 `POST /api/v1/collection-tasks/manual` `[Planned]`
Authoritative manual task scheduling command executed by a WasteOfficer.

- **Access:** `WasteOfficer` only.
- **Request Body Examples (`CreateManualCollectionTaskRequest`):**

*Example A — Verified Waste Report Target:*
```json
{
  "wasteReportId": "5ac553cf-967f-4115-91f5-7f947b55802c",
  "wasteBinId": null,
  "collectionReason": "VerifiedReport",
  "scheduledAt": "2026-09-21T15:00:00Z",
  "handlingNotes": "Bulky compaction required.",
  "schedulingReason": null
}
```

*Example B — Roadside Bin Target (Officer Discretion):*
```json
{
  "wasteReportId": null,
  "wasteBinId": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
  "collectionReason": "OfficerDiscretion",
  "scheduledAt": "2026-09-21T16:30:00Z",
  "handlingNotes": "Use secondary access gate behind market.",
  "schedulingReason": "Anticipated overflow due to weekend cultural festival along Main Street."
}
```

- **Validation & Business Preconditions:**
  - Target XOR: Exactly one of `wasteReportId` or `wasteBinId` must be provided.
  - Reason Consistency:
    - If `wasteReportId` provided: `collectionReason` must be `VerifiedReport`. Report must have status `Verified`. Report-targeted tasks cannot use `OfficerDiscretion`, `FullOrBlockedBin`, or `RoutineCollection`.
    - If `wasteBinId` provided: `collectionReason` must be `FullOrBlockedBin`, `RoutineCollection`, or `OfficerDiscretion`. Bin must have status `Active`.
  - Officer Discretion Justification (`schedulingReason`):
    - When `collectionReason == 'OfficerDiscretion'`: `schedulingReason` is strictly mandatory (non-empty, 5–500 chars).
    - `handlingNotes` is an optional operational field containing driver instructions; it must NOT substitute for `schedulingReason`.
  - Planned Timestamp: `scheduledAt` must be in UTC and >= current server UTC time.
  - Active Task Guard: Target must not already have an active task (`Scheduled`, `Assigned`, `InProgress`). Returns `409 Conflict`.
- **Atomic Side Effects:**
  - Generates unique `taskCode` (`TSK-YYYYMMDD-XXXX`).
  - Sets `CreationMethod = Manual` and `CreatedByUserId = actorUserId`.
  - Inserts initial `CollectionTaskStatusHistory` (`FromStatus = null`, `ToStatus = Scheduled`).
  - If targeting a `WasteReport`:
    - Updates `WasteReport.Status = Scheduled` and `WasteReport.UpdatedAt = UtcNow`.
    - Inserts `WasteReportStatusHistory` adhering strictly to existing C1 entity fields:
      - `wasteReportId = report.Id`
      - `fromStatus = "Verified"`
      - `toStatus = "Scheduled"`
      - `changedByUserId = actorUserId` (authenticated WasteOfficer ID)
      - `notes = $"Collection task {taskCode} scheduled"` (stored in `notes`, not `reason`)
      - `changedAt = UtcNow`
  - Commits all operations in one atomic database transaction.
- **Failed & Replacement Task Boundaries:**
  - Initial scheduling requires `WasteReport.Status == Verified`.
  - This endpoint CANNOT be used to re-schedule or replace a task for a report currently in `Scheduled` or `InProgress`.
  - Reopening and replacement-task scheduling for failed or cancelled report-targeted tasks is a **RESERVED FUTURE C3/C1 INTEGRATION DECISION**; C2 introduces no reverse `WasteReport` status transitions.
  - Bin-targeted failed task replacement requires explicit WasteOfficer review, which is an **UNRESOLVED FUTURE OPERATION** (mechanism to formally persist and enforce this review is deferred).
  - No operational report-task cancellation endpoint is exposed in C2 to prevent leaving linked reports stranded in `Scheduled`.
- **Response `201 Created`:** `CollectionTaskDetailDto` with `Location` header.
- **Error Responses:**
  - `400 BadRequest`: Validation failure (invalid XOR target, date in the past, missing or empty `schedulingReason` for `OfficerDiscretion`, invalid reason combination).
  - `409 Conflict`: Target not in required status (`WasteReport` not `Verified`, `WasteBin` not `Active`) OR duplicate active task already exists.

#### 4.4.4 `POST /api/v1/collection-tasks/{id:guid}/reschedule` `[Planned]`
Reschedules an unstarted collection task to a new planned execution time.

- **Access:** `WasteOfficer` only.
- **Request Body (`RescheduleCollectionTaskRequest`):**
```json
{
  "newScheduledAt": "2026-09-21T18:00:00Z",
  "reason": "Depot vehicle maintenance delayed morning departure."
}
```
- **Validation & Business Preconditions:**
  - Task Status: Task must be in `Status == Scheduled`. If `Assigned`, `InProgress`, `Completed`, `Failed`, or `Cancelled`, returns `409 Conflict`.
  - New Timestamp: `newScheduledAt` must be in UTC and > current server UTC time.
  - Reason: Mandatory, 5 to 500 characters.
- **Atomic Side Effects:**
  - Records old `ScheduledAt` and new `ScheduledAt` in `CollectionTaskScheduleHistory` along with `RescheduledByUserId = actorUserId`, `Reason`, and `RescheduledAt = UtcNow`.
  - Updates `CollectionTask.ScheduledAt = newScheduledAt` and `CollectionTask.UpdatedAt = UtcNow`.
  - Task status remains `Scheduled` (no `CollectionTaskStatusHistory` record inserted).
- **Response `200 OK`:** Updated `CollectionTaskDetailDto`.
- **Error Responses:**
  - `400 BadRequest`: Validation failure (date in past, missing reason).
  - `404 NotFound`: Task not found.
  - `409 Conflict`: Task is already assigned or in-progress.

#### 4.4.5 `GET /api/v1/collection-tasks/{id:guid}/history` `[Planned]`
Retrieves the complete audit trail of status transitions and reschedule events for a collection task.

- **Access:** `WasteOfficer`, `MunicipalManager`.
- **Response `200 OK`:** `CollectionTaskAuditTrailDto`
```json
{
  "collectionTaskId": "f4c051a3-3d5e-6f7a-8b9c-0d1e2f3a4b5c",
  "taskCode": "TSK-20260921-0012",
  "statusHistory": [
    {
      "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
      "fromStatus": null,
      "toStatus": "Scheduled",
      "changedByUserId": "2fa85f64-5717-4562-b3fc-2c963f66afa2",
      "changedByUserName": "Officer Silva",
      "notes": "Task manually created",
      "changedAt": "2026-09-21T09:30:00Z"
    }
  ],
  "scheduleHistory": [
    {
      "id": "b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e",
      "previousScheduledAt": "2026-09-21T14:00:00Z",
      "newScheduledAt": "2026-09-21T18:00:00Z",
      "reason": "Depot vehicle maintenance delayed morning departure.",
      "rescheduledByUserId": "2fa85f64-5717-4562-b3fc-2c963f66afa2",
      "rescheduledByUserName": "Officer Silva",
      "rescheduledAt": "2026-09-21T11:00:00Z"
    }
  ]
}
```

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
      "action": "Execute route planning/optimization for waypoints",
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

---

## 8. Internal Service & AI Tool Endpoints

Internal endpoints are exposed strictly for private service-to-service communication (such as the Python AI microservice). Public web and mobile clients cannot access these endpoints.

### 8.1 `GET /api/v1/internal/ai-tools/waste-reports/verified` `[Implemented]`
Retrieves a strictly read-only, paginated, safe projection of WasteReports whose authoritative status is `Verified`.

- **Access:** Internal Service only (`X-Internal-Service-Key` header validated against configured server secret). Callers cannot authenticate with user JWT Bearer tokens.
- **Protocol:** HTTP GET, strictly read-only (zero database side effects, no status mutations, no priority assignments, no audit log creations).
- **Status Filter:** Server-enforced `Status == Verified`. Callers cannot request unverified, submitted, under-review, or rejected reports.
- **Data Minimization Contract:**
  - Citizen personal identifiable information (`citizenId`, `fullName`, `email`, `phoneNumber`) is excluded.
  - Internal WasteOfficer identifiers are excluded.
  - Private Supabase Storage keys and signed image URLs are excluded.
  - Internal status transition notes and history records are excluded.
  - Exposes only operational metadata required for planning: `id`, `description`, `wasteType`, `latitude`, `longitude`, `addressText`, `status`, `createdAt`, `verifiedAt`, and `attachmentCount`.
- **Query Parameters:**
  - `page` (integer, default: `1`, minimum: `1`)
  - `pageSize` (integer, default: `20`, range: `1` to `50`)
- **Sorting:** Deterministic descending order by `CreatedAt DESC`, `Id DESC`.
- **Request Headers:**
  - `X-Internal-Service-Key`: `<secret_key>`
- **Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "description": "Accumulation of organic waste near market entrance",
      "wasteType": "Organic",
      "latitude": 6.9271,
      "longitude": 79.8612,
      "addressText": "Main Street, Colombo",
      "status": "Verified",
      "createdAt": "2026-09-17T10:00:00Z",
      "verifiedAt": "2026-09-17T10:30:00Z",
      "attachmentCount": 2
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```
- **Error Responses:**
  - `401 Unauthorized`: Missing or invalid `X-Internal-Service-Key` header (`ProblemDetails`).
  - `400 BadRequest`: Query parameter validation failure (`page < 1` or `pageSize < 1` or `pageSize > 50`).

---

### 8.2 `GET /api/v1/internal/ai-tools/collection-needs` `[Proposed for C2 AI Step]`
Retrieves a strictly read-only, paginated, safe projection of the municipality's outstanding collection needs (derived from Sources A, B, and C with active task suppression).

- **Access:** Internal Service only (`X-Internal-Service-Key` header validated against configured server secret). Callers cannot authenticate with user JWT Bearer tokens.
- **Protocol:** HTTP GET, strictly read-only (zero database side effects, no status mutations, no tasks created).
- **Purpose:** Exposes candidate collection needs to the Component 2 Collection Planning Agent / LangGraph microservice to formulate non-authoritative collection route/batch proposals.
- **Data Minimization Contract:**
  - Citizen personal identifiable information (`fullName`, `email`, `phoneNumber`) is completely omitted.
  - Internal staff user IDs and transition notes are omitted.
  - Supabase image storage keys are omitted.
  - Exposes only geographic coordinates, target identification (`targetType`, `wasteReportId` or `wasteBinId`, `binCode`), accepted `wasteTypes`, `collectionReason`, calculated `urgency`, `triggerDate`, and essential bin capacity/observation telemetry.
- **Query Parameters:**
  - `targetType` (string, optional): `"Report"` or `"Bin"`.
  - `collectionReason` (string, optional): `"VerifiedReport"`, `"FullOrBlockedBin"`, `"RoutineCollection"`.
  - `targetDate` (string, optional): ISO date (`YYYY-MM-DD`, e.g., `"2026-09-21"`) to evaluate routine collection weekday schedules in local municipality time. Defaults to municipality today.
  - `page` (integer, default: `1`, minimum: `1`).
  - `pageSize` (integer, default: `20`, range: `1` to `50`).
- **Sorting:** Deterministic order by calculated urgency (`Urgent`/`High` before `Routine`), then `triggerDate ASC`.
- **Request Headers:**
  - `X-Internal-Service-Key`: `<secret_key>`
- **Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "5ac553cf-967f-4115-91f5-7f947b55802c",
      "targetType": "Report",
      "wasteReportId": "5ac553cf-967f-4115-91f5-7f947b55802c",
      "wasteBinId": null,
      "collectionReason": "VerifiedReport",
      "latitude": 6.9351,
      "longitude": 79.8512,
      "addressText": "Main Street, Pettah",
      "wasteTypes": ["General"],
      "urgency": "High",
      "triggerDate": "2026-09-20T14:30:00Z",
      "binTelemetry": null
    },
    {
      "id": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
      "targetType": "Bin",
      "wasteReportId": null,
      "wasteBinId": "c1f728c4-e4c1-424a-8d38-9cfb2e652a91",
      "collectionReason": "FullOrBlockedBin",
      "latitude": 6.9271,
      "longitude": 79.8612,
      "addressText": "Main Street, Pettah (Near Central Bus Stand)",
      "wasteTypes": ["General", "Recyclable"],
      "urgency": "High",
      "triggerDate": "2026-09-21T08:30:00Z",
      "binTelemetry": {
        "binCode": "BIN-COL-0042",
        "capacityLiters": 660,
        "latestFillLevelPercent": 100,
        "latestCondition": "Good",
        "observationAgeHours": 1.2
      }
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 2,
  "totalPages": 1
}
```
- **Error Responses:**
  - `401 Unauthorized`: Missing or invalid `X-Internal-Service-Key` header (`ProblemDetails`).
  - `400 BadRequest`: Query parameter validation failure (`page < 1` or `pageSize < 1` or `pageSize > 50`).


