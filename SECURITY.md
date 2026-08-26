# Security Policy

## Reporting a vulnerability

Please do not disclose security vulnerabilities in a public issue. Use GitHub's
private vulnerability reporting feature for this repository when it is enabled,
or contact the repository owner privately.

Include the affected component, reproduction steps, impact, and any suggested
mitigation. Do not include active credentials, access tokens, personal data, or
customer data in a report.

## Secrets and local configuration

- Store local credentials in `.env` or .NET user secrets. These files are not
  committed.
- Use `.env.example` only as a key template; it must contain no working values.
- Rotate any credential immediately if it is accidentally committed or exposed.
- Use HTTPS and a secrets manager for non-development deployments.

## Deployment

The Docker Compose configuration is intended as a local development baseline.
Production operators are responsible for TLS termination, network isolation,
secret management, image update policies, database backups, and log retention.
