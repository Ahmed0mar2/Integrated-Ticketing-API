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
- **Real-time Trip Search** - Search available trips with filtering by date, origin, destination, and class
- **User Authentication** - Secure JWT-based authentication with email verification
- **Role-based Access Control** - Admin, User, and Partner roles
- **Booking Management** - Complete booking lifecycle with passenger management

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

| Category       | Technology         |
|---------------------|---------------------------|
| **Framework**       | ASP.NET Core 9.0   |
| **ORM**    | Entity Framework Core 9.0 |
| **Database**        | SQL Server                |
| **Authentication**  | JWT Bearer Tokens         |
| **Identity**        | ASP.NET Core Identity     |
| **Email Service**   | SendGrid    |
| **Validation**      | FluentValidation     |
| **API Documentation** | OpenAPI / Scalar UI     |

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

After the application is running, you **MUST** call the following API endpoints to import the GoBus and Train data from the CSV files included in the project:

#### Import GoBus Data

```http
POST /api/Seed/import-gobus
```

#### Import Train (ENR) Data

```http
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

> 📁 The CSV seed files are located in the Infrastructure layer:
> - `GP.Infrastructure/Data/SeedData/GoBus/` - GoBus stations, agencies, coach classes, and trips
> - `GP.Infrastructure/Data/SeedData/ENR/` - Train stations, types, coach classes, configurations, trips, and pricing
>
> *Note: Ensure these files are set to **"Copy if newer"** in Visual Studio properties so they are automatically moved to the output directory upon build.*

---

## 🔐 Default Admin Credentials

After running migrations, you can log in with the seeded admin account:

| Field        | Value      |
|--------------|------------------|
| **Email**    | `admin@gp.com`   |
| **Password** | `Admin@123456`   |

> ⚠️ Change these credentials in production!

---

## 📚 API Endpoints Overview

### 🔐 Authentication (`/api/Auth`)

| Method | Endpoint           | Description         | Auth Required |
|--------|-----------------------------------|----------------------------------------------|---------------|
| `POST` | `/api/Auth/register`  | Register a new user         | ❌            |
| `POST` | `/api/Auth/login`          | Login with email and password          | ❌         |
| `POST` | `/api/Auth/refresh`            | Refresh access token using refresh token     | ❌   |
| `POST` | `/api/Auth/revoke`                | Revoke a specific refresh token (logout)     | ❌       |
| `POST` | `/api/Auth/revoke-all`            | Revoke all refresh tokens (logout all devices) | ✅   |
| `GET`  | `/api/Auth/me`            | Get current authenticated user info          | ✅            |
| `POST` | `/api/Auth/send-verification-email` | Send email verification link              | ❌       |
| `POST` | `/api/Auth/verify-email`   | Verify email address with token            | ❌    |
| `POST` | `/api/Auth/forgot-password`       | Request password reset email  | ❌   |
| `POST` | `/api/Auth/reset-password`        | Reset password with token              | ❌   |
| `POST` | `/api/Auth/change-password`       | Change password for authenticated user       | ✅     |

### 🌍 Countries (`/api/Countries`)

| Method | Endpoint          | Description        | Auth Required |
|--------|-------------------|------------------------------------------|---------------|
| `GET`  | `/api/Countries`  | Get all countries (for registration dropdown) | ❌       |

### 🌱 Data Seeding (`/api/Seed`)

| Method | Endpoint      | Description      | Auth Required |
|--------|---------------------------|--------------------------------------|---------------|
| `POST` | `/api/Seed/import-gobus`  | Import GoBus data from CSV files     | ❌ |
| `POST` | `/api/Seed/import-trains` | Import Train (ENR) data from CSV files | ❌      |

> 📖 For complete API documentation with request/response schemas, visit `/scalar/v1` when the application is running.

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
