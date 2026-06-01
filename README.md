# Rehla - Integrated Ticketing Platform for Inter-Governorate Travel in Egypt

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet)
![EF Core](https://img.shields.io/badge/EF%20Core-9.0-512BD4?style=flat)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=flat&logo=microsoftsqlserver)


> **University Graduation Project**  
> A unified backend API for booking inter-governorate transportation across Egypt, supporting both **Egyptian National Railways (ENR)** and **GoBus** services.

---

## 📋 Project Overview

**Rehla** (رحلة - Arabic for "Journey") is a comprehensive ticketing platform designed to simplify inter-governorate travel booking in Egypt. This ASP.NET Core Web API serves as the backend, providing:

- **Unified Booking System** - Book train and bus tickets through a single platform
- **Flexible Search Engine** - Governorate-to-governorate OR station-to-station search with date/transport filters
- **Advanced 1-Stop Indirect Routing Algorithm via Spatial Bounding Boxes** - Finds transfer hubs efficiently and builds valid connecting itineraries
- **Dynamic SQL-level Filtering & Sorting** - Supports transport mode, preferred agencies, max price, and sort strategies without client-side filtering
- **Dynamic Seat Availability Filtering** - Returns only classes with enough remaining seats for requested passenger count
- **Bilingual Station Directory** - Grouped stations by governorate for Arabic/English dropdown experiences
- **User Authentication** - Secure JWT-based authentication with email verification
- **Role-based Access Control** - Admin, User, and Partner roles
- **Booking Management** - Complete booking lifecycle with passenger management
- **Refund Requests** - Users can request refunds; admins approve/reject with wallet refunds and notifications
- **Loyalty & Gamification** - Points ledger, monthly challenges, and progress tracking
- **Real-Time Notifications + Inbox** - Persistent user inbox plus SignalR live push for marketplace sales, gamification rewards, and boarding alerts

### 🎯 Loyalty & Gamification Highlights

- Points redemption is capped at 50% of the checkout total
- Earned points remain pending until departure and expire 4 months later
- Monthly challenges reset and reseed via the Jobs endpoints
- Challenge types include TotalTrips, TotalSpend, RoundTrip, and MultiDestination

### 🔔 Notification Highlights

- Every notification is persisted to a user inbox history and can be marked as read
- Marketplace sellers receive a real-time `Ticket Sold!` notification when a listing is purchased
- Users receive real-time `Points Earned! 🎉` notifications for checkout-earned points and challenge rewards
- Confirmed passengers receive one-time `Boarding Soon!` notifications via cron-driven job processing 15 minutes before boarding
- FCM notification title/body is localized using the user's preferred language (defaults to `en`)

---

## ⏱️ Timing & Timezone Rules

This project uses a unified timing wrapper: `GP.Application/Common/AppTime.cs`.

### Standard

- **Schedule-local business time (Egypt/Cairo):** Use `AppTime.GetScheduleNow()`.
- **Absolute UTC instants:** Use `DateTime.UtcNow`.

### When to Use Each

- Use `AppTime.GetScheduleNow()` when comparing against timetable/business fields such as:
  - `DepartureDateTime`
  - `ArrivalDateTime`
  - `UnlocksAt`
  - `travelDate` boundaries
- Use `DateTime.UtcNow` for cross-system absolute instants such as:
  - API wrapper timestamps
  - token/identity expiry and revocation times
  - audit timestamps (`CreatedAt`, `UpdatedAt`)

### Serialization Contract

- UTC fields are serialized with `Z`.
- Schedule-local timetable fields are serialized without timezone suffix.

### Important Guardrail

- Do not compare schedule-local database values directly with `DateTime.UtcNow`.
- Convert "now" to schedule-local via `AppTime.GetScheduleNow()` first, then compare.

---

## 🏗️ Architecture

This solution follows **Clean Architecture** principles with clear separation of concerns:

```
GP/
├── GP.Api/     # Presentation Layer - Controllers, Middleware, Configuration
├── GP.Application/     # Application Layer - Services, DTOs, Interfaces, Validators
├── GP.Domain/      # Domain Layer - Entities, Enums, Business Logic
└── GP.Infrastructure/  # Infrastructure Layer - EF Core, Identity, External Services
```

---

## 🛠️ Tech Stack

| Category              | Technology                |
| --------------------- | ------------------------- |
| **Framework**         | ASP.NET Core 9.0          |
| **ORM**               | Entity Framework Core 9.0 |
| **Database**          | SQL Server                |
| **Authentication**    | JWT Bearer Tokens         |
| **Identity**          | ASP.NET Core Identity     |
| **Email Service**     | SendGrid                  |
| **Validation**        | FluentValidation          |
| **API Documentation** | OpenAPI / Scalar UI       |

---

## 📦 Prerequisites

Before running the project, ensure you have the following installed:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server 2019+](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB or full instance)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (v17.8+) or [VS Code](https://code.visualstudio.com/) with C# DevKit
- [Git](https://git-scm.com/)

---

## 🚀 Getting Started

Follow these steps to get the project running on your local machine:

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/Integrated-Ticketing-API.git
cd Integrated-Ticketing-API
```

### 2. Configure User Secrets

To run this application, you need to configure your own API keys. For security, this project uses **User Secrets** instead of hardcoding keys in `appsettings.json`.

> 💡 **Internal Team Members:** You can request the shared development keys (SendGrid & JWT) directly from the project owner.
>
> 🌍 **External Developers:** You will need to provide your own SendGrid API key or your own SMTP server credentials and generate your own random JWT Secret.

Configure the keys using the .NET CLI:

```bash
cd GP.Api
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "<YOUR_JWT_SECRET_KEY>"
dotnet user-secrets set "EmailSettings:Password" "<YOUR_SENDGRID_API_KEY>"
```

Or in Visual Studio:

1. Right-click on `GP.Api` project
2. Select **Manage User Secrets**
3. Add the following JSON:

```json
{
  "JwtSettings": {
    "SecretKey": "<YOUR_JWT_SECRET_KEY>"
  },
  "EmailSettings": {
    "Password": "<YOUR_SENDGRID_API_KEY>"
  }
}
```

### 3. Update the Database Connection String (if needed)

The default connection string in `appsettings.json` uses Windows Authentication. Edit the connection string if your SQL Server instance differs:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=.;Initial Catalog=GPdatabase;Integrated Security=True;Encrypt=False;TrustServerCertificate=True"
}
```

Modify this if your SQL Server instance has a different name or requires SQL Authentication.

### 4. Apply Database Migrations

Open **Package Manager Console** in Visual Studio (Tools → NuGet Package Manager → Package Manager Console):

```powershell
Update-Database -Project GP.Infrastructure -StartupProject GP.Api
```

Or using the .NET CLI:

```bash
dotnet ef database update --project GP.Infrastructure --startup-project GP.Api
```

> ✅ This will create the database and automatically run the **DbInitializer**, which:
> - Seeds all countries
> - Creates default roles (Admin, User, Partner)
> - Creates the default Admin user (`admin@gp.com` / `Admin@123456`)

### 5. Run the Application

```bash
dotnet run --project GP.Api
```

Or press **F5** in Visual Studio.

Once the application is running, look at your terminal (or Visual Studio output) to see which local ports were assigned. It will look something like this:

```
Now listening on: https://localhost:<PORT>
```

You can view the interactive API Documentation at:

```
https://localhost:<PORT>/scalar/v1
```

### 6. Seed Transportation Data

After the application is running, you **MUST** call the seed endpoints in the recommended order to import master stations and agency trips. Recommended sequence:

1. Initialize Identity

```http
POST /api/Seed/init-identity
```

2. Import Master Stations

```http
POST /api/Seed/import-master-stations
```

3. Agency imports (order between agency imports is flexible, but master stations must exist first)

```http
POST /api/Seed/import-horus
POST /api/Seed/import-bluebus
POST /api/Seed/import-gobus
POST /api/Seed/import-trains
```

You can call these endpoints using:

- **Scalar UI** (built-in API documentation)
- **Postman** or any HTTP client
- **curl**:

```bash
curl -X POST https://localhost:<PORT>/api/Seed/import-gobus
curl -X POST https://localhost:<PORT>/api/Seed/import-trains
```

> 📁 The seed files are located in the API project `GP.Api/Data/SeedData/`:
> - `GP.Api/Data/SeedData/Master_stations.json`
> - `GP.Api/Data/SeedData/gobus_trips.json`
> - `GP.Api/Data/SeedData/Horus_trips.json`
> - `GP.Api/Data/SeedData/bluebus_trips.json`
> - `GP.Api/Data/SeedData/train_stops.json`
> - `GP.Api/Data/SeedData/trains_trips.json`
>
> *Note: Ensure these files are set to **"Copy if newer"** in Visual Studio properties so they are automatically moved to the output directory upon build.*

---

## 🔐 Default Admin Credentials

After running migrations, you can log in with the seeded admin account:

| Field        | Value          |
| ------------ | -------------- |
| **Email**    | `admin@gp.com` |
| **Password** | `Admin@123456` |

> ⚠️ Change these credentials in production!

---

## 📚 API Endpoints Overview

### 🔐 Authentication (`/api/Auth`)

| Method | Endpoint                            | Description                                    | Auth Required |
| ------ | ----------------------------------- | ---------------------------------------------- | ------------- |
| `POST` | `/api/Auth/register`                | Register a new user                            | ❌             |
| `POST` | `/api/Auth/login`                   | Login with email and password                  | ❌             |
| `POST` | `/api/Auth/refresh`                 | Refresh access token using refresh token       | ❌             |
| `POST` | `/api/Auth/revoke`                  | Revoke a specific refresh token (logout)       | ❌             |
| `POST` | `/api/Auth/revoke-all`              | Revoke all refresh tokens (logout all devices) | ✅             |
| `GET`  | `/api/Auth/me`                      | Get current authenticated user info            | ✅             |
| `POST` | `/api/Auth/send-verification-email` | Send email verification link                   | ❌             |
| `POST` | `/api/Auth/verify-email`            | Verify email address with token                | ❌             |
| `POST` | `/api/Auth/forgot-password`         | Request password reset email                   | ❌             |
| `POST` | `/api/Auth/reset-password`          | Reset password with token                      | ❌             |
| `POST` | `/api/Auth/change-password`         | Change password for authenticated user         | ✅             |

### 🌍 Countries (`/api/Countries`)

| Method | Endpoint         | Description                                   | Auth Required |
| ------ | ---------------- | --------------------------------------------- | ------------- |
| `GET`  | `/api/Countries` | Get all countries (for registration dropdown) | ❌             |

### 🔎 Search (`/api/trips` preferred, `/api/Search` alias)

| Method | Endpoint                     | Description                                                                | Auth Required |
| ------ | ---------------------------- | -------------------------------------------------------------------------- | ------------- |
| `GET`  | `/api/trips/search`          | Preferred paginated direct-trip search endpoint (`pageNumber`, `pageSize`) | ❌             |
| `GET`  | `/api/Search`                | Backward-compatible direct-trip search alias                               | ❌             |
| `GET`  | `/api/trips/search/indirect` | Preferred 1-stop indirect search route                                     | ❌             |
| `GET`  | `/api/Search/indirect`       | Backward-compatible indirect search alias                                  | ❌             |
| `GET`  | `/api/Search/popular-routes` | Top 3 governorate-to-governorate routes from the last 7 days (cached 1h)   | ❌             |

### 🪑 Occurrence Seat Map (`/api/occurrences`)

| Method | Endpoint                      | Description                                                                                                      | Auth Required |
| ------ | ----------------------------- | ---------------------------------------------------------------------------------------------------------------- | ------------- |
| `GET`  | `/api/occurrences/{id}/seats` | Real-time seat states (Available/Pending/Booked) plus layout metadata (`layoutType`, `deckCount`, `seatMapJson`) | ❌             |

### 🚉 Stations (`/api/Stations`)

| Method | Endpoint        | Description                                                | Auth Required |
| ------ | --------------- | ---------------------------------------------------------- | ------------- |
| `GET`  | `/api/Stations` | Get bilingual station dropdown data grouped by governorate | ❌             |

### 🌱 Data Seeding (`/api/Seed`)

| Method | Endpoint                           | Description                        | Auth Required |
| ------ | ---------------------------------- | ---------------------------------- | ------------- |
| `POST` | `/api/Seed/init-identity`          | Create default roles + admin user  | ❌ (currently) |
| `POST` | `/api/Seed/import-master-stations` | Upload master stations JSON file   | ❌ (currently) |
| `POST` | `/api/Seed/import-horus`           | Import trips from Horus JSON files | ❌ (currently) |
| `POST` | `/api/Seed/import-bluebus`         | Import Blue Bus trips data         | ❌ (currently) |
| `POST` | `/api/Seed/import-gobus`           | Import GoBus CSV/JSON data         | ❌ (currently) |
| `POST` | `/api/Seed/import-trains`          | Import train CSV/JSON data         | ❌ (currently) |
| `POST` | `/api/Seed/generate-occurrences`   | Generate future occurrences        | ❌ (currently) |

### ⚙️ Jobs (`/api/Jobs`)

| Method | Endpoint                                                   | Description                                           | Auth Required      |
| ------ | ---------------------------------------------------------- | ----------------------------------------------------- | ------------------ |
| `POST` | `/api/Jobs/generate-occurrences?secret=<JobSecretKey>`     | Generate future occurrences (scheduler endpoint)      | Secret query param |
| `POST` | `/api/Jobs/process-completed-trips?secret=<JobSecretKey>`  | Mark eligible trips as completed                      | Secret query param |
| `POST` | `/api/Jobs/release-expired-holds?secret=<JobSecretKey>`    | Release expired holds and restore inventory           | Secret query param |
| `POST` | `/api/Jobs/process-boarding-alerts?secret=<JobSecretKey>`  | Send one-time boarding alerts for trips boarding soon | Secret query param |
| `POST` | `/api/Jobs/expire-points?secret=<JobSecretKey>`            | Expire old loyalty point transactions                 | Secret query param |
| `POST` | `/api/Jobs/reset-monthly-challenges?secret=<JobSecretKey>` | Reset and reassign monthly challenges                 | Secret query param |
| `POST` | `/api/Jobs/seed-challenges?secret=<JobSecretKey>`          | Seed the static monthly challenges                    | Secret query param |

### 🔔 Notifications Inbox (`/api/Notifications`)

| Method  | Endpoint                       | Description                                  | Auth Required |
| ------- | ------------------------------ | -------------------------------------------- | ------------- |
| `GET`   | `/api/Notifications?limit=50`  | Retrieve latest notifications (newest first) | ✅             |
| `PATCH` | `/api/Notifications/{id}/read` | Mark one notification as read                | ✅             |
| `PATCH` | `/api/Notifications/read-all`  | Mark all unread notifications as read        | ✅             |

### 🆘 Support Tickets (`/api/Support`)

| Method | Endpoint               | Description                         | Auth Required |
| ------ | ---------------------- | ----------------------------------- | ------------- |
| `POST` | `/api/Support/tickets` | Create a new support ticket         | ✅             |
| `GET`  | `/api/Support/tickets` | Retrieve user's support ticket list | ✅             |

### 🔔 Real-Time Notifications (SignalR)

- Hub route: `/hubs/notifications`
- Auth: JWT-authenticated connection
- User targeting: routed by `domain_user_id` claim through server-side `Clients.User(userId)`
- Client callback: `ReceiveNotification(title, message, type)`
- Delivery model: notification is saved to inbox first, then pushed live through SignalR
- Offline delivery uses stored FCM tokens registered via `/api/users/fcm-token`
- FCM `notification` title/body is localized using the user's preferred language set via `/api/users/language`

Current notification types:

| Type           | Trigger                                                                |
| -------------- | ---------------------------------------------------------------------- |
| `Marketplace`  | Seller ticket sold in marketplace buy flow                             |
| `Gamification` | Checkout points earned and challenge reward completion                 |
| `Boarding`     | Cron endpoint `/api/Jobs/process-boarding-alerts` for 15-minute alerts |

### 🛡️ Admin Users (`/api/admin/users`)

| Method   | Endpoint                              | Auth Required | Description                                            |
| -------- | ------------------------------------- | ------------- | ------------------------------------------------------ |
| `GET`    | `/api/admin/users`                    | ✅             | List all domain users alongside their country metadata |
| `GET`    | `/api/admin/users/{id}`               | ✅             | View complete details of a specific user               |
| `PATCH`  | `/api/admin/users/{id}/toggle-status` | ✅             | Suspend or activate a user account                     |
| `POST`   | `/api/admin/users/{id}/roles`         | ✅             | Assign a system role to a user                         |
| `DELETE` | `/api/admin/users/{id}`               | ✅             | Permanently delete a user                              |

### 🛡️ Admin Support (`/api/admin/support`)

| Method | Endpoint                                  | Auth Required | Description                         |
| ------ | ----------------------------------------- | ------------- | ----------------------------------- |
| `GET`  | `/api/admin/support/tickets`              | ✅             | List all support tickets            |
| `PUT`  | `/api/admin/support/tickets/{ticketId}/status` | ✅             | Update support ticket status        |

### 🛡️ Admin Bookings (`/api/admin`)

| Method | Endpoint                                | Auth Required | Description                                |
| ------ | --------------------------------------- | ------------- | ------------------------------------------ |
| `GET`  | `/api/admin/bookings/refund-requests`   | ✅             | List booking refund requests               |
| `PUT`  | `/api/admin/bookings/{bookingId}/refund` | ✅             | Approve or reject a pending refund request |

### 🧑‍💻 User Profile 

These endpoints were added as part of the User Profile epic. They allow authenticated users to view and manage their profile and upload a profile picture. All endpoints require a valid JWT (Bearer) token.

#### User Profile (`/api/users`)

| Method | Endpoint                        | Auth Required | Description                                                                                                                                       |
| ------ | ------------------------------- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| `GET`  | `/api/users/me`                 | ✅             | Get current user's profile (including ID type/number and preferred language), loyalty points, active challenges, and wallet balance             |
| `PUT`  | `/api/users/me`                 | ✅             | Update current user's basic profile info (first/family/last name, email, phone). Email & phone uniqueness validated at domain and identity levels |
| `POST` | `/api/users/me/profile-picture` | ✅             | Upload or replace user's profile picture (multipart file). Allowed extensions: `.jpg`, `.jpeg`, `.png`                                            |
| `POST` | `/api/users/fcm-token`          | ✅             | Register or update user's FCM token for offline push notifications                                                                                |
| `PUT`  | `/api/users/language`           | ✅             | Update user's preferred language (`ar` or `en`) for server-side localized push notifications                                                     |

### 🎁 Loyalty (`/api/Loyalty`)

| Method | Endpoint                  | Description                                                     | Auth Required |
| ------ | ------------------------- | --------------------------------------------------------------- | ------------- |
| `GET`  | `/api/Loyalty/history`    | Retrieve the user's loyalty point ledger history (latest first) | ✅             |
| `GET`  | `/api/Loyalty/challenges` | Retrieve paged active and completed challenge history           | ✅             |

### 🛒 Bookings (`/api/Bookings`)

| Method   | Endpoint                                                        | Description                                                                               | Auth Required |
| -------- | --------------------------------------------------------------- | ----------------------------------------------------------------------------------------- | ------------- |
| `POST`   | `/api/Bookings/cart`                                            | Add trip to cart with 10-minute seat soft-lock (one passenger ↔ one required seat number) | ✅             |
| `POST`   | `/api/Bookings/cart/add`                                        | Backward-compatible add-to-cart alias                                                     | ✅             |
| `GET`    | `/api/Bookings/cart`                                            | Get current active cart (pending + not expired)                                           | ✅             |
| `DELETE` | `/api/Bookings/bookings/{bookingId}`                            | Cancel an entire pending booking hold and release all held seats                          | ✅             |
| `POST`   | `/api/Bookings/checkout`                                        | Checkout all pending cart items with one wallet charge                                    | ✅             |
| `GET`    | `/api/Bookings/my-tickets`                                      | Get user's ticket history (non-pending bookings)                                          | ✅             |
| `POST`   | `/api/Bookings/{bookingId}/refund-request`                      | Request a refund for a confirmed booking                                                  | ✅             |
| `GET`    | `/api/Bookings/{bookingId}/passengers/{passengerId}/qr-payload` | Get signed boarding pass QR payload for a passenger                                       | ✅             |
| `POST`   | `/api/Bookings/verify-pass`                                     | Verify scanned boarding pass payload (driver app)                                         | ✅             |

### 🏪 Marketplace (`/api/Marketplace`)

| Method | Endpoint                              | Description                          | Auth Required |
| ------ | ------------------------------------- | ------------------------------------ | ------------- |
| `POST` | `/api/Marketplace/list`               | List a booking for resale            | ✅             |
| `POST` | `/api/Marketplace/listings/{listingId}/buy` | Purchase a listed booking (alias: `/api/Marketplace/buy/{listingId}`) | ✅             |
| `GET`  | `/api/Marketplace/active`             | Retrieve active marketplace listings | ❌             |
| `POST` | `/api/Marketplace/cancel/{listingId}` | Delist a marketplace listing         | ✅             |

### 💳 Wallet (`/api/Wallet`)

| Method | Endpoint              | Description                                                 | Auth Required |
| ------ | --------------------- | ----------------------------------------------------------- | ------------- |
| `POST` | `/api/Wallet/deposit` | Deposit funds into user wallet using simulated card gateway | ✅             |
| `GET`  | `/api/Wallet/history` | Retrieve wallet transaction ledger history (latest first)   | ✅             |


> 📖 For complete API documentation with request/response schemas, see API_SPECIFICATION.md or visit `/scalar/v1` when the application is running.

---

## 👥 Team & Acknowledgements

This project was developed as the backend infrastructure for our graduation project at the **Faculty of Engineering at Shoubra, Benha University**.

We would like to express our gratitude to our project supervisors for their guidance:

- **Assoc. Prof. Eman Ahmed**
- **Dr. May Salama**

**Backend Developers:** Ahmed Mohamed Ali Omar, Mark Wafik Gamal Loka  
**Frontend Developers:** Afraim Elkes Eleia Samy, Abdelrahaman Emad Ibrahim, Mohamed Saeed Atia, Youssef Fawzy Abdelrahman

---

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📞 Contact

For questions or to request API keys, please contact the project owner.

---

<p align="center">
  Made with ❤️ in Egypt 🇪🇬
</p>
