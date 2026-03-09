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
  "type": "[https://tools.ietf.org/html/rfc7231#section-6.6.1](https://tools.ietf.org/html/rfc7231#section-6.6.1)",
  "title": "Server Error",
  "status": 500,
  "detail": "Unexpected error message"
}
```

---

## Authentication Notes

### JWT Behavior
The API uses JWT Bearer authentication. Send the token in the header:
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
- Authentication endpoints use anonymous access or authenticated access depending on the route.
- Admin user management endpoints require the `RequireAdminRole` policy (Admin role).

---

# API Endpoints

---

# 1. Authentication API
Base route: `/api/Auth`

## 1.1 Register User
- **Method:** `POST`
- **URL:** `/api/Auth/register`
- **Auth:** None
- **Payload:** User registration details (email, password, name, phone, gender, dob, national ID, country code).
- **Response:** 200 OK with `accessToken`, `refreshToken`, and user object.

## 1.2 Login
- **Method:** `POST`
- **URL:** `/api/Auth/login`
- **Auth:** None
- **Payload:** `{ "email": "...", "password": "...", "deviceInfo": "..." }`
- **Response:** 200 OK with tokens and user object.

## 1.3 Refresh Access Token
- **Method:** `POST`
- **URL:** `/api/Auth/refresh`
- **Auth:** None
- **Payload:** `{ "refreshToken": "..." }`
- **Response:** 200 OK with fresh tokens.

## 1.4 Revoke Specific Refresh Token
- **Method:** `POST`
- **URL:** `/api/Auth/revoke`
- **Auth:** None
- **Payload:** `{ "refreshToken": "..." }`

## 1.5 Revoke All User Tokens
- **Method:** `POST`
- **URL:** `/api/Auth/revoke-all`
- **Auth:** Yes (Bearer)

## 1.6 Get Current Authenticated User (Claims)
- **Method:** `GET`
- **URL:** `/api/Auth/me`
- **Auth:** Yes (Bearer)
- **Response:** 200 OK returning raw JWT claims.

## 1.7 Send Verification Email
- **Method:** `POST`
- **URL:** `/api/Auth/send-verification-email`
- **Auth:** None

## 1.8 Verify Email
- **Method:** `POST`
- **URL:** `/api/Auth/verify-email`
- **Auth:** None
- **Payload:** `{ "userId": "...", "token": "..." }`

## 1.9 Forgot Password
- **Method:** `POST`
- **URL:** `/api/Auth/forgot-password`
- **Auth:** None

## 1.10 Reset Password
- **Method:** `POST`
- **URL:** `/api/Auth/reset-password`
- **Auth:** None
- **Payload:** `{ "email": "...", "token": "...", "newPassword": "...", "confirmPassword": "..." }`

## 1.11 Change Password
- **Method:** `POST`
- **URL:** `/api/Auth/change-password`
- **Auth:** Yes (Bearer)
- **Payload:** `{ "currentPassword": "...", "newPassword": "...", "confirmPassword": "..." }`

---

# 2. Countries API
Base route: `/api/Countries`

## 2.1 Get Countries
- **Method:** `GET`
- **URL:** `/api/Countries`
- **Auth:** None
- **Response:** 200 OK. Returns complete list of countries including `phoneCode` and `allowsTrainBooking` flags.

---

# 3. Data Seeder API
Base route: `/api/Seed`

## 3.1 Import GoBus CSV Data
- **Method:** `POST`
- **URL:** `/api/Seed/import-gobus`
- **Auth:** None

## 3.2 Import Train CSV Data
- **Method:** `POST`
- **URL:** `/api/Seed/import-trains`
- **Auth:** None

---

# 4. Admin Users API
Base route: `/api/admin/users`
*(Requires JWT Bearer token and `RequireAdminRole` policy)*

## 4.1 Get All Users
- **Method:** `GET`
- **URL:** `/api/admin/users`
- **Response:** 200 OK with alphabetically sorted array of all domain users.

## 4.2 Get User by ID
- **Method:** `GET`
- **URL:** `/api/admin/users/{id}`
- **Response:** 200 OK returning `AdminUserDetailDto` (merged domain + identity data).

## 4.3 Toggle User Status
- **Method:** `PATCH`
- **URL:** `/api/admin/users/{id}/toggle-status`
- **Business Rule:** Rejects deactivating the last active Admin.

## 4.4 Assign Role
- **Method:** `POST`
- **URL:** `/api/admin/users/{id}/roles`
- **Payload:** `{ "role": "Admin" }`

## 4.5 Delete User
- **Method:** `DELETE`
- **URL:** `/api/admin/users/{id}`
- **Business Rule:** Fails (409 Conflict) if user has existing Bookings.

---

# 5. User Profile API
Base route: `/api/users`
*(Requires JWT Bearer token)*

## 5.1 Get My Profile
- **Method:** `GET`
- **URL:** `/api/Users/me`
- **Business Use Case:** Retrieve the authenticated user's profile, including gamification stats and digital wallet balance.
- **Response (200):**
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
    "totalTripsCount": 12,
    "totalDistanceTraveled": 345.5,
    "walletBalance": 50.00
  },
  "errors": null,
  "timestamp": "2026-03-10T00:00:00Z"
}
```

## 5.2 Update My Profile
- **Method:** `PUT`
- **URL:** `/api/Users/me`
- **Business Use Case:** Updates the current user's personal details.
- **Payload (`application/json`):**
```json
{
  "firstName": "Ahmed",
  "familyName": "Mohamed",
  "lastName": "Hassan",
  "email": "new-email@example.com",
  "phoneNumber": "+201234567891"
}
```
- **Business Rules:** - Domain and Identity stores are updated transactionally.
  - If `email` is updated, uniqueness is verified across the system, and `EmailConfirmed` is revoked.

## 5.3 Upload Profile Picture
- **Method:** `POST`
- **URL:** `/api/Users/me/profile-picture`
- **Business Use Case:** Uploads or replaces the user's profile picture.
- **Payload (`multipart/form-data`):** Requires a single file attached to the key `file`.
- **Allowed Extensions:** `.jpg`, `.jpeg`, `.png`
- **Response (200):**
```json
{
  "success": true,
  "message": "Profile picture uploaded successfully.",
  "data": {
    "profilePictureUrl": "images/profiles/abcd.jpg"
  },
  "errors": null,
  "timestamp": "2026-03-10T00:00:00Z"
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
Identity is configured with `RequireConfirmedEmail = false`. Users can log in even if email is not verified.

## 3. Rate Limiting
A fixed window limiter is registered with:
- Window: `1 minute`
- Permit limit: `10`
Frontend should be prepared for possible `429 Too Many Requests` responses.

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
| `GET` | `/api/admin/users` | Yes | List all domain users alongside country metadata |
| `GET` | `/api/admin/users/{id}` | Yes | View complete details of a specific user |
| `PATCH`| `/api/admin/users/{id}/toggle-status` | Yes | Suspend or activate a user account |
| `POST` | `/api/admin/users/{id}/roles` | Yes | Assign a system role to a user |
| `DELETE`| `/api/admin/users/{id}` | Yes | Permanently delete a user |
| `GET` | `/api/users/me` | Yes | Get the logged-in user's profile |
| `PUT` | `/api/users/me` | Yes | Update the logged-in user's profile |
| `POST` | `/api/users/me/profile-picture` | Yes | Upload user profile picture |