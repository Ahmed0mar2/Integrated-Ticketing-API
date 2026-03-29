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
| Field | Type | Required | Notes |
|---|---|---|---|
| email | string | Yes | Valid email, max 255 |
| password | string | Yes | Min 8, upper/lower/digit |
| confirmPassword | string | Yes | Must match password |
| phoneNumber | string | Yes | E.164 format |
| firstName | string | Yes | Max 100 |
| lastName | string | Yes | Max 100 |
| familyName | string | Yes | Max 100 |
| gender | int | Yes | 1=Male,2=Female,3=Other |
| dateOfBirth | date | Yes | At least 16 years old |
| nationalIdNumber | string | No | 14 digits if provided |
| countryCode | string | Yes | 2 chars, must exist |

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
      "countryName": "Egyptian",
      "profilePictureUrl": null,
      "roles": ["User"]
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
| Field | Type | Required | Notes |
|---|---|---|---|
| email | string | Yes | Valid email |
| password | string | Yes | Non-empty |
| deviceInfo | string | No | Optional device metadata |

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
      "countryName": "Egyptian",
      "profilePictureUrl": null,
      "roles": ["User"]
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
| Field | Type | Required | Notes |
|---|---|---|---|
| refreshToken | string | Yes | Must be active token |

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "new-refresh-token",
    "expiresAt": "2026-03-06T13:00:00Z",
    "user": { "userId": 15, "email": "user@example.com", "fullName": "Ahmed Mohamed Hassan", "phoneNumber": "+201234567890", "gender": "Male", "countryCode": "EG", "countryName": "Egyptian", "profilePictureUrl": null, "roles": ["User"] }
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
| Field | Type | Required | Notes |
|---|---|---|---|
| refreshToken | string | Yes | Required |

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
| Field | Type | Required | Notes |
|---|---|---|---|
| email | string | Yes | Valid email format |

### Response Example (200 OK)
```json
{ "success": true, "message": "Verification email sent successfully", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

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
| Field | Type | Required | Notes |
|---|---|---|---|
| userId | string | Yes | Identity user id (string form) |
| token | string | Yes | Email confirmation token |

### Response Example (200 OK)
```json
{ "success": true, "message": "Email verified successfully", "data": null, "errors": null, "timestamp": "2026-03-06T12:00:00Z" }
```

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
| Field | Type | Required | Notes |
|---|---|---|---|
| email | string | Yes | Valid email |

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
| Field | Type | Required | Notes |
|---|---|---|---|
| email | string | Yes | Valid email |
| token | string | Yes | Password reset token |
| newPassword | string | Yes | Min 8, upper/lower/digit |
| confirmPassword | string | Yes | Must match newPassword |

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
| Field | Type | Required | Notes |
|---|---|---|---|
| currentPassword | string | Yes | Current user password |
| newPassword | string | Yes | Min 8, upper/lower/digit |
| confirmPassword | string | Yes | Must match newPassword |

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

All endpoints require Admin policy.

## 3.1 Initialize Identity
### Endpoint Overview
- **Method:** `POST`
- **URL:** `/api/Seed/init-identity`
- **Business Use Case:** Seeds default roles + admin user.
### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Admin
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
- **JWT Required:** Yes
- **Role Required:** Admin
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
- **JWT Required:** Yes
- **Role Required:** Admin
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
- **JWT Required:** Yes
- **Role Required:** Admin
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
- **JWT Required:** Yes
- **Role Required:** Admin
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
- **JWT Required:** Yes
- **Role Required:** Admin
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
- **Business Use Case:** Generates next 60-day occurrences and class inventories.
### Authentication / Authorization
- **JWT Required:** Yes
- **Role Required:** Admin
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
    "isActive": true
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
| Field | Type | Required | Notes |
|---|---|---|---|
| role | string | Yes | Must exist in Identity roles |
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
| Field | Type | Required | Notes |
|---|---|---|---|
| firstName | string | Yes | Max 50 |
| familyName | string | No | Max 50 |
| lastName | string | Yes | Max 50 |
| email | string | No | Unique + valid email |
| phoneNumber | string | No | Unique + valid phone |
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

Base route: `/api/Search`

## 7.1 Search Trips

### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/Search`
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
  "preferredAgencies": ["GoBus", "Blue Bus"]
}
```

### Request Field Reference
| Field | Type | Required | Notes |
|---|---|---|---|
| travelDate | date | Yes | Must be today..+60 days |
| fromGovernorate | string | Conditional | Required if fromStationId missing |
| fromStationId | int | Conditional | Required if fromGovernorate missing |
| toGovernorate | string | Conditional | Required if toStationId missing |
| toStationId | int | Conditional | Required if toGovernorate missing |
| passengers | int | Yes | Must be > 0 |
| transport | int | No | 0=All, 1=Bus, 2=Train |
| sortBy | int | No | 0=DepartureTime, 1=LowestPrice, 2=ShortestDuration |
| maxPrice | decimal | No | Excludes trips where cheapest available class exceeds this value |
| preferredAgencies | string[] | No | Optional exact-match allowlist for agency names |

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Successfully found 2 available trips.",
  "data": [
    {
      "tripOccurrenceId": 1001,
      "tripId": 200,
      "agencyName": "GoBus",
      "departureTime": "2026-03-20T07:00:00Z",
      "arrivalTime": "2026-03-20T10:00:00Z",
      "totalDurationMinutes": 180,
      "originStationId": 101,
      "originStationName": "رمسيس",
      "originGovernorate": "Cairo",
      "destinationStationId": 201,
      "destinationStationName": "سيدي جابر",
      "destinationGovernorate": "Alexandria",
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
  "errors": null,
  "timestamp": "2026-03-20T00:00:00Z"
}
```

## 7.2 Search Indirect Trips (1-Stop)

### Endpoint Overview
- **Method:** `GET`
- **URL:** `/api/Search/indirect`
- **Business Use Case:** Finds valid 1-stop routes via spatial transfer-hub pruning, layover validation, and seat-aware class filtering.

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
  "preferredAgencies": ["Egyptian National Railways", "GoBus"]
}
```

### Request Field Reference
| Field | Type | Required | Notes |
|---|---|---|---|
| travelDate | date | Yes | Must be today..+60 days |
| fromGovernorate | string | Conditional | Required if fromStationId missing |
| fromStationId | int | Conditional | Required if fromGovernorate missing |
| toGovernorate | string | Conditional | Required if toStationId missing |
| toStationId | int | Conditional | Required if toGovernorate missing |
| passengers | int | Yes | Must be > 0 |
| transport | int | No | 0=All, 1=Bus, 2=Train |
| sortBy | int | No | 0=DepartureTime, 1=LowestPrice, 2=ShortestDuration |
| maxPrice | decimal | No | Applied through class price filtering |
| preferredAgencies | string[] | No | Optional exact-match allowlist for agency names |

### Response Example (200 OK)
```json
{
  "success": true,
  "message": "Found 1 indirect routes.",
  "data": [
    {
      "totalDurationMinutes": 505,
      "layoverDurationMinutes": 95,
      "totalStartingPrice": 420.0,
      "legs": [
        {
          "tripOccurrenceId": 5011,
          "tripId": 310,
          "agencyName": "GoBus",
          "departureTime": "2026-03-20T06:30:00Z",
          "arrivalTime": "2026-03-20T09:30:00Z",
          "totalDurationMinutes": 180,
          "originStationId": 101,
          "originStationName": "رمسيس",
          "originGovernorate": "Cairo",
          "destinationStationId": 220,
          "destinationStationName": "المنيا",
          "destinationGovernorate": "Minya",
          "availableClasses": [
            { "coachClassId": 1, "className": "Business", "remainingSeats": 9, "price": 180.0 }
          ]
        },
        {
          "tripOccurrenceId": 9912,
          "tripId": 777,
          "agencyName": "Egyptian National Railways",
          "departureTime": "2026-03-20T11:05:00Z",
          "arrivalTime": "2026-03-20T16:30:00Z",
          "totalDurationMinutes": 325,
          "originStationId": 220,
          "originStationName": "المنيا",
          "originGovernorate": "Minya",
          "destinationStationId": 880,
          "destinationStationName": "أسوان",
          "destinationGovernorate": "Aswan",
          "availableClasses": [
            { "coachClassId": 2, "className": "Second Class", "remainingSeats": 22, "price": 240.0 }
          ]
        }
      ]
    }
  ],
  "errors": null,
  "timestamp": "2026-03-20T00:00:00Z"
}
```

---

# Quick Endpoint Index

| Method | URL | Auth | Description |
|---|---|---:|---|
| `POST` | `/api/Auth/register` | No | Register user and return tokens |
| `POST` | `/api/Auth/login` | No | Login and return tokens |
| `POST` | `/api/Auth/refresh` | No | Refresh access token |
| `POST` | `/api/Auth/revoke` | No | Revoke one refresh token |
| `POST` | `/api/Auth/revoke-all` | Yes | Revoke all active refresh tokens |
| `GET` | `/api/Auth/me` | Yes | Return current JWT claim info |
| `POST` | `/api/Auth/send-verification-email` | No | Send verification email |
| `POST` | `/api/Auth/verify-email` | No | Confirm email token |
| `POST` | `/api/Auth/forgot-password` | No | Send reset link |
| `POST` | `/api/Auth/reset-password` | No | Reset password |
| `POST` | `/api/Auth/change-password` | Yes | Change password |
| `GET` | `/api/Countries` | No | List countries |
| `POST` | `/api/Seed/init-identity` | Yes (Admin) | Initialize identity roles/admin |
| `POST` | `/api/Seed/import-master-stations` | Yes (Admin) | Import master stations |
| `POST` | `/api/Seed/import-horus` | Yes (Admin) | Import Horus trips |
| `POST` | `/api/Seed/import-gobus` | Yes (Admin) | Import GoBus trips |
| `POST` | `/api/Seed/import-bluebus` | Yes (Admin) | Import BlueBus trips |
| `POST` | `/api/Seed/import-trains` | Yes (Admin) | Import train trips |
| `POST` | `/api/Seed/generate-occurrences` | Yes (Admin) | Generate future occurrences |
| `GET` | `/api/admin/users` | Yes (Admin) | List all users |
| `GET` | `/api/admin/users/{id}` | Yes (Admin) | Get user detail |
| `PATCH` | `/api/admin/users/{id}/toggle-status` | Yes (Admin) | Toggle user active status |
| `POST` | `/api/admin/users/{id}/roles` | Yes (Admin) | Assign role |
| `DELETE` | `/api/admin/users/{id}` | Yes (Admin) | Delete user |
| `GET` | `/api/Users/me` | Yes | Get profile |
| `PUT` | `/api/Users/me` | Yes | Update profile |
| `POST` | `/api/Users/me/profile-picture` | Yes | Upload profile picture |
| `GET` | `/api/Stations` | No | Get grouped stations |
| `GET` | `/api/Search` | No | Flexible governorate/station trip search with inventory filtering |
| `GET` | `/api/Search/indirect` | No | Advanced 1-stop indirect routing with spatial transfer-hub pruning |