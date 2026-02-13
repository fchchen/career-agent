# Azure Free-Tier Deployment

This repo is now configured for Azure free-tier hosting:

- Frontend: Azure Static Web Apps (Free)
- API: Azure App Service F1 (Free)
- Database: Azure SQL free offer (optional, recommended for durable storage)

## 1) Create Azure resources

1. Create an Azure App Service Web App for the API (`.NET 8`, Linux or Windows).
2. Create an Azure Static Web App for the Angular frontend.
3. (Recommended) Create an Azure SQL Database with the free offer.

## 2) Configure API App Settings

Set these in App Service -> Configuration -> Application settings:

- `Database__Provider=SqlServer`
- `ConnectionStrings__SqlServer=<your-azure-sql-connection-string>`
- `SerpApi__ApiKey=<your-serpapi-key>`
- `Adzuna__AppId=<your-adzuna-app-id>`
- `Adzuna__AppKey=<your-adzuna-app-key>`
- `Gemini__ApiKey=<your-gemini-api-key>`
- `Cors__AllowedOriginsCsv=https://<your-static-web-app-domain>`

Notes:

- Keep `Encrypt=True;TrustServerCertificate=False;` in the SQL connection string.
- If you use multiple frontend origins, separate with commas in `Cors__AllowedOriginsCsv`.
- With `Database__Provider=SqlServer`, the API runs `Database.Migrate()` on startup.
- SQL Server objects for this app are stored under the `career` schema (for example `career.JobListings`).

## SQL Server migration workflow

This repo now keeps a dedicated SQL Server migration stream in `src/Api/MigrationsSqlServer`.

Create a new SQL Server migration after model changes:

```bash
dotnet ef migrations add <MigrationName> \
  --context SqlServerCareerAgentDbContext \
  --project src/Api/CareerAgent.Api.csproj \
  --startup-project src/Api/CareerAgent.Api.csproj \
  --output-dir MigrationsSqlServer
```

Apply migrations to Azure SQL (or any SQL Server target):

```bash
Database__Provider=SqlServer \
ConnectionStrings__SqlServer="<sql-server-connection-string>" \
dotnet ef database update \
  --context SqlServerCareerAgentDbContext \
  --project src/Api/CareerAgent.Api.csproj \
  --startup-project src/Api/CareerAgent.Api.csproj
```

Local SQLite migrations remain in `src/Api/Migrations` and are independent.

## 3) Configure GitHub Actions Secrets

Repository Settings -> Secrets and variables -> Actions:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_RESOURCE_GROUP`
- `AZURE_STATIC_WEB_APPS_API_TOKEN`
- `AZURE_SQL_CONNECTION_STRING`
- `FRONTEND_API_URL` (example: `https://<api-app>.azurewebsites.net/api`)

For API deploy, these are used with OIDC (`azure/login`) so no publish profile secret is required.

## 4) Configure Azure OIDC trust for GitHub Actions

1. Create an Azure App Registration (or reuse one dedicated for GitHub deploy automation).
2. In the App Registration, open `Certificates & secrets` -> `Federated credentials` -> `Add credential`.
3. Choose `GitHub Actions deploying Azure resources`.
4. Set your GitHub `Organization`, `Repository`, and `Branch` (for example `main`).
5. Save, then copy IDs:
   - Application (client) ID -> `AZURE_CLIENT_ID`
   - Directory (tenant) ID -> `AZURE_TENANT_ID`
   - Subscription ID -> `AZURE_SUBSCRIPTION_ID`
6. Grant RBAC to that service principal:
   - Scope: target App Service (or its resource group)
   - Role: `Website Contributor` (or `Contributor` if you want broader scope)

## 5) Deploy

Two workflows are included:

- `.github/workflows/deploy-azure-api.yml`
- `.github/workflows/deploy-azure-frontend.yml`

Trigger them manually via `workflow_dispatch` in GitHub Actions.

`deploy-azure-api.yml` runs `dotnet ef database update` for `SqlServerCareerAgentDbContext`
before publishing/deploying the API, then deploys with `az webapp deploy`.

## 6) Local development defaults

Local defaults remain:

- Database provider: SQLite
- CORS origin: `http://localhost:4200`

Production behavior is controlled via Azure App Settings above.
