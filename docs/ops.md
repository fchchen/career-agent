# Operations Runbook (Azure)

This runbook documents day-2 operations for Career Agent in Azure.

Important: never store secret values in this file. Only secret names and procedures are documented.

## Resources

- API App Service: `career-agent-api`
- Frontend Static Web App: `career-agent-web`
- SQL Server: `propel-server-michigan`
- SQL Database: `sqldb-propel-prod`

## GitHub Actions Workflows

- API deploy: `.github/workflows/deploy-azure-api.yml`
- Frontend deploy: `.github/workflows/deploy-azure-frontend.yml`
- CI build/test: `.github/workflows/ci.yml`

Deploy workflows are manual (`workflow_dispatch`).

## Required GitHub Secrets (names only)

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_RESOURCE_GROUP`
- `AZURE_SQL_CONNECTION_STRING`
- `AZURE_STATIC_WEB_APPS_API_TOKEN`
- `FRONTEND_API_URL`

## Required API App Settings (names only)

- `Database__Provider`
- `ConnectionStrings__SqlServer`
- `SerpApi__ApiKey`
- `Adzuna__AppId`
- `Adzuna__AppKey`
- `Gemini__ApiKey`
- `Cors__AllowedOriginsCsv`

## Manual Deploy Procedure

### API

1. Confirm required secrets exist in GitHub Actions.
2. In GitHub Actions, run `Deploy API (Azure App Service)` on `master`.
3. Wait for steps:
   - OIDC login
   - SQL migrations (`dotnet ef database update`)
   - Zip deploy to App Service
4. Verify:
   - `GET /health` returns healthy
   - `GET /swagger` loads

### Frontend

1. Confirm `AZURE_STATIC_WEB_APPS_API_TOKEN` and `FRONTEND_API_URL` exist.
2. Run `Deploy Frontend (Azure Static Web Apps)` on `master`.
3. Hard refresh frontend URL after deployment.

## Post-Deploy Smoke Test

1. API health endpoint returns `200`.
2. `GET /api/dashboard` returns JSON.
3. Frontend loads dashboard/jobs routes without CORS errors.
4. `POST /api/jobs/search` returns non-empty results when provider keys are configured.

## SQL Schema Notes

- Career Agent SQL tables live under schema `career` (for example `career.JobListings`).
- SQL Server migrations are in `src/Api/MigrationsSqlServer`.
- SQLite migrations for local dev are in `src/Api/Migrations`.

## Common Failures

### Azure Login step fails in API workflow

- Cause: OIDC federated credential mismatch (repo/branch/subject).
- Fix: verify federated credential points to the exact repo and `refs/heads/master`.

### Migration fails with SQL transient errors (`40613`)

- Cause: Azure SQL temporary availability issue.
- Fix: rerun workflow. Migration step already retries.

### Migration fails with `Login failed for user`

- Cause: invalid SQL credentials in `AZURE_SQL_CONNECTION_STRING`.
- Fix: reset SQL password if needed; update GitHub secret and App Service setting.

### Frontend deploy fails with `deployment_token was not provided`

- Cause: missing/empty `AZURE_STATIC_WEB_APPS_API_TOKEN`.
- Fix: copy deployment token from Static Web App and update secret.

### Frontend shows empty dashboard

- Causes:
  - API has no data yet.
  - `FRONTEND_API_URL` is wrong.
  - API CORS missing frontend origin.
- Fix:
  - verify `FRONTEND_API_URL` points to API `/api`.
  - update `Cors__AllowedOriginsCsv`.
  - run search to seed jobs.

## Rotation Procedure (Secrets)

1. Rotate secret in provider (Azure SQL password, API token, etc.).
2. Update secret in GitHub Actions (same name).
3. Update App Service setting if applicable.
4. Restart App Service if setting change requires reload.
5. Run deploy workflow and smoke test.

## No-Secrets Policy

- Do not commit secret values, connection string values, or tokens.
- Do not paste secrets into issues/PRs/log snippets.
- If a secret is exposed, rotate immediately.
