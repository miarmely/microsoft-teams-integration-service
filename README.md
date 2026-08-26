# Microsoft Teams Integration Service

> Enterprise-grade ASP.NET Core (.NET 9) microservice for synchronizing Microsoft Teams channel messages and hosted media into PostgreSQL and MinIO, while providing notification capabilities through Microsoft Teams Workflow Webhooks.

## Enterprise dashboard

The repository includes a separate React and TypeScript dashboard in `frontend/`. It provides session-based AccessHub login, live Microsoft Teams messages, synchronized database messages, channel synchronization, and Adaptive Card message delivery.

Create your private local configuration and replace every placeholder with a
real value before running the stack:

```bash
cp .env.example .env
```

Generate strong, unique values for `POSTGRES_PASSWORD`,
`MINIO_ROOT_PASSWORD`, and `ACCESSHUB_JWT_SECRET_KEY`. Never commit `.env`.

Run the complete stack:

```bash
docker compose up --build
```

The dashboard is available at `http://localhost:3000` and the API at `http://localhost:8080`. To deploy with different public addresses, set these environment variables before building:

```env
DASHBOARD_API_URL=https://teams-api.example.com
DASHBOARD_ORIGIN=https://teams.example.com
```

To use your company logo, add it to `frontend/public/company-logo.svg`, then replace the marked `brand-mark` element in `frontend/src/components/Brand.tsx` with the adjacent image example.

---

## Overview

Microsoft Teams Integration Service is an enterprise backend microservice that integrates Microsoft Teams with internal business systems. It synchronizes channel messages and hosted media using the Microsoft Graph API, stores message metadata in PostgreSQL, stores synchronized media in MinIO Object Storage, uploads outgoing card images to SharePoint, and enables sending Adaptive Card notifications to Microsoft Teams channels through Workflow Webhooks.

The application follows a layered architecture with a strong focus on maintainability, scalability, and production-ready deployment.

---

# Features

## Microsoft Teams Synchronization

- Synchronize Microsoft Teams channel messages
- Synchronize hosted media (images)
- Retrieve complete message history using Graph pagination
- Store message metadata in PostgreSQL
- Store media files in MinIO Object Storage
- Automatic synchronization summary generation

---

## Teams Notifications

- Send Adaptive Card notifications
- Microsoft Teams Workflow Webhook integration
- Channel-specific notification support
- SharePoint-hosted images for outgoing Adaptive Cards
- HTTP-based notification service

---

## Logging Infrastructure

- Custom Database Logger Provider
- Asynchronous logging
- Producer–Consumer architecture
- BackgroundService log processing
- Timed batch writing
- Retry mechanism
- Graceful shutdown flushing

---

## Infrastructure

- Docker & Docker Compose
- Automatic EF Core migrations
- Automatic MinIO bucket initialization
- Structured logging
- Strongly typed configuration
- Dependency Injection
- Production-ready deployment

---

# Technology Stack

| Category | Technology |
|----------|------------|
| Backend | ASP.NET Core (.NET 9) |
| Language | C# |
| Database | PostgreSQL 18 |
| ORM | Entity Framework Core |
| Object Storage | MinIO |
| External API | Microsoft Graph SDK |
| Identity | Microsoft Entra ID |
| Notifications | Microsoft Teams Workflow |
| Containerization | Docker & Docker Compose |

---

# System Architecture

```text
                    Microsoft Graph
                           │
                           ▼
            ┌───────────────────────────┐
            │ Teams Integration Service │
            └───────────────────────────┘
                 │         │          │
                 ▼         ▼          ▼
          PostgreSQL    MinIO    Teams Workflow
```

Application flow

```text
Client
   │
   ▼
Controllers
   │
   ▼
Services
   │
   ▼
Repositories
   │
   ├──────── Microsoft Graph
   ├──────── PostgreSQL
   ├──────── MinIO
   └──────── Teams Workflow
```

---

# Project Structure

```text
src/
│
├── Controllers/
├── Services/
├── Repositories/
├── Entities/
├── Responses/
├── Logging/
├── Options/
├── Extensions/
├── Middleware/
├── Configurations/
└── Migrations/
```

### Folder Responsibilities

| Folder | Description |
|---------|-------------|
| Controllers | Exposes REST API endpoints |
| Services | Contains business logic |
| Repositories | Handles database and external services |
| Entities | Entity Framework Core models |
| Responses | DTO models returned by the API |
| Logging | Custom Database Logger implementation |
| Options | Strongly typed configuration classes |
| Extensions | Dependency Injection registrations |
| Middleware | ASP.NET Core middleware components |
| Migrations | Entity Framework Core migrations |

---

# REST API

## 1. Synchronize Teams Channel

### Endpoint

```http
POST /api/teamsSync/{teamId}/channels/{channelId}/sync
```

### Description

Synchronizes Teams channel messages and hosted media into PostgreSQL and MinIO.

### Path Parameters

| Parameter | Description |
|------------|-------------|
| teamId | Microsoft Teams Team Identifier |
| channelId | Microsoft Teams Channel Identifier |

### Success Response

```json
{
  "isSuccess": true,
  "insertedMessageCount": 25,
  "updatedMessageCount": 3,
  "unchangedMessageCount": 42,
  "synchronizedMediaCount": 18
}
```

---

## 2. Retrieve Synchronized Messages

### Endpoint

```http
GET /api/message/team/{teamId}/channel/{channelId}
```

### Description

Returns synchronized Teams messages from PostgreSQL.

### Path Parameters

| Parameter | Description |
|------------|-------------|
| teamId | Microsoft Teams Team Identifier |
| channelId | Microsoft Teams Channel Identifier |

### Success Response

```json
[
  {
    "graphMessageId": "...",
    "senderDisplayName": "...",
    "htmlContent": "...",
    "createdAt": "...",
    "media": []
  }
]
```

---

## 3. Send Teams Notification

### Endpoint

```http
POST /api/teams/message/send
```

### Description

Sends an Adaptive Card notification to a specific Microsoft Teams channel through a Teams Workflow Webhook.

### Request

```json
{
  "teamId": "...",
  "channelId": "...",
  "title": "Synchronization Completed",
  "message": "25 messages synchronized successfully."
}
```

### Success Response

```json
{
  "isSuccess": true
}
```

---

# Logging Architecture

The project contains a custom asynchronous database logging provider.

```text
ILogger
    │
    ▼
DatabaseLogger
    │
    ▼
Channel<ApplicationLog>
    │
    ▼
DatabaseLogWriterService
    │
    ▼
PostgreSQL
```

### Features

- Non-blocking logging
- Background processing
- Batch writes
- Retry mechanism
- Timed flushing
- Graceful shutdown handling

---

# Docker Deployment

## SharePoint image storage

Outgoing Adaptive Card images are uploaded to a dedicated SharePoint document
library. MinIO remains responsible for durable copies of media synchronized from
Teams.

Before starting the application:

1. Create a dedicated SharePoint site and document-library folder matching
   `SHAREPOINT_FOLDER_PATH`.
2. Grant the Entra application the Microsoft Graph application permission
   `Sites.Selected` and admin consent.
3. Grant that application `write` access to the selected SharePoint site.
4. Set `SHAREPOINT_SITE_ID` and `SHAREPOINT_DRIVE_ID` in `.env`.
5. Ensure anonymous read-only sharing is allowed for the tenant and selected
   site. Teams Workflows must be able to retrieve the image without a bearer
   token.

The service creates an anonymous view link for every outgoing image and adds
`download=1` for card rendering. Anonymous links should be treated as secrets;
anyone possessing a link can read its image. If anonymous sharing is disabled,
the upload is rolled back and message delivery fails.

Run the complete environment:

```bash
docker compose up --build
```

The following containers will be started:

- Teams Integration API
- PostgreSQL
- MinIO

During application startup:

- EF Core migrations are applied automatically.
- Required MinIO bucket is initialized.
- Background services are started.
- Database Logger Provider becomes active.

---

# Security

Current implementation:

- Microsoft Graph authentication uses **Client Credentials Flow**.
- Microsoft Teams Workflow uses secure webhook endpoints.
- Dashboard users authenticate through AccessHub.
- API clients can authenticate with an AccessHub bearer token or API key.
- Permission policies protect application endpoints.
- Browser access tokens are kept in session storage and are removed when the
  browser tab is closed.

The included Compose file is a local-development baseline. For production, use
a secrets manager, TLS, restricted container networks, trusted proxy settings,
database backups, and an explicit container-image update policy. See
[`SECURITY.md`](SECURITY.md) for vulnerability reporting and deployment notes.

---

# Future Improvements

- Role-based Authorization
- Health Checks
- Rate Limiting
- OpenTelemetry
- Metrics
- Distributed Tracing
- CI/CD Pipeline
- Unit Tests
- Integration Tests

---

# Documentation

Additional documentation available for this project:

- Developer Guide
- API Reference
- System Architecture Documentation

---

# License

No open-source license has been granted yet. Unless a `LICENSE` file is added,
the repository remains publicly visible but all rights are reserved. Choose and
add a license before inviting third parties to copy, modify, or redistribute the
project.

---

# Author

**Software Engineer | Backend Developer (.NET & Node.js) | Database Engineer**
