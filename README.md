# Secure File Statement Delivery

A production-oriented ASP.NET Core Web API for securely storing customer account statements as PDF files and providing customers with time-limited, one-time download links.

## Technology

* .NET 8
* ASP.NET Core Web API
* ASP.NET Core Identity
* Entity Framework Core
* SQLite
* Docker
* Swagger/OpenAPI

No SQL Server or SQL Server Management Studio is required.

## Features

* Staff authentication
* Password security through ASP.NET Core Identity
* Account lockout
* Authenticated customer management
* Authenticated statement uploads
* Private PDF storage
* PDF size and extension validation
* SHA-256 file integrity hash
* Cryptographically secure download tokens
* Time-limited download links
* One-time download links
* Download-link revocation
* Audit logging
* SQLite database
* Entity Framework Core migrations
* Global exception handling
* Health checks
* Docker support


## Prerequisites

* .NET 8 SDK
* Visual Studio Code
* Docker Desktop for containerized execution

## Build

Restore packages:

```bash
dotnet restore
```

Build:

```bash
dotnet build
```

## Database

SQLite is used as the database.

Create the Entity Framework migration:

```bash
dotnet ef migrations add InitialCreate
```

Apply it:

```bash
dotnet ef database update
```

The local database will be created as:

```text
statements.db
```

## Run

Start the API:

```bash
dotnet run
```

Swagger is available in development at:

```text
/swagger
```

The exact localhost port is displayed by ASP.NET Core when the application starts.

## Health Check

The API provides:

```text
GET /health
```

This verifies application/database health.

## Authentication

Staff users are managed through ASP.NET Core Identity.

Register:

```text
POST /api/Auth/register
```

Login:

```text
POST /api/Auth/login
```

Logout:

```text
POST /api/Auth/logout
```

Password requirements include:

* Minimum 12 characters
* Uppercase character
* Lowercase character
* Number
* Non-alphanumeric character

Repeated failed login attempts are subject to account lockout.


## Security

Statement PDFs are stored outside the public web root.

The server generates random filenames instead of using customer-supplied filenames as storage paths.

Download tokens are generated using a cryptographically secure random-number generator.

Only a SHA-256 hash of the token is stored in the database.

Tokens are:

* time limited
* revocable
* single use
* unpredictable

Staff endpoints require authentication.

Customer download links use anonymous access only after successful token validation.

## Docker

Build the image:

```bash
docker build -t secure-statement-delivery .
```

Run:

```bash
docker run -d \
  --name secure-statement-delivery \
  -p 8080:8080 \
  -v statement-data:/app/data \
  secure-statement-delivery
```

The API will be available at:

```text
http://localhost:8080
```

## Docker Health Check

```text
GET http://localhost:8080/health
```

## Testing

Build:

```bash
dotnet build
```

Run automated tests:

```bash
dotnet test
```

Manual API testing should verify:

1. Staff registration.
2. Staff login.
3. Unauthorized access to protected endpoints.
4. Customer creation.
5. PDF upload.
6. Secure download-link generation.
7. Successful PDF download.
8. Token cannot be reused.
9. Expired token is rejected.
10. Revoked token is rejected.
11. Audit log records security events.
12. Health endpoint works.

## Production Deployment

Before handling real customer data:

* Use HTTPS.
* Use production secrets management.
* Configure secure backups.
* Protect Docker volumes.
* Enable centralized logging.
* Configure monitoring.
* Perform dependency scanning.
* Perform security testing.
* Configure rate limiting.
* Review privacy and data-retention requirements.
* Ensure PDFs cannot be directly accessed from the web.
* Use a secure service for delivering customer links.
* Regularly patch the .NET runtime and NuGet packages.
