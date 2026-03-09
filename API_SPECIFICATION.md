# Rehla API Specification

Frontend integration guide for the `Rehla` backend API, intended for the Flutter and Angular teams.

This document is derived from the current controller layer, DTOs, FluentValidation rules, service-layer business logic, authentication setup, and response wrappers in the workspace.

---

## Base Information

- **Base Route Prefix:** `/api`
- **Authentication Scheme:** `JWT Bearer`
- **Primary Response Wrapper:** `ApiResponse<T>` or `ApiResponse`
- **Global Validation Behavior:** Request DTO validation is enforced through a custom validation filter using FluentValidation.
- **Unhandled Exceptions:** Returned as RFC-style `ProblemDetails` from the global exception handler.

### Standard Success Response Shape

```json
{
  "success": true,
  "message": "Operation successful",
  "data": {},
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### Standard Error Response Shape

```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": [
    "Email is required",
    "Password must be at least 8 characters"
  ],
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### Unhandled Server Error Shape

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Server Error",
  "status": 500,
  "detail": "Unexpected error message"
}
```

---

## Authentication Notes

### JWT Behavior

The API uses JWT Bearer authentication.

Send the token in the header:

```http
Authorization: Bearer <access_token>
```

### JWT Claims Issued on Login/Register/Refresh

The access token includes these important claims:

- `nameidentifier`: Identity user id
- `email`: user email
- `name`: user display name (`FirstName LastName`)
- `domain_user_id`: domain user id
- `jti`: token unique identifier
- one or more role claims

### Role Requirements

From the currently open controllers:

- Authentication endpoints use anonymous access or authenticated access depending on the route.
- Admin user management endpoints require the `RequireAdminRole` policy.
- The `RequireAdminRole` policy maps to the `Admin` role.

---

## Validation & Error Handling Rules

### Validation Source

Validation is enforced by FluentValidation through `ValidationFilter`.

If validation fails, the API returns:

- **HTTP 400 Bad Request**
- `message = "Validation failed"`
- `errors = [...]`

### Validator Coverage Note

The current codebase includes dedicated validators for:

- `RegisterRequest`
- `LoginRequest`
- `RefreshTokenRequest`
- `RevokeTokenRequest`
- `EmailRequest`
- `VerifyEmailRequest`
- `ForgotPasswordRequest`
- `ResetPasswordRequest`
- `ChangePasswordRequest`

Frontend should still mirror these rules client-side for better UX, but request validation is now enforced server-side for these DTOs.

---

# API Endpoints

---

# 1. Authentication API

Base route: `/api/Auth`

---

## 1.1 Register User

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Auth/register`
- **Business Use Case:** Creates a new user account, creates the linked domain profile, assigns the default `User` role, and immediately returns JWT access and refresh tokens.

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
|---|---|---:|---|
| `email` | string | Yes | Must be valid email format |
| `password` | string | Yes | Must satisfy password rules |
| `confirmPassword` | string | Yes | Must match `password` |
| `phoneNumber` | string | Yes | International phone format expected |
| `firstName` | string | Yes | Max 100 chars |
| `lastName` | string | Yes | Max 100 chars |
| `familyName` | string | Yes | Max 100 chars |
| `gender` | number | Yes | Enum value: `1=Male`, `2=Female`, `3=Other` |
| `dateOfBirth` | string (`yyyy-MM-dd`) | Yes | Must represent age >= 16 |
| `nationalIdNumber` | string/null | No | If supplied, must be 14 numeric digits and unique |
| `countryCode` | string | Yes | Must be a valid 2-letter country code existing in DB |

### Validation Constraints

- `email`
  - Required
  - Must be valid email format
  - Maximum length: `255`
- `password`
  - Required
  - Minimum length: `8`
  - Must contain at least one uppercase letter
  - Must contain at least one lowercase letter
  - Must contain at least one digit
- `confirmPassword`
  - Must equal `password`
- `phoneNumber`
  - Required
  - Must match regex: `^\+?[1-9]\d{9,14}$`
- `firstName`
  - Required
  - Maximum length: `100`
- `lastName`
  - Required
  - Maximum length: `100`
- `familyName`
  - Required
  - Maximum length: `100`
- `gender`
  - Must be a valid enum value
- `dateOfBirth`
  - Required
  - User must be at least `16` years old
- `nationalIdNumber`
  - Optional
  - If supplied, length must be exactly `14`
  - If supplied, must contain digits only
- `countryCode`
  - Required
  - Must be exactly length `2`
  - Must exist in the `Countries` table

### Business Rules

- Email must not already exist in ASP.NET Identity users.
- National ID must be unique if provided.
- Country must exist.
- The identity user is created first.
- The user is assigned the `User` role.
- A domain user profile is created in the `users` table.
- `IsNationalIdVerified` becomes `true` only if a national ID is supplied.
- `Nationality` is copied from the selected country’s `NationalityName`.
- Registration returns tokens immediately after success.

### 200 Success Response

```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "accessToken": "eyJhbGciOi...",
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
      "profilePictureUrl": null
    }
  },
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 400 Bad Request

#### Validation errors

```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": [
    "Email is required",
    "Password must contain at least one uppercase letter",
    "You must be at least 16 years old"
  ],
  "timestamp": "2026-03-06T12:00:00Z"
}
```

#### Business logic errors returned by service

Possible messages:

- `Passwords do not match`
- `Email already registered`
- `National ID number already registered`
- `Invalid country`
- Identity errors such as:
  - `Passwords must have at least one non alphanumeric character` is **not expected** because the app disables that requirement
  - `Passwords must have at least one digit`
  - `Passwords must have at least one uppercase ('A'-'Z')`
  - `Passwords must have at least one lowercase ('a'-'z')`
  - `Email '...' is already taken`
- Role assignment failures returned as concatenated identity messages
- Generic runtime failure in wrapped transaction:
  - `Registration failed: <exception message>`

### 404 / 409 Domain-Style Outcomes

This endpoint does not currently return HTTP `404` or `409` explicitly.

Business conflicts are returned as **400** with messages such as:

- `Email already registered`
- `National ID number already registered`

---

## 1.2 Login

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Auth/login`
- **Business Use Case:** Authenticates a user by email and password, updates last login timestamp, and returns a fresh access token and refresh token.

### Authentication / Authorization

- **JWT Required:** No
- **Role Required:** None

### Request Payload

```json
{
  "email": "user@example.com",
  "password": "Password123",
  "deviceInfo": "Flutter Android App"
}
```

### Validation Constraints

- `email`
  - Required
  - Must be valid email format
- `password`
  - Required
- `deviceInfo`
  - Optional
  - No explicit server-side validator in open files beyond DTO typing

### Business Rules

- User is located by email in Identity.
- If no user exists, login fails.
- If `IsActive == false`, login fails.
- Password must be correct.
- Linked domain user profile must exist.
- Country data is loaded for profile mapping.
- Last login timestamp is updated.
- A new refresh token record is persisted for the login session.

### 200 Success Response

```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJhbGciOi...",
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
      "profilePictureUrl": null
    }
  },
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 400 Bad Request

Validation errors can occur if request validation is triggered before controller execution:

- `Email is required`
- `Invalid email format`
- `Password is required`

### 401 Unauthorized

Possible service-level messages:

- `Invalid email or password`
- `Account is deactivated`

### 404 / 409 Domain-Style Outcomes

No explicit `404` or `409` returned.

Business-like missing resource case is returned as unauthorized/failure:

- `User profile not found`

### Possible Internal Failure

- `Login failed: <exception message>`

---

## 1.3 Refresh Access Token

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Auth/refresh`
- **Business Use Case:** Exchanges a valid active refresh token for a new access token and a new refresh token. Old token is revoked.

### Authentication / Authorization

- **JWT Required:** No
- **Role Required:** None

### Request Payload

```json
{
  "refreshToken": "base64-refresh-token"
}
```

### Validation Constraints

- `refreshToken`
  - Required

### Business Rules

- Refresh token is hashed with SHA-256 before lookup.
- Token must exist in `refresh_tokens`.
- Token must be active (not expired and not revoked).
- Linked domain user must exist.
- Existing refresh token is revoked.
- New access token and refresh token are created.

### 200 Success Response

```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "data": {
    "accessToken": "eyJhbGciOi...",
    "refreshToken": "new-base64-refresh-token",
    "expiresAt": "2026-03-06T13:00:00Z",
    "user": {
      "userId": 15,
      "email": "user@example.com",
      "fullName": "Ahmed Mohamed Hassan",
      "phoneNumber": "+201234567890",
      "gender": "Male",
      "countryCode": "EG",
      "countryName": "Egyptian",
      "profilePictureUrl": null
    }
  },
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 400 Bad Request

Validation failure:

- `Refresh token is required`

### 401 Unauthorized

Possible service-level messages:

- `Invalid token`
- `Token is expired or revoked`

### 404 / 409 Domain-Style Outcomes

No explicit `404` or `409`.

Possible missing user case returned as failure:

- `User not found`

### Possible Internal Failure

- `Token refresh failed: <exception message>`

---

## 1.4 Revoke Specific Refresh Token

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Auth/revoke`
- **Business Use Case:** Logs out a device/session by revoking a specific refresh token.

### Authentication / Authorization

- **JWT Required:** No
- **Role Required:** None

### Request Payload

```json
{
  "refreshToken": "base64-refresh-token"
}
```

### Validation Constraints

- `refreshToken`
  - Required
  - Validation message: `Refresh token is required.`

### Business Rules

- Token is hashed then matched in storage.
- Token must exist and be active.
- Token is marked revoked and save is persisted.

### 200 Success Response

```json
{
  "success": true,
  "message": "Token revoked successfully",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 400 Bad Request

Validation or service-level messages may include:

- `Validation failed` with:
  - `Refresh token is required.`
- `Invalid token`
- `Token revocation failed: <exception message>`

### 404 / 409 Domain-Style Outcomes

No explicit `404` or `409`.

---

## 1.5 Revoke All User Tokens

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Auth/revoke-all`
- **Business Use Case:** Logs the current authenticated user out from all devices by revoking all active refresh tokens for that identity user.

### Authentication / Authorization

- **JWT Required:** Yes
- **Role Required:** None

### Request Payload

No request body.

### Business Rules

- User id is extracted from JWT claim `ClaimTypes.NameIdentifier`.
- If token is missing/invalid or cannot be parsed to int, request is unauthorized.
- All active refresh tokens for that identity user are revoked.

### 200 Success Response

```json
{
  "success": true,
  "message": "Revoked 3 token(s)",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 401 Unauthorized

```json
{
  "success": false,
  "message": "Invalid token",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 400 Bad Request

Possible service-level message:

- `Failed to revoke tokens: <exception message>`

---

## 1.6 Get Current Authenticated User

### Endpoint Overview

- **Method:** `GET`
- **URL:** `/api/Auth/me`
- **Business Use Case:** Returns basic details extracted directly from the current JWT and all token claims for debugging/profile bootstrapping.

### Authentication / Authorization

- **JWT Required:** Yes
- **Role Required:** None

### Request Payload

No request body.

### Business Rules

- Data is taken from JWT claims, not from a database lookup.
- `userId` returned here is the `domain_user_id` claim, not the identity user id.

### 200 Success Response

```json
{
  "success": true,
  "message": "Operation successful",
  "data": {
    "userId": "15",
    "email": "user@example.com",
    "name": "Ahmed Hassan",
    "claims": [
      {
        "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        "value": "7"
      },
      {
        "type": "domain_user_id",
        "value": "15"
      }
    ]
  },
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 401 Unauthorized

Standard JWT authentication failure if token is missing/invalid/expired.

---

## 1.7 Send Verification Email

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Auth/send-verification-email`
- **Business Use Case:** Sends an email confirmation link to a user who has not yet confirmed their email.

### Authentication / Authorization

- **JWT Required:** No
- **Role Required:** None

### Request Payload

```json
{
  "email": "user@example.com"
}
```

### Validation Constraints

- `email`
  - Required
  - Must be valid email format
  - Validation messages:
    - `Email is required.`
    - `Please provide a valid email address format.`

### Business Rules

- User must exist.
- Email must not already be confirmed.
- Identity email confirmation token is generated.
- Verification link is currently built using localhost URL.
- Email sending must succeed via configured email service.

### 200 Success Response

```json
{
  "success": true,
  "message": "Verification email sent successfully",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 400 Bad Request

Validation or business messages may include:

- `Validation failed` with:
  - `Email is required.`
  - `Please provide a valid email address format.`
- `User not found`
- `Email already verified`
- `Failed to send verification email`
- `Failed to send verification email: <exception message>`

---

## 1.8 Verify Email

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Auth/verify-email`
- **Business Use Case:** Confirms a user’s email using the token generated by Identity.

### Authentication / Authorization

- **JWT Required:** No
- **Role Required:** None

### Request Payload

```json
{
  "userId": "7",
  "token": "CfDJ8..."
}
```

### Validation Constraints

- `userId`
  - Required
  - Validation message: `User ID is required.`
- `token`
  - Required
  - Validation message: `Verification token is required.`

### Business Rules

- `userId` must be parseable to integer.
- User must exist.
- User must not already be email-confirmed.
- Token must be valid for confirmation.

### 200 Success Response

```json
{
  "success": true,
  "message": "Email verified successfully",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 400 Bad Request

Validation or business messages may include:

- `Validation failed` with:
  - `User ID is required.`
  - `Verification token is required.`
- `Invalid user ID`
- `User not found`
- `Email already verified`
- `Email verification failed`
- `Email verification failed: <exception message>`

---

## 1.9 Forgot Password

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Auth/forgot-password`
- **Business Use Case:** Starts password reset flow by sending a reset link if the email is registered.

### Authentication / Authorization

- **JWT Required:** No
- **Role Required:** None

### Request Payload

```json
{
  "email": "user@example.com"
}
```

### Validation Constraints

- `email`
  - Required
  - Must be valid email format
  - Validation messages:
    - `Email is required.`
    - `Please provide a valid email address format.`

### Business Rules

- If user does not exist, backend still returns success-like message to prevent email enumeration.
- If user exists, Identity reset token is generated.
- Reset email is sent through email service.

### 200 Success Response

Controller intentionally always returns `200`.

Typical response:

```json
{
  "success": true,
  "message": "If your email is registered, you will receive a password reset link",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 400 Bad Request

Validation failures may include:

- `Validation failed` with:
  - `Email is required.`
  - `Please provide a valid email address format.`

### Business Error Notes

Even when email does not exist, the public response remains success-oriented.

Internal service may produce messages such as:

- `Failed to send password reset email`
- `Password reset request failed: <exception message>`

However, controller still wraps returned message into `200 OK`.

---

## 1.10 Reset Password

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Auth/reset-password`
- **Business Use Case:** Resets user password using the email + reset token flow.

### Authentication / Authorization

- **JWT Required:** No
- **Role Required:** None

### Request Payload

```json
{
  "email": "user@example.com",
  "token": "CfDJ8...",
  "newPassword": "NewPassword123",
  "confirmPassword": "NewPassword123"
}
```

### Validation Constraints

- `email`
  - Required
  - Must be valid email format
- `token`
  - Required
- `newPassword`
  - Required
  - Minimum length: `8`
  - Must contain at least one uppercase letter
  - Must contain at least one lowercase letter
  - Must contain at least one digit
- `confirmPassword`
  - Must equal `newPassword`
- Controller also re-checks password equality explicitly before service call.

### Business Rules

- User must exist.
- Reset token must be valid.
- Password must satisfy Identity rules.

### 200 Success Response

```json
{
  "success": true,
  "message": "Password reset successfully",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 400 Bad Request

Validation or business messages may include:

- `Email is required`
- `Invalid email format`
- `Reset token is required`
- `New password is required`
- `Password must be at least 8 characters`
- `Password must contain at least one uppercase letter`
- `Password must contain at least one lowercase letter`
- `Password must contain at least one digit`
- `Passwords do not match`
- `Invalid request`
- Identity reset errors concatenated in a single message
- `Password reset failed: <exception message>`

---

## 1.11 Change Password

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Auth/change-password`
- **Business Use Case:** Allows the authenticated user to change their current password.

### Authentication / Authorization

- **JWT Required:** Yes
- **Role Required:** None

### Request Payload

```json
{
  "currentPassword": "OldPassword123",
  "newPassword": "NewPassword123",
  "confirmPassword": "NewPassword123"
}
```

### Validation Constraints

- `currentPassword`
  - Required
- `newPassword`
  - Required
  - Minimum length: `8`
  - Must contain at least one uppercase letter
  - Must contain at least one lowercase letter
  - Must contain at least one digit
- `confirmPassword`
  - Must equal `newPassword`
- Controller also explicitly checks password equality.

### Business Rules

- User id comes from JWT `ClaimTypes.NameIdentifier`.
- If claim is missing/invalid, request is unauthorized.
- Identity user must exist.
- Current password must be correct.
- New password must satisfy Identity rules.

### 200 Success Response

```json
{
  "success": true,
  "message": "Password changed successfully",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 401 Unauthorized

Possible controller-level message:

- `Invalid token`

### 400 Bad Request

Possible messages:

- `Current password is required`
- `New password is required`
- `Password must be at least 8 characters`
- `Password must contain at least one uppercase letter`
- `Password must contain at least one lowercase letter`
- `Password must contain at least one digit`
- `Passwords do not match`
- `User not found`
- Identity change-password errors as concatenated text
- `Password change failed: <exception message>`

---

# 2. Countries API

Base route: `/api/Countries`

---

## 2.1 Get Countries

### Endpoint Overview

- **Method:** `GET`
- **URL:** `/api/Countries`
- **Business Use Case:** Returns all available countries for registration dropdowns and client-side user profile forms.

### Authentication / Authorization

- **JWT Required:** No
- **Role Required:** None

### Request Payload

No request body.

### Business Rules

- Countries are ordered alphabetically by `CountryName`.
- Returned country objects include booking capability flag (`AllowsTrainBooking`).

### 200 Success Response

```json
{
  "success": true,
  "message": "Operation successful",
  "data": [
    {
      "countryCode": "EG",
      "countryName": "Egypt",
      "nationalityName": "Egyptian",
      "phoneCode": "+20",
      "allowsTrainBooking": true
    },
    {
      "countryCode": "SA",
      "countryName": "Saudi Arabia",
      "nationalityName": "Saudi",
      "phoneCode": "+966",
      "allowsTrainBooking": false
    }
  ],
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### Country Object Reference

| Field | Type | Notes |
|---|---|---|
| `countryCode` | string | ISO-like 2-letter code |
| `countryName` | string | Display country name |
| `nationalityName` | string | Nationality label used in profile data |
| `phoneCode` | string/null | International dialing prefix |
| `allowsTrainBooking` | boolean | Whether train booking is available for that country |

### Error Codes

- No custom business errors are implemented in the open controller.
- Unhandled DB/server failures would surface as `500 ProblemDetails`.

---

# 3. Data Seeder API

Base route: `/api/Seed`

> These endpoints are operational/seeding endpoints intended for development or controlled setup workflows.
> They are currently **not protected** by authentication in the open controllers.

---

## 3.1 Import GoBus CSV Data

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Seed/import-gobus`
- **Business Use Case:** Imports GoBus stations, agencies, coach classes, a shared daily calendar, and GoBus trip blueprints from CSV files.

### Authentication / Authorization

- **JWT Required:** No
- **Role Required:** None

### Request Payload

No request body.

### Business Rules

- Reads files from:
  - `AppContext.BaseDirectory/Data/SeedData/GoBus`
- Required files:
  - `stations.csv`
  - `agencies.csv`
  - `coach_classes.csv`
  - `normalized_trips.csv`
- Import behavior:
  - Stops are deduplicated by `StopName + City`
  - Agencies are deduplicated by `AgencyName`
  - Coach classes are deduplicated by `CoachClassId`
  - A daily calendar is created if none exists
  - Trips are grouped by:
    - origin station
    - destination station
    - service class
    - departure time
  - Existing trip blueprints are skipped if already present
  - No occurrences/inventories are created here; they remain `0`

### 200 Success Response

```json
{
  "success": true,
  "message": "GoBus import completed successfully!",
  "stopsCreated": 120,
  "agenciesCreated": 1,
  "coachClassesCreated": 4,
  "calendarsCreated": 1,
  "tripsCreated": 350,
  "occurrencesCreated": 0,
  "inventoriesCreated": 0
}
```

### 400 Bad Request

Returned when importer reports failure:

```json
{
  "success": false,
  "message": "Import failed: <reason>",
  "stopsCreated": 0,
  "agenciesCreated": 0,
  "coachClassesCreated": 0,
  "calendarsCreated": 0,
  "tripsCreated": 0,
  "occurrencesCreated": 0,
  "inventoriesCreated": 0
}
```

Possible causes include:

- CSV parse issues
- invalid CSV content
- DB save failures
- missing required related records during import flow

### 404 Not Found

If seed folder does not exist:

```json
{
  "message": "SeedData folder not found at <path>. Make sure the CSV files are set to 'Copy if newer'."
}
```

---

## 3.2 Import Train CSV Data

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/Seed/import-trains`
- **Business Use Case:** Imports train agencies, train types, coach classes, train type coach configuration, stations, trip blueprints, stop times, and class pricing from CSV files.

### Authentication / Authorization

- **JWT Required:** No
- **Role Required:** None

### Request Payload

No request body.

### Business Rules

- Reads files from:
  - `AppContext.BaseDirectory/Data/SeedData/ENR`
- Required files:
  - `agencies.csv`
  - `train_types.csv`
  - `coach_classes.csv`
  - `train_type_coach_config.csv`
  - `stations_final.csv`
  - `trips.csv`
  - `trip_stop_times.csv`
  - `trip_class_pricing.csv`
- Import behavior:
  - Existing agencies are deduplicated by name
  - Existing train types are deduplicated by name
  - Existing coach classes are deduplicated by name
  - Existing stations are deduplicated by `StopName + City`
  - Arabic digits in departure time are normalized before parsing
  - Calendar is created if none exists
  - Train type coach config rows are skipped if the combination already exists

### 200 Success Response

The endpoint now returns a standard `ApiResponse<object>` wrapper.

```json
{
  "success": true,
  "message": "Train blueprints imported successfully!",
  "data": {
    "success": true,
    "message": "Train blueprints imported successfully!",
    "stopsCreated": 85,
    "tripsCreated": 420,
    "tripStopTimesCreated": 2100,
    "tripClassPricingsCreated": 680
  },
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

### 400 Bad Request

```json
{
  "success": false,
  "message": "<import failure message>",
  "data": {
    "success": false,
    "message": "<import failure message>",
    "stopsCreated": 0,
    "tripsCreated": 0,
    "tripStopTimesCreated": 0,
    "tripClassPricingsCreated": 0
  },
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

Possible causes include:

- CSV parse issues
- invalid CSV content
- DB save failures
- invalid file contents
- missing related mappings during import

### 404 Not Found

If seed folder does not exist, the endpoint now returns a standard wrapper:

```json
{
  "success": false,
  "message": "SeedData folder not found at <path>. Make sure the CSV files are set to 'Copy if newer'.",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-06T12:00:00Z"
}
```

---

# 4. Admin Users API

Base route: `/api/admin/users`

> All endpoints below require a valid JWT Bearer token and the `RequireAdminRole` policy (Admin role).

---

## 4.1 Get All Users

### Endpoint Overview

- **Method:** `GET`
- **URL:** `/api/admin/users`
- **Business Use Case:** Retrieves a complete, alphabetically sorted list of all registered users.

### Authentication / Authorization

- **JWT Required:** Yes
- **Role Required:** Admin (`RequireAdminRole`)

### Request Payload

No request body.

### Business Rules

- Reads all users via the generic repository with `AsNoTracking`.
- Includes the linked `Country` navigation property for each user.
- Results are ordered by first name then last name.

### Expected Responses

#### 200 Success Response

```json
{
  "success": true,
  "message": "Users retrieved successfully.",
  "data": [
    {
      "userId": 15,
      "email": "user@example.com",
      "fullName": "Ahmed Mohamed Hassan",
      "phoneNumber": "+201234567890",
      "gender": "Male",
      "countryCode": "EG",
      "countryName": "Egypt",
      "profilePictureUrl": null
    }
  ],
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```

---

## 4.2 Get User by ID

### Endpoint Overview

- **Method:** `GET`
- **URL:** `/api/admin/users/{id}`
- **Business Use Case:** Retrieves comprehensive user details for the admin dashboard, combining domain profile data with identity authentication statistics.

### Authentication / Authorization

- **JWT Required:** Yes
- **Role Required:** Admin (`RequireAdminRole`)

### Request Payload

No request body.

### Business Rules

- Fetches profile data from the `Users` table (including Country relations).
- Fetches authentication data from the `AspNetUsers` Identity table.
- Both data sets are merged into a single `AdminUserDetailDto`.

### Expected Responses

#### 200 Success Response

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
    "totalDistanceTraveled": 1250.5,
    "createdAt": "2026-03-01T08:30:00Z",
    "lastLoginAt": "2026-03-09T10:15:00Z",
    "isActive": true
  },
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```

#### 404 Not Found

```json
{
  "success": false,
  "message": "User not found.",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```

---

## 4.3 Toggle User Status

### Endpoint Overview

- **Method:** `PATCH`
- **URL:** `/api/admin/users/{id}/toggle-status`
- **Business Use Case:** Soft-bans or unbans a user by flipping their active status flag, preventing or allowing future logins.

### Authentication / Authorization

- **JWT Required:** Yes
- **Role Required:** Admin (`RequireAdminRole`)

### Request Payload

No request body.

### Business Rules

- Locates the Identity user using the `DomainUserId`.
- Flips the `IsActive` boolean flag.
- **Last Admin Safeguard:** If the target user is an Admin and is currently active, the system checks the database to ensure at least one *other* active Admin exists. The system will reject the request if it would result in zero active admins.

### Expected Responses

#### 200 Success Response

```json
{
  "success": true,
  "message": "User disabled successfully.", 
  "data": null,
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```

#### 400 Bad Request

```json
{
  "success": false,
  "message": "Cannot deactivate the last active Admin account.",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```

#### 404 Not Found

```json
{
  "success": false,
  "message": "User not found.",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```

---

## 4.4 Assign Role

### Endpoint Overview

- **Method:** `POST`
- **URL:** `/api/admin/users/{id}/roles`
- **Business Use Case:** Promotes a standard user to a specific system role, such as Admin.

### Authentication / Authorization

- **JWT Required:** Yes
- **Role Required:** Admin (`RequireAdminRole`)

### Request Payload

```json
{
  "role": "Admin"
}
```

### Business Rules

- Validates that the requested role string exists in the Identity `AspNetRoles` table using `RoleManager`.
- Checks if the user already possesses the role to prevent duplicate assignments.
- Uses `UserManager` to apply the new role.

### Expected Responses

#### 200 Success Response

```json
{
  "success": true,
  "message": "Role assigned successfully.",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```

#### 400 Bad Request

```json
{
  "success": false,
  "message": "Role is required.",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```

#### 404 Not Found

```json
{
  "success": false,
  "message": "Role does not exist.",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```

---

## 4.5 Delete User

### Endpoint Overview

- **Method:** `DELETE`
- **URL:** `/api/admin/users/{id}`
- **Business Use Case:** Permanently deletes a user from both the Identity system and the Domain database.

### Authentication / Authorization

- **JWT Required:** Yes
- **Role Required:** Admin (`RequireAdminRole`)

### Request Payload

No request body.

### Business Rules

- Deletion is executed within a transaction to guarantee Identity and Domain consistency.
- A user **cannot** be deleted if they have associated records in the `Bookings` table.

### Expected Responses

#### 200 Success Response

```json
{
  "success": true,
  "message": "User deleted successfully.",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```

#### 404 Not Found

```json
{
  "success": false,
  "message": "User not found.",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```

#### 409 Conflict

```json
{
  "success": false,
  "message": "User cannot be deleted because related bookings exist.",
  "data": null,
  "errors": null,
  "timestamp": "2026-03-09T12:00:00Z"
}
```


---

# Additional Frontend Notes


## 1. Token Expiration Handling

JWT middleware appends the response header below when authentication fails due to expired token:

```http
Token-Expired: true
```

Useful for Flutter/Angular interceptors.

## 2. Email Verification Requirement

Identity is configured with:

- `RequireConfirmedEmail = false`

So users can log in even if email is not verified.

## 3. Password Policy

Identity configuration enforces:

- minimum length `8`
- uppercase required
- lowercase required
- digit required
- non-alphanumeric **not required**

This matches the validators currently visible for password-based DTOs.

## 4. Rate Limiting

A fixed window limiter is registered with:

- window: `1 minute`
- permit limit: `10`

The current open `Program.cs` applies `app.UseRateLimiter()`. Frontend should be prepared for possible throttling responses if endpoint mapping is later tied to a limiter policy.

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
| `POST` | `/api/Seed/import-gobus` | No | Import GoBus CSV data |
| `POST` | `/api/Seed/import-trains` | No | Import Train CSV data |
| `GET` | `/api/admin/users` | Yes | List all domain users alongside their country metadata |
| `GET` | `/api/admin/users/{id}` | Yes | View complete details of a specific user |
| `PATCH` | `/api/admin/users/{id}/toggle-status` | Yes | Suspend or activate a user account |
| `POST` | `/api/admin/users/{id}/roles` | Yes | Assign a system role to a user |
| `DELETE` | `/api/admin/users/{id}` | Yes | Permanently delete a user |
