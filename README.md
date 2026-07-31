# Teams Integration Service

A production-oriented ASP.NET Core Web API that synchronizes Microsoft Teams channel messages and hosted media into PostgreSQL and MinIO using Microsoft Graph API.

The project is designed as an independent integration service that can be consumed by other applications to archive Teams conversations, download hosted media, and provide a reliable synchronization pipeline.

---

# Features

- Synchronize Microsoft Teams channel messages
- Synchronize hosted images and media attachments
- Store messages in PostgreSQL
- Store media files in MinIO Object Storage
- Microsoft Graph API integration
- Full channel synchronization with Graph pagination (`@odata.nextLink`)
- Date range filtering (`fromDate` / `toDate`)
- Incremental synchronization
- Automatic duplicate detection
- Rollback support for failed media synchronization
- Production-level exception handling
- Structured logging
- ServiceResponse-based API responses
- Repository & Service architecture

---

# Technologies

- ASP.NET Core (.NET 9)
- C#
- Entity Framework Core
- PostgreSQL
- Microsoft Graph SDK
- Microsoft Kiota
- MinIO Object Storage
- Docker
- Swagger / OpenAPI

---

# Architecture

```
Controllers
      │
      ▼
Services
      │
      ▼
Repositories
      │
      ▼
Microsoft Graph API

            │

            ▼

PostgreSQL + MinIO
```

The application separates business logic, external integrations and persistence layers, making it easier to maintain, test and extend.

---

# Synchronization Flow

1. Retrieve channel messages from Microsoft Graph.
2. Automatically follow Graph pagination until all pages are synchronized.
3. Apply optional date range filtering.
4. Detect new and existing messages.
5. Store messages in PostgreSQL.
6. Extract hosted media references.
7. Download hosted media from Microsoft Graph.
8. Upload media files to MinIO.
9. Save media metadata into PostgreSQL.
10. Return synchronization statistics.

---

# Error Handling

The project includes production-oriented exception handling for:

- Microsoft Graph API
- PostgreSQL
- MinIO Object Storage
- Network failures
- Request cancellation
- Invalid requests
- Unexpected runtime exceptions

Rollback mechanisms are used during media synchronization to minimize data inconsistency between PostgreSQL and MinIO.

---

# REST API

Example endpoints:

```
POST /api/teams-sync/synchronize
```

```
GET /api/teams
```

```
GET /api/teams/{teamId}/channels
```

```
POST /api/storage-test/upload
```

---

# Project Goals

This project was developed to demonstrate enterprise backend development concepts including:

- External API integrations
- Background synchronization workflows
- Object Storage integration
- Database synchronization
- Error resilience
- Clean layered architecture
- Production-ready backend design