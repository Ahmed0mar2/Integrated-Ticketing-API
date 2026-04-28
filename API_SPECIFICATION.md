# Rehla API Specification

Frontend integration guide for Flutter and Angular teams.

---

## Base Information

- **Base Route Prefix:** `/api`
- **Authentication Scheme:** `JWT Bearer`
- **Primary Wrapper:** `ApiResponse<T>` / `ApiResponse`
- **Validation:** FluentValidation + validation filter
- **Unhandled exceptions:** RFC `ProblemDetails`

### Standard Success Wrapper

```json
{
  "success": true,
  "message": "Operation successful",
  "data": {},
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### Standard Error Wrapper

```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": ["Error message"],
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### Date & Time Contract

- **UTC timestamps (absolute instants, serialized with `Z`):**
  - Wrapper `timestamp`
  - Booking/cart hold fields such as `holdExpiresAt`
  - Booking audit fields such as `bookingDate`
  - Occurrence seat snapshot field `generatedAtUtc`
- **Schedule-local timestamps (timetable wall-clock, no timezone suffix):**
  - `boardingTime`, `dropoffTime`
  - Occurrence-level `departureTime`, `arrivalTime`
- **Route stop clock values:**
  - `routeStops[].arrivalTime` / `routeStops[].departureTime` are clock-only values like `HH:mm:ss`.
- **Date window logic:**
  - `travelDate` validation and occurrence generation use the schedule-local date boundary (Egypt/Cairo schedule timezone), not UTC day rollover.

---

# 1. Authentication API

Base route: `/api/Auth`

## 1.1 Register User

### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Auth/register`
- **Business Use Case:** Creates identity + domain user and returns access/refresh tokens.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
```json
{
  "email": "user@example.com",
  "password": "Password123",
  "confirmPassword": "Password123",
  "phoneNumber": "+201234567890",
  "firstName": "Ahmed",
  "lastName": "Hassan",
  "familyName": "Mohamed",
  "gender": 1,
  "dateOfBirth": "1995-05-15",
  "nationalIdNumber": "29805151234567",
  "countryCode": "EG"
}
```

### Request Field Reference
| Field            | Type   | Required | Notes                    |
| ---------------- | ------ | -------- | ------------------------ |
| email            | string | Yes      | Valid email, max 255     |
| password         | string | Yes      | Min 8, upper/lower/digit |
| confirmPassword  | string | Yes      | Must match password      |
| phoneNumber      | string | Yes      | E.164 format             |
| firstName        | string | Yes      | Max 100                  |
| lastName         | string | Yes      | Max 100                  |
| familyName       | string | Yes      | Max 100                  |
| gender           | int    | Yes      | 1=Male,2=Female,3=Other  |
| dateOfBirth      | date   | Yes      | At least 16 years old    |
| nationalIdNumber | string | No       | 14 digits if provided    |
| countryCode      | string | Yes      | 2 chars, must exist      |

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "base64-refresh-token",
    "expiresAt": "2026-03-06T13:00:00Z",
    "user": {
      "userId": 15,
      "email": "user@example.com",
      "fullName": "Ahmed Mohamed Hassan",
      "phoneNumber": "+201234567890",
      "gender": "Male",
      "countryCode": "EG",
      "countryName": "Egypt",
      "profilePictureUrl": null,
      "roles": []
    }
  },
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

## 1.2 Login

### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Auth/login`
- **Business Use Case:** Authenticates user and issues tokens.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
```json
{
  "email": "user@example.com",
  "password": "Password123",
  "deviceInfo": "Android"
}
```

### Request Field Reference
| Field      | Type   | Required | Notes                    |
| ---------- | ------ | -------- | ------------------------ |
| email      | string | Yes      | Valid email              |
| password   | string | Yes      | Non-empty                |
| deviceInfo | string | No       | Optional device metadata |

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "base64-refresh-token",
    "expiresAt": "2026-03-06T13:00:00Z",
    "user": {
      "userId": 15,
      "email": "user@example.com",
      "fullName": "Ahmed Mohamed Hassan",
      "phoneNumber": "+201234567890",
      "gender": "Male",
      "countryCode": "EG",
      "countryName": "Egypt",
      "profilePictureUrl": null,
      "roles": []
    }
  },
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

## 1.3 Refresh Access Token

### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Auth/refresh`
- **Business Use Case:** Rotates refresh token and returns new access token.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
```json
{ "refreshToken": "<token>" }
```

### Request Field Reference
| Field        | Type   | Required | Notes                |
| ------------ | ------ | -------- | -------------------- |
| refreshToken | string | Yes      | Must be active token |

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "new-refresh-token",
    "expiresAt": "2026-03-06T13:00:00Z",
    "user": { "userId": 15, "email": "user@example.com", "fullName": "Ahmed Mohamed Hassan", "phoneNumber": "+201234567890", "gender": "Male", "countryCode": "EG", "countryName": "Egypt", "profilePictureUrl": null, "roles": [] }
  },
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

## 1.4 Revoke Specific Refresh Token

### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Auth/revoke`
- **Business Use Case:** Logs out one session/device by revoking refresh token.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
```json
{ "refreshToken": "<token>" }
```

### Request Field Reference
| Field        | Type   | Required | Notes    |
| ------------ | ------ | -------- | -------- |
| refreshToken | string | Yes      | Required |

### Response Example (200 OK)
```json
{ "success": true, "message": "Token revoked successfully", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 1.5 Revoke All User Tokens

### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Auth/revoke-all`
- **Business Use Case:** Logs out user from all devices.

### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** User/Admin/Partner (authenticated)

### Request Payload
No request body.

### Response Example (200 OK)
```json
{ "success": true, "message": "Revoked 3 token(s)", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 1.6 Get Current Authenticated User

### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/Auth/me`
- **Business Use Case:** Returns authenticated claim snapshot.

### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Authenticated user

### Request Payload
No request body.

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Operation successful",
  "data": {
    "userId": "15",
    "email": "user@example.com",
    "name": "Ahmed Hassan",
    "claims": [{ "type": "domain_user_id", "value": "15" }]
  },
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

## 1.7 Send Verification Email

### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Auth/send-verification-email`
- **Business Use Case:** Sends email verification link.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
```json
{ "email": "user@example.com" }
```

### Request Field Reference
| Field | Type   | Required | Notes              |
| ----- | ------ | -------- | ------------------ |
| email | string | Yes      | Valid email format |

### Response Example (200 OK)
```json
{ "success": true, "message": "Verification email sent successfully", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

### Notes
- Returns 400 if the user does not exist or the email is already verified.

## 1.8 Verify Email

### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Auth/verify-email`
- **Business Use Case:** Confirms account email with token.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
```json
{ "userId": "7", "token": "<token>" }
```

### Request Field Reference
| Field  | Type   | Required | Notes                          |
| ------ | ------ | -------- | ------------------------------ |
| userId | string | Yes      | Identity user id (string form) |
| token  | string | Yes      | Email confirmation token       |

### Response Example (200 OK)
```json
{ "success": true, "message": "Email verified successfully", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

### Notes
- `userId` must be a numeric identity user ID. Invalid IDs or already verified emails return 400.

## 1.9 Forgot Password

### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Auth/forgot-password`
- **Business Use Case:** Sends reset link (anti-enumeration friendly response).

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
```json
{ "email": "user@example.com" }
```

### Request Field Reference
| Field | Type   | Required | Notes       |
| ----- | ------ | -------- | ----------- |
| email | string | Yes      | Valid email |

### Response Example (200 OK)
```json
{ "success": true, "message": "If your email is registered, you will receive a password reset link", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 1.10 Reset Password

### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Auth/reset-password`
- **Business Use Case:** Resets password by email+token.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
```json
{
  "email": "user@example.com",
  "token": "<token>",
  "newPassword": "NewPassword123",
  "confirmPassword": "NewPassword123"
}
```

### Request Field Reference
| Field           | Type   | Required | Notes                    |
| --------------- | ------ | -------- | ------------------------ |
| email           | string | Yes      | Valid email              |
| token           | string | Yes      | Password reset token     |
| newPassword     | string | Yes      | Min 8, upper/lower/digit |
| confirmPassword | string | Yes      | Must match newPassword   |

### Response Example (200 OK)
```json
{ "success": true, "message": "Password reset successfully", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 1.11 Change Password

### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Auth/change-password`
- **Business Use Case:** Changes current authenticated user's password.

### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Authenticated user

### Request Payload
```json
{
  "currentPassword": "OldPassword123",
  "newPassword": "NewPassword123",
  "confirmPassword": "NewPassword123"
}
```

### Request Field Reference
| Field           | Type   | Required | Notes                    |
| --------------- | ------ | -------- | ------------------------ |
| currentPassword | string | Yes      | Current user password    |
| newPassword     | string | Yes      | Min 8, upper/lower/digit |
| confirmPassword | string | Yes      | Must match newPassword   |

### Response Example (200 OK)
```json
{ "success": true, "message": "Password changed successfully", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

---

# 2. Countries API

Base route: `/api/Countries`

## 2.1 Get Countries

### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/Countries`
- **Business Use Case:** Returns countries for registration and dropdowns.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
No request body.

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Operation successful",
  "data": [
    { "countryCode": "EG", "countryName": "Egypt", "nationalityName": "Egyptian", "phoneCode": "+20", "allowsTrainBooking": true }
  ],
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

---

# 3. Data Seeder API

Base route: `/api/Seed`

Admin authorization is currently disabled in controller code (the `[Authorize]` attribute is commented). Treat these endpoints as internal-only.

## 3.1 Initialize Identity
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Seed/init-identity`
- **Business Use Case:** Seeds default roles + admin user.
### Authentication / Authorization
- **JWT Required:** No (currently)
- **Role Required:** None
### Request Payload
No request body.
### Response Example (200 OK)
```json
{ "success": true, "message": "Roles and Admin credentials seeded successfully!", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 3.2 Import Master Stations
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Seed/import-master-stations`
- **Business Use Case:** Imports spatial master stations + agency mappings.
### Authentication / Authorization
- **JWT Required:** No (currently)
- **Role Required:** None
### Request Payload
No request body.
### Response Example (200 OK)
```json
{ "success": true, "message": "Master Stations imported successfully!", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 3.3 Import Horus
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Seed/import-horus`
- **Business Use Case:** Imports Horus schedules and fares.
### Authentication / Authorization
- **JWT Required:** No (currently)
- **Role Required:** None
### Request Payload
No request body.
### Response Example (200 OK)
```json
{ "success": true, "message": "Horus Trips imported successfully!", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 3.4 Import BlueBus
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Seed/import-bluebus`
- **Business Use Case:** Imports BlueBus trip blueprints and destination fare matrices.
### Authentication / Authorization
- **JWT Required:** No (currently)
- **Role Required:** None
### Request Payload
No request body.
### Response Example (200 OK)
```json
{ "success": true, "message": "Blue Bus Trips imported successfully!", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 3.5 Import GoBus
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Seed/import-gobus`
- **Business Use Case:** Imports GoBus trips and synthetic route blueprints.
### Authentication / Authorization
- **JWT Required:** No (currently)
- **Role Required:** None
### Request Payload
No request body.
### Response Example (200 OK)
```json
{ "success": true, "message": "GoBus Trips imported successfully!", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 3.6 Import Trains
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Seed/import-trains`
- **Business Use Case:** Imports ENR schedules, stop sequence, and fare matrix.
### Authentication / Authorization
- **JWT Required:** No (currently)
- **Role Required:** None
### Request Payload
No request body.
### Response Example (200 OK)
```json
{ "success": true, "message": "ENR Trains imported successfully!", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 3.7 Generate Occurrences
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Seed/generate-occurrences`
- **Business Use Case:** Generates next 60-day occurrences and class inventories using the schedule-local day boundary.
### Authentication / Authorization
- **JWT Required:** No (currently)
- **Role Required:** None
### Request Payload
No request body.
### Response Example (200 OK)
```json
{ "success": true, "message": "60-Day Calendar generated successfully!", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

---

# 4. Admin Users API

Base route: `/api/admin/users`

All endpoints require Admin policy.

## 4.1 Get All Users
### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/admin/users`
- **Business Use Case:** Lists users for admin management.
### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Admin
### Request Payload
No request body.
### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Users retrieved successfully.",
  "data": [
    { "userId": 15, "email": "user@example.com", "fullName": "Ahmed Mohamed Hassan", "phoneNumber": "+201234567890", "gender": "Male", "countryCode": "EG", "countryName": "Egypt", "profilePictureUrl": null, "roles": ["User"] }
  ],
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

## 4.2 Get User by ID
### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/admin/users/{id}`
- **Business Use Case:** Fetches detailed domain + identity user profile.
### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Admin
### Request Payload
No request body.
### Response Example (200 OK)
```json
{
  "success": true,
  "message": "User retrieved successfully.",
  "data": {
    "userId": 15,
    "fullName": "Ahmed Mohamed Hassan",
    "email": "user@example.com",
    "phone": "+201234567890",
    "nationalIdNumber": "29805151234567",
    "totalTripsCount": 5,
    "totalDistanceTraveled": 1200.5,
    "createdAt": "2026-03-01T10:00:00Z",
    "lastLoginAt": "2026-03-05T09:00:00Z",
    "isActive": true,
    "countryCode": "EG",
    "countryName": "Egypt",
    "roles": ["User"]
  },
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

## 4.3 Toggle User Status
### Endpoint Overview
- **Method:** `PATCH`
- **URL:** `/api/admin/users/{id}/toggle-status`
- **Business Use Case:** Enables/disables account access.
### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Admin
### Request Payload
No request body.
### Response Example (200 OK)
```json
{ "success": true, "message": "User disabled successfully.", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 4.4 Assign Role
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/admin/users/{id}/roles`
- **Business Use Case:** Assigns an existing system role to target user.
### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Admin
### Request Payload
```json
{ "role": "Partner" }
```
### Request Field Reference
| Field | Type   | Required | Notes                        |
| ----- | ------ | -------- | ---------------------------- |
| role  | string | Yes      | Must exist in Identity roles |
### Response Example (200 OK)
```json
{ "success": true, "message": "Role assigned successfully.", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 4.5 Delete User
### Endpoint Overview
- **Method:** `DELETE`
- **URL:** `/api/admin/users/{id}`
- **Business Use Case:** Deletes user from Identity and domain safely.
### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Admin
### Request Payload
No request body.
### Response Example (200 OK)
```json
{ "success": true, "message": "User deleted successfully.", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

---

# 5. User Profile API

Base route: `/api/Users`

## 5.1 Get My Profile
### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/Users/me`
- **Business Use Case:** Returns authenticated user's profile, stats, and wallet.
### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Authenticated user
### Request Payload
No request body.
### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Profile retrieved successfully.",
  "data": {
    "userId": 15,
    "firstName": "Ahmed",
    "familyName": "Mohamed",
    "lastName": "Hassan",
    "email": "user@example.com",
    "phoneNumber": "+201234567890",
    "gender": "Male",
    "profilePictureUrl": "images/profiles/abcd.jpg",
    "countryCode": "EG",
    "countryName": "Egypt",
    "totalTripsCount": 12,
    "totalDistanceTraveled": 345.5,
    "walletBalance": 50.0
  },
  "errors": null,
  "timestamp": "2026-03-10T00:00:00Z"
}
```

## 5.2 Update My Profile
### Endpoint Overview
- **Method:** `PUT`
- **URL:** `/api/Users/me`
- **Business Use Case:** Updates basic profile fields.
### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Authenticated user
### Request Payload
```json
{
  "firstName": "Ahmed",
  "familyName": "Mohamed",
  "lastName": "Hassan",
  "email": "new-email@example.com",
  "phoneNumber": "+201234567891"
}
```
### Request Field Reference
| Field       | Type   | Required | Notes                |
| ----------- | ------ | -------- | -------------------- |
| firstName   | string | Yes      | Max 50               |
| familyName  | string | No       | Max 50               |
| lastName    | string | Yes      | Max 50               |
| email       | string | No       | Unique + valid email |
| phoneNumber | string | No       | Unique + valid phone |
### Response Example (200 OK)
```json
{ "success": true, "message": "Profile updated successfully.", "data": null, "errors": null, "timestamp": "2026-03-10T00:00:00Z" }
```

## 5.3 Upload Profile Picture
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Users/me/profile-picture`
- **Business Use Case:** Uploads/replaces profile image.
### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Authenticated user
### Request Payload
`multipart/form-data` with key `file`.
### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Profile picture uploaded successfully.",
  "data": { "profilePictureUrl": "images/profiles/abcd.jpg" },
  "errors": null,
  "timestamp": "2026-03-10T00:00:00Z"
}
```

---

# 6. Stations API

Base route: `/api/Stations`

## 6.1 Get Grouped Stations
### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/Stations`
- **Business Use Case:** Provides bilingual station dropdown data grouped by governorate.
### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None
### Request Payload
No request body.
### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Stations retrieved successfully.",
  "data": [
    {
      "governorate": "Cairo",
      "stations": [
        { "id": 101, "arabicName": "?????", "englishName": "ramses", "slug": "ramses", "city": "Cairo" }
      ]
    },
    {
      "governorate": "Alexandria",
      "stations": [
        { "id": 201, "arabicName": "???? ????", "englishName": "sidi-gaber", "slug": "sidi-gaber", "city": "Alexandria" }
      ]
    }
  ],
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

---

# 7. Search API

Preferred base route: `/api/trips`

Backward-compatible base route: `/api/Search`

## 7.1 Search Trips (Pagination)

### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/trips/search`
- **Backward-Compatible Alias:** `/api/Search`
- **Business Use Case:** Performs flexible governorate/station-based intercity trip search with dynamic seat inventory filtering.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
Query string parameters:

```json
{
  "travelDate": "2026-03-20",
  "fromGovernorate": "Cairo",
  "fromStationId": null,
  "toGovernorate": "Alexandria",
  "toStationId": null,
  "passengers": 2,
  "transport": 0,
  "sortBy": 1,
  "maxPrice": 250.0,
  "preferredAgencies": ["GoBus", "Blue Bus"],
  "pageNumber": 1,
  "pageSize": 10
}
```

### Request Field Reference
| Field             | Type     | Required    | Notes                                                            |
| ----------------- | -------- | ----------- | ---------------------------------------------------------------- |
| travelDate        | date     | Yes         | Must be schedule-local today..+60 days                           |
| fromGovernorate   | string   | Conditional | Required if fromStationId missing                                |
| fromStationId     | int      | Conditional | Required if fromGovernorate missing                              |
| toGovernorate     | string   | Conditional | Required if toStationId missing                                  |
| toStationId       | int      | Conditional | Required if toGovernorate missing                                |
| passengers        | int      | Yes         | Must be > 0                                                      |
| transport         | int      | No          | 0=All, 1=Bus, 2=Train                                            |
| sortBy            | int      | No          | 0=DepartureTime, 1=LowestPrice, 2=ShortestDuration               |
| maxPrice          | decimal  | No          | Excludes trips where cheapest available class exceeds this value |
| preferredAgencies | string[] | No          | Optional exact-match allowlist for agency names                  |
| pageNumber        | int      | No          | Defaults to 1. Must be > 0                                       |
| pageSize          | int      | No          | Defaults to 10. Allowed range: 1..100                            |

### Paged Response Envelope Fields
| Field       | Type                    | Description                               |
| ----------- | ----------------------- | ----------------------------------------- |
| items       | TripSearchResponseDto[] | Current page of matching trips            |
| totalCount  | int                     | Total matching records across all pages   |
| totalPages  | int                     | Computed as `ceil(totalCount / pageSize)` |
| currentPage | int                     | Current page index (1-based)              |
| pageSize    | int                     | Page size actually applied                |

### Time Semantics
- `boardingTime` / `dropoffTime`: passenger segment schedule-local times (selected origin to selected destination), serialized without timezone suffix.
- `departureTime` / `arrivalTime`: global occurrence-level schedule-local trip start and end times, serialized without timezone suffix.
- `timestamp` in wrapper remains UTC (`Z`).

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Successfully found 2 available trips.",
  "data": {
    "items": [
      {
        "tripOccurrenceId": 1001,
        "tripId": 200,
        "agencyName": "GoBus",
        "boardingTime": "2026-03-20T07:20:00",
        "dropoffTime": "2026-03-20T10:00:00",
        "departureTime": "2026-03-20T07:00:00",
        "arrivalTime": "2026-03-20T10:40:00",
        "totalDurationMinutes": 160,
        "originStationId": 101,
        "originStationName": "رمسيس",
        "originGovernorate": "Cairo",
        "destinationStationId": 201,
        "destinationStationName": "سيدي جابر",
        "destinationGovernorate": "Alexandria",
        "startingPrice": 180.0,
        "routeStops": [
          {
            "stationName": "رمسيس",
            "arrivalTime": null,
            "departureTime": "07:20:00",
            "stopSequence": 1
          },
          {
            "stationName": "سيدي جابر",
            "arrivalTime": "10:00:00",
            "departureTime": null,
            "stopSequence": 5
          }
        ],
        "availableClasses": [
          {
            "coachClassId": 1,
            "className": "Business",
            "remainingSeats": 14,
            "price": 180.0
          }
        ]
      }
    ],
    "totalCount": 2,
    "totalPages": 1,
    "currentPage": 1,
    "pageSize": 10
  },
  "errors": null,
  "timestamp": "2026-03-20T00:00:00Z"
}
```

## 7.2 Search Indirect Trips (1-Stop, Pagination)

### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/trips/search/indirect`
- **Backward-Compatible Alias:** `/api/Search/indirect`
- **Business Use Case:** Finds valid 1-stop routes via spatial transfer-hub pruning, layover validation, and seat-aware class filtering, returned in a paginated envelope.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
Query string parameters:

```json
{
  "travelDate": "2026-03-20",
  "fromGovernorate": "Cairo",
  "toGovernorate": "Aswan",
  "passengers": 1,
  "transport": 0,
  "sortBy": 2,
  "maxPrice": 600.0,
  "preferredAgencies": ["Egyptian National Railways", "GoBus"],
  "pageNumber": 1,
  "pageSize": 10
}
```

### Request Field Reference
| Field             | Type     | Required    | Notes                                              |
| ----------------- | -------- | ----------- | -------------------------------------------------- |
| travelDate        | date     | Yes         | Must be schedule-local today..+60 days             |
| fromGovernorate   | string   | Conditional | Required if fromStationId missing                  |
| fromStationId     | int      | Conditional | Required if fromGovernorate missing                |
| toGovernorate     | string   | Conditional | Required if toStationId missing                    |
| toStationId       | int      | Conditional | Required if toGovernorate missing                  |
| passengers        | int      | Yes         | Must be > 0                                        |
| transport         | int      | No          | 0=All, 1=Bus, 2=Train                              |
| sortBy            | int      | No          | 0=DepartureTime, 1=LowestPrice, 2=ShortestDuration |
| maxPrice          | decimal  | No          | Applied through class price filtering              |
| preferredAgencies | string[] | No          | Optional exact-match allowlist for agency names    |
| pageNumber        | int      | No          | Defaults to 1. Must be > 0                         |
| pageSize          | int      | No          | Defaults to 10. Allowed range: 1..100              |

### Indirect Search Behavior Notes
- Indirect routes are returned only when direct routes do not exist for the same request criteria.
- Routes are evaluated with layover window constraints (minimum 1 hour, maximum 6 hours).

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Found 1 indirect routes.",
  "data": {
    "items": [
      {
        "totalDurationMinutes": 600,
        "layoverDurationMinutes": 95,
        "totalStartingPrice": 420.0,
        "legs": [
          {
            "tripOccurrenceId": 5011,
            "tripId": 310,
            "agencyName": "GoBus",
            "boardingTime": "2026-03-20T06:30:00",
            "dropoffTime": "2026-03-20T09:30:00",
            "departureTime": "2026-03-20T06:00:00",
            "arrivalTime": "2026-03-20T10:00:00",
            "totalDurationMinutes": 180,
            "originStationId": 101,
            "originStationName": "رمسيس",
            "originGovernorate": "Cairo",
            "destinationStationId": 220,
            "destinationStationName": "المنيا",
            "destinationGovernorate": "Minya",
            "startingPrice": 180.0,
            "routeStops": [
              { "stationName": "رمسيس", "arrivalTime": null, "departureTime": "06:30:00", "stopSequence": 1 },
              { "stationName": "المنيا", "arrivalTime": "09:30:00", "departureTime": null, "stopSequence": 4 }
            ],
            "availableClasses": [
              { "coachClassId": 1, "className": "Business", "remainingSeats": 9, "price": 180.0 }
            ]
          },
          {
            "tripOccurrenceId": 9912,
            "tripId": 777,
            "agencyName": "Egyptian National Railways",
            "boardingTime": "2026-03-20T11:05:00",
            "dropoffTime": "2026-03-20T16:30:00",
            "departureTime": "2026-03-20T10:30:00",
            "arrivalTime": "2026-03-20T17:10:00",
            "totalDurationMinutes": 325,
            "originStationId": 220,
            "originStationName": "المنيا",
            "originGovernorate": "Minya",
            "destinationStationId": 880,
            "destinationStationName": "أسوان",
            "destinationGovernorate": "Aswan",
            "startingPrice": 240.0,
            "routeStops": [
              { "stationName": "المنيا", "arrivalTime": null, "departureTime": "11:05:00", "stopSequence": 2 },
              { "stationName": "أسوان", "arrivalTime": "16:30:00", "departureTime": null, "stopSequence": 7 }
            ],
            "availableClasses": [
              { "coachClassId": 2, "className": "Second Class", "remainingSeats": 22, "price": 240.0 }
            ]
          }
        ]
      }
    ],
    "totalCount": 1,
    "totalPages": 1,
    "currentPage": 1,
    "pageSize": 10
  },
  "errors": null,
  "timestamp": "2026-03-20T00:00:00Z"
}
```

---

# 8. Occurrences API

Base route: `/api/occurrences`

## 8.1 Get Seat Map
### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/occurrences/{id}/seats`
- **Business Use Case:** Returns real-time seat status for each class on a trip occurrence, including layout metadata and temporary pending holds.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Path Parameters
| Field | Type | Required | Notes                      |
| ----- | ---- | -------- | -------------------------- |
| id    | int  | Yes      | Trip occurrence identifier |

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Seat map retrieved successfully.",
  "data": {
    "occurrenceId": 1001,
    "generatedAtUtc": "2026-04-18T12:35:00Z",
    "classes": [
      {
        "coachClassId": 1,
        "className": "Business",
        "totalSeats": 36,
        "remainingSeats": 12,
        "layoutType": "2x1",
        "deckCount": 1,
        "seatMapJson": "{\"rows\":12,\"cols\":3}",
        "availableCount": 12,
        "pendingCount": 4,
        "bookedCount": 20,
        "seats": [
          {
            "seatNumber": "1",
            "status": "Booked",
            "bookingId": 4001,
            "holdExpiresAt": null
          },
          {
            "seatNumber": "2",
            "status": "Pending",
            "bookingId": 4050,
            "holdExpiresAt": "2026-04-18T12:40:00Z"
          },
          {
            "seatNumber": "3",
            "status": "Available",
            "bookingId": null,
            "holdExpiresAt": null
          }
        ]
      }
    ]
  },
  "errors": null,
  "timestamp": "2026-04-18T12:35:00Z"
}
```

### Seat Status Semantics
- **Available:** Seat is free and can be selected.
- **Pending:** Seat is held by a pending booking whose hold has not expired; includes `holdExpiresAt`.
- **Booked:** Seat belongs to a confirmed/completed booking.

### Response Field Highlights
| Field                           | Type      | Description                                            |
| ------------------------------- | --------- | ------------------------------------------------------ |
| classes[].layoutType            | string?   | Optional layout descriptor such as `2x1`, `2x2`, etc.  |
| classes[].deckCount             | int       | Number of decks for this class layout                  |
| classes[].seatMapJson           | string?   | Serialized layout metadata for advanced seat rendering |
| classes[].availableCount        | int       | Seats currently available                              |
| classes[].pendingCount          | int       | Seats on active pending hold                           |
| classes[].bookedCount           | int       | Seats locked by confirmed/completed bookings           |
| classes[].seats[].holdExpiresAt | datetime? | Present only when seat status is `Pending`             |

### Note
Seat layout metadata depends on CoachClass seat layout columns being present; missing migrations can cause this endpoint to fail.

---

# 9. Bookings API

Base route: `/api/Bookings`

### Time Semantics
- `holdExpiresAt` and `bookingDate` are UTC timestamps (`Z`).
- `boardingTime` and `dropoffTime` are schedule-local timetable timestamps without timezone suffix.

## 9.1 Add Trip to Cart (Soft Lock)
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Bookings/cart`
- **Backward-Compatible Alias:** `/api/Bookings/cart/add`
- **Business Use Case:** Adds a new independent cart item with its own 10-minute hold timer.

### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Any logged-in User

### Request Payload
```json
{
  "tripOccurrenceId": 1001,
  "coachClassId": 1,
  "originStationId": 101,
  "destinationStationId": 201,
  "contactName": "Ali Hassan",
  "contactPhone": "+201234567890",
  "contactEmail": "ali@example.com",
  "passengers": [
    { "seatNumber": "7", "passengerName": "Ali Hassan", "idType": "NationalId", "idNumber": "29805151111121" },
    { "seatNumber": "8", "passengerName": "Sara Mohamed", "idType": "Passport", "idNumber": "A12345678" }
  ]
}
```

### Request Field Reference
| Field                      | Type     | Required    | Notes                                                                                              |
| -------------------------- | -------- | ----------- | -------------------------------------------------------------------------------------------------- |
| tripOccurrenceId           | int      | Yes         | Must be > 0                                                                                        |
| coachClassId               | int      | Yes         | Must be > 0                                                                                        |
| originStationId            | int      | Yes         | Must be > 0                                                                                        |
| destinationStationId       | int      | Yes         | Must be > 0 and cannot equal origin                                                                |
| contactName                | string   | Yes         | Required, max 200                                                                                  |
| contactPhone               | string   | Yes         | Required, max 50                                                                                   |
| contactEmail               | string   | Yes         | Required, valid email, max 255                                                                     |
| passengers                 | object[] | Yes         | Minimum 1 and maximum 10 passengers                                                                |
| passengers[].seatNumber    | string   | Conditional | Required for non-ENR agencies, max 50                                                              |
| passengers[].passengerName | string   | Conditional | Required for ENR agency, max 200                                                                   |
| passengers[].idType        | string   | Conditional | Required for ENR agency. Allowed: `NationalId`, `Passport`, `DrivingLicense`, `StudentId`, `Other` |
| passengers[].idNumber      | string   | Conditional | Required for ENR agency, max 50                                                                    |

### Booking Rules Enforced
- Hold duration is 10 minutes per cart item.
- Inventory and fare must exist for the selected occurrence/class/segment.
- **Non-ENR agencies:** seat number is mandatory per passenger; seats must be unique, numeric, and within class capacity.
- **ENR agency:** seats are auto-assigned; each passenger must provide `passengerName`, `idType`, and `idNumber`.
- For ENR, passenger ID numbers must be unique in the same request and cannot already exist on active pending/confirmed bookings for the same occurrence.
- If seats are taken concurrently, API returns conflict and asks client to refresh seat map.

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Trip added to cart successfully.",
  "data": {
    "items": [
      {
        "bookingId": 1024,
        "totalPrice": 360.0,
        "seatsBooked": 2,
        "holdExpiresAt": "2026-03-20T07:10:00Z",
        "agencyName": "GoBus",
        "className": "Business",
        "origin": "رمسيس",
        "destination": "سيدي جابر",
        "boardingTime": "2026-03-20T07:20:00",
        "dropoffTime": "2026-03-20T10:00:00",
        "passengers": [
          {
            "name": "Ali Hassan",
            "idNumber": "29805151111121",
            "seatNumber": "7"
          },
          {
            "name": "Sara Mohamed",
            "idNumber": "A12345678",
            "seatNumber": "8"
          }
        ]
      }
    ],
    "grandTotal": 360.0
  },
  "errors": null,
  "timestamp": "2026-03-20T00:10:00Z"
}
```

## 9.2 Checkout & Confirm Booking
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Bookings/checkout`
- **Business Use Case:** Checks out all valid pending cart items in one operation and charges wallet once.

### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Any logged-in User

### Request Payload
```json
{
  "paymentMethod": "Wallet"
}
```

### Request Field Reference
| Field         | Type   | Required | Notes                                |
| ------------- | ------ | -------- | ------------------------------------ |
| paymentMethod | string | Yes      | Currently only `Wallet` is supported |

### Checkout Rules
- Only `Wallet` payment is accepted (case-insensitive).
- Returns 400 if the cart is empty/expired or wallet balance is insufficient.
- Returns 409 on seat concurrency conflicts.

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Checkout successful. 360.00 was deducted from your wallet for 2 trip(s).",
  "data": "Checkout successful. 360.00 was deducted from your wallet for 2 trip(s).",
  "errors": null,
  "timestamp": "2026-03-31T00:10:00.000Z"
}
```

## 9.3 Get Active Cart
### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/Bookings/cart`
- **Business Use Case:** Returns the current pending, unexpired cart for the authenticated user.

### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Any logged-in User

### Request Payload
No request body.

### Response Example (200 OK - cart exists)
```json
{
  "success": true,
  "message": "Active cart retrieved successfully.",
  "data": {
    "items": [
      {
        "bookingId": 1024,
        "totalPrice": 360.0,
        "seatsBooked": 2,
        "holdExpiresAt": "2026-03-31T00:15:00Z",
        "agencyName": "GoBus",
        "className": "Business",
        "origin": "رمسيس",
        "destination": "سيدي جابر",
        "boardingTime": "2026-04-02T07:20:00",
        "dropoffTime": "2026-04-02T10:00:00",
        "passengers": [
          {
            "name": "Ali Hassan",
            "idNumber": "29805151111121",
            "seatNumber": "7"
          },
          {
            "name": "Sara Mohamed",
            "idNumber": "A12345678",
            "seatNumber": "8"
          }
        ]
      }
    ],
    "grandTotal": 360.0
  },
  "errors": null,
  "timestamp": "2026-03-31T00:10:00Z"
}
```

### Response Example (200 OK - no cart)
```json
{
  "success": true,
  "message": "No active cart found.",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-31T00:10:00Z"
}
```

## 9.4 Get My Tickets
### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/Bookings/my-tickets`
- **Business Use Case:** Returns ticket history for the authenticated user (all non-pending bookings).

### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Any logged-in User

### Request Payload
No request body.

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Tickets retrieved successfully.",
  "data": [
    {
      "bookingId": 1024,
      "status": "Confirmed",
      "paymentStatus": "Paid",
      "totalPrice": 360.0,
      "seatsBooked": 2,
      "bookingDate": "2026-03-31T00:02:00Z",
      "agencyName": "GoBus",
      "className": "Business",
      "originStation": "رمسيس",
      "destinationStation": "سيدي جابر",
      "boardingTime": "2026-04-02T07:20:00",
      "dropoffTime": "2026-04-02T10:00:00",
      "passengers": [
        {
          "name": "Ali Hassan",
          "idNumber": "29805151111121",
          "seatNumber": "7"
        },
        {
          "name": "Sara Mohamed",
          "idNumber": "A12345678",
          "seatNumber": "8"
        }
      ]
    }
  ],
  "errors": null,
  "timestamp": "2026-03-31T00:10:00Z"
}
```

---

# 10. Wallet API

Base route: `/api/Wallet`

## 10.1 Deposit to Wallet
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Wallet/deposit`
- **Business Use Case:** Simulates a payment gateway card deposit and records a wallet ledger transaction.

### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Authenticated user

### Response Statuses
- **200 OK**: Deposit processed successfully.
- **400 Bad Request**: Validation or business rule failure.
- **401 Unauthorized**: Missing/invalid token.

### Request Payload
```json
{
  "amount": 500.0,
  "mockCardNumber": "4242424242424242",
  "expiryDate": "12/29",
  "cvv": "123"
}
```

### Request Field Reference
| Field          | Type    | Required | Notes                                            |
| -------------- | ------- | -------- | ------------------------------------------------ |
| amount         | decimal | Yes      | Must be between 10 and 10000                     |
| mockCardNumber | string  | Yes      | Exactly 16 digits                                |
| expiryDate     | string  | Yes      | Format `MM/YY`, must be a valid non-expired date |
| cvv            | string  | Yes      | Exactly 3 digits                                 |

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Successfully deposited 500.00 EGP. Your new balance is 650.00 EGP.",
  "data": "Successfully deposited 500.00 EGP. Your new balance is 650.00 EGP.",
  "errors": null,
  "timestamp": "2026-03-31T01:20:00.000Z"
}
```

## 10.2 Get Wallet Transaction History
### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/Wallet/history`
- **Business Use Case:** Returns the authenticated user's wallet ledger transactions, newest first.

### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Authenticated user

### Request Payload
No request body.

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Wallet transaction history retrieved successfully.",
  "data": [
    {
      "id": 51,
      "amount": -360.0,
      "type": "TicketPurchase",
      "description": "Checkout for multiple trips.",
      "bookingId": null,
      "createdAt": "2026-03-31T00:10:00Z"
    },
    {
      "id": 50,
      "amount": 500.0,
      "type": "Deposit",
      "description": "Deposit via simulated card ending in 4242",
      "bookingId": null,
      "createdAt": "2026-03-31T01:20:00Z"
    }
  ],
  "errors": null,
  "timestamp": "2026-03-31T01:21:00Z"
}
```

---

# 11. Jobs API

Base route: `/api/Jobs`

These endpoints are intended for scheduler/automation usage.

Authentication model:
- JWT is not required.
- A `secret` query parameter is required and must match server-side `JobSecretKey`.

## 11.1 Generate Occurrences (Scheduler)
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Jobs/generate-occurrences?secret=<JobSecretKey>`
- **Business Use Case:** Generates next 60-day occurrences and inventories using schedule-local day boundaries.

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Trip occurrences generated successfully.",
  "data": null,
  "errors": null,
  "timestamp": "2026-04-18T17:00:00Z"
}
```

## 11.2 Process Completed Trips
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Jobs/process-completed-trips?secret=<JobSecretKey>`
- **Business Use Case:** Marks eligible confirmed bookings as completed based on schedule-local arrival cutoff.

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Completed trips processed successfully.",
  "data": null,
  "errors": null,
  "timestamp": "2026-04-18T17:00:00Z"
}
```

## 11.3 Release Expired Holds
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Jobs/release-expired-holds?secret=<JobSecretKey>`
- **Business Use Case:** Releases expired pending holds and restores inventory.

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Expired holds released and inventory restored.",
  "data": null,
  "errors": null,
  "timestamp": "2026-04-18T17:00:00Z"
}
```

---

# 12. Marketplace API

Base route: `/api/Marketplace`

## 12.1 List Ticket
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Marketplace/list`
- **Business Use Case:** Lists a single passenger ticket for resale.

### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Authenticated user

### Request Payload
```json
{
  "bookingId": 1024,
  "passengerId": 5001,
  "askingPrice": 120.0
}
```

### Request Field Reference
| Field       | Type    | Required | Notes                       |
| ----------- | ------- | -------- | --------------------------- |
| bookingId   | int     | Yes      | Must be a confirmed booking |
| passengerId | int     | Yes      | Passenger within booking    |
| askingPrice | decimal | Yes      | Must be > 0 and below original ticket price |

### Listing Rules
- Only the booking owner can list a ticket.
- Booking must be `Confirmed` and trip departure must be in the future.
- Passenger must exist and not be already offered for resale.
- Asking price must be strictly less than the original ticket price.

### Response Example (200 OK)
```json
{ "success": true, "message": "Ticket listed on marketplace successfully.", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 12.2 Buy Ticket
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Marketplace/buy/{listingId}`
- **Business Use Case:** Purchases a listed ticket and transfers ownership.

### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Authenticated user

### Path Parameters
| Field     | Type | Required | Notes          |
| --------- | ---- | -------- | -------------- |
| listingId | int  | Yes      | Marketplace ID |

### Purchase Rules
- Listing must exist and be `Available`.
- Buyer cannot be the seller.
- Trip departure must be in the future.
- Buyer wallet balance must cover `askingPrice`.

### Response Example (200 OK)
```json
{ "success": true, "message": "Ticket purchased successfully.", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

## 12.3 Get Active Listings
### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/Marketplace/active`
- **Business Use Case:** Returns paged active listings with trip details.

### Authentication / Authorization
- **JWT Required:** No
- **Role Required:** None

### Request Payload
Query string parameters:

| Field                  | Type   | Required | Notes                             |
| ---------------------- | ------ | -------- | --------------------------------- |
| pageNumber             | int    | No       | Default 1, must be > 0            |
| pageSize               | int    | No       | Default 10, must be >= 1          |
| originStationId        | int    | No       | Filter by origin station          |
| destinationStationId   | int    | No       | Filter by destination station     |
| originGovernorate      | string | No       | Filter by origin governorate      |
| destinationGovernorate | string | No       | Filter by destination governorate |
| travelDate             | date   | No       | Schedule-local departure date     |

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Active marketplace listings retrieved successfully.",
  "data": {
    "items": [
      {
        "listingId": 100,
        "originalPrice": 180.0,
        "askingPrice": 150.0,
        "sellerName": "Ahmed Hassan",
        "tripDetails": {
          "origin": "Ramses",
          "destination": "Sidi Gaber",
          "time": "2026-04-02T07:20:00",
          "class": "GoBus - Business"
        }
      }
    ],
    "totalCount": 1,
    "totalPages": 1,
    "currentPage": 1,
    "pageSize": 10
  },
  "errors": null,
  "timestamp": "2026-04-02T00:00:00Z"
}
```

### Notes
- If no listings exist, the message is "No active marketplace listings found." and items list is empty.
- `tripDetails.time` is a schedule-local timestamp without timezone suffix.
- Filters are optional and combined with AND logic.
- `travelDate` matches the schedule-local date portion of the trip departure.

# Quick Endpoint Index

| Method   | URL                                   |               Auth | Description                                                  |
| -------- | ------------------------------------- | -----------------: | ------------------------------------------------------------ |
| `POST`   | `/api/Auth/register`                  |                 No | Register user and return tokens                              |
| `POST`   | `/api/Auth/login`                     |                 No | Login and return tokens                                      |
| `POST`   | `/api/Auth/refresh`                   |                 No | Refresh access token                                         |
| `POST`   | `/api/Auth/revoke`                    |                 No | Revoke one refresh token                                     |
| `POST`   | `/api/Auth/revoke-all`                |                Yes | Revoke all active refresh tokens                             |
| `GET`    | `/api/Auth/me`                        |                Yes | Return current JWT claim info                                |
| `POST`   | `/api/Auth/send-verification-email`   |                 No | Send verification email                                      |
| `POST`   | `/api/Auth/verify-email`              |                 No | Confirm email token                                          |
| `POST`   | `/api/Auth/forgot-password`           |                 No | Send reset link                                              |
| `POST`   | `/api/Auth/reset-password`            |                 No | Reset password                                               |
| `POST`   | `/api/Auth/change-password`           |                Yes | Change password                                              |
| `GET`    | `/api/Countries`                      |                 No | List countries                                               |
| `POST`   | `/api/Seed/init-identity`             |     No (currently) | Initialize identity roles/admin                              |
| `POST`   | `/api/Seed/import-master-stations`    |     No (currently) | Import master stations                                       |
| `POST`   | `/api/Seed/import-horus`              |     No (currently) | Import Horus trips                                           |
| `POST`   | `/api/Seed/import-gobus`              |     No (currently) | Import GoBus trips                                           |
| `POST`   | `/api/Seed/import-bluebus`            |     No (currently) | Import BlueBus trips                                         |
| `POST`   | `/api/Seed/import-trains`             |     No (currently) | Import train trips                                           |
| `POST`   | `/api/Seed/generate-occurrences`      |     No (currently) | Generate future occurrences                                  |
| `POST`   | `/api/Jobs/generate-occurrences`      | Secret query param | Generate future occurrences (scheduler endpoint)             |
| `POST`   | `/api/Jobs/process-completed-trips`   | Secret query param | Mark eligible trips as completed                             |
| `POST`   | `/api/Jobs/release-expired-holds`     | Secret query param | Release expired holds and restore inventory                  |
| `GET`    | `/api/admin/users`                    |        Yes (Admin) | List all users                                               |
| `GET`    | `/api/admin/users/{id}`               |        Yes (Admin) | Get user detail                                              |
| `PATCH`  | `/api/admin/users/{id}/toggle-status` |        Yes (Admin) | Toggle user active status                                    |
| `POST`   | `/api/admin/users/{id}/roles`         |        Yes (Admin) | Assign role                                                  |
| `DELETE` | `/api/admin/users/{id}`               |        Yes (Admin) | Delete user                                                  |
| `GET`    | `/api/Users/me`                       |                Yes | Get profile                                                  |
| `PUT`    | `/api/Users/me`                       |                Yes | Update profile                                               |
| `POST`   | `/api/Users/me/profile-picture`       |                Yes | Upload profile picture                                       |
| `GET`    | `/api/Stations`                       |                 No | Get grouped stations                                         |
| `GET`    | `/api/trips/search`                   |                 No | Preferred paginated direct-trip search route                 |
| `GET`    | `/api/Search`                         |                 No | Backward-compatible alias for direct-trip search             |
| `GET`    | `/api/trips/search/indirect`          |                 No | Preferred 1-stop indirect search route                       |
| `GET`    | `/api/Search/indirect`                |                 No | Backward-compatible alias for indirect search                |
| `GET`    | `/api/occurrences/{id}/seats`         |                 No | Get real-time seat map with available/pending/booked states  |
| `POST`   | `/api/Bookings/cart`                  |                Yes | Add trip to cart with 10-minute seat soft-lock               |
| `POST`   | `/api/Bookings/cart/add`              |                Yes | Backward-compatible add-to-cart alias                        |
| `GET`    | `/api/Bookings/cart`                  |                Yes | Retrieve current active cart                                 |
| `POST`   | `/api/Bookings/checkout`              |                Yes | Checkout all valid pending cart items with one wallet charge |
| `GET`    | `/api/Bookings/my-tickets`            |                Yes | Retrieve user's ticket history                               |
| `POST`   | `/api/Marketplace/list`               |                Yes | List ticket for resale                                       |
| `POST`   | `/api/Marketplace/buy/{listingId}`    |                Yes | Purchase listed ticket                                       |
| `GET`    | `/api/Marketplace/active`             |                 No | Retrieve active marketplace listings                         |
| `POST`   | `/api/Wallet/deposit`                 |                Yes | Deposit wallet funds and write ledger entry                  |
| `GET`    | `/api/Wallet/history`                 |                Yes | Retrieve wallet transaction history (newest first)           |