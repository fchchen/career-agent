# Career Agent

AI-powered job search and resume tailoring tool. Aggregates job listings from Google Jobs and Adzuna, scores them against your profile, and uses Google Gemini to generate tailored resumes and cover letters.

## Screenshots

### Dashboard
Overview with job stats, top scoring matches, and recently posted listings.

![Dashboard](docs/screenshots/dashboard.png)

### Job Search
Search and filter jobs by query, location, status, and relevance score. Location filter shows remote jobs and/or jobs within a radius of your home address.

![Job Search](docs/screenshots/job-search.png)

### Job Detail
Full job posting with skill match analysis and direct apply links (LinkedIn, Indeed, ZipRecruiter, etc.).

![Job Detail](docs/screenshots/job-detail.png)

### Resume
View and edit your master resume directly in the browser.

![Resume](docs/screenshots/resume.png)

### Resume Tailor
AI-generated tailored resume and cover letter side-by-side with the job description.

![Resume Tailor](docs/screenshots/resume-tailor.png)

### Settings
Configure your search profile — required/preferred skills, title keywords, negative keywords, and search preferences. All scoring and background fetch adapt automatically.

![Settings](docs/screenshots/settings.png)

## Features

- **Profile-Driven Personalization** — Configure your skills, title keywords, and search preferences in Settings; all scoring, searching, and background fetch adapt automatically
- **Multi-Source Job Search** — Aggregates jobs from Google Jobs (via SerpAPI) and Adzuna, with cross-source deduplication
- **Background Job Fetching** — Automatically searches using your profile's query and location on a 2-hour cycle
- **Relevance Scoring** — Scores jobs against your profile's required/preferred skills, title keywords, and negative keywords with weighted fallback to defaults
- **Location Filter** — Show remote jobs and/or jobs within a radius of your home address (geocoded via OpenStreetMap Nominatim)
- **Direct Apply Links** — One-click apply buttons for LinkedIn, Indeed, ZipRecruiter, and other job boards
- **Days-Old Badges** — Visual freshness indicators on job cards (Today, 1d, 2d, etc.)
- **Resume Management** — View and edit your master resume directly in the browser
- **Resume Tailoring** — AI rewrites your resume to match specific job descriptions using Google Gemini (free tier)
- **Cover Letter Generation** — Generates targeted cover letters alongside each tailored resume
- **PDF Export** — Download tailored resumes as PDF
- **Model Fallback** — Automatically cycles through multiple Gemini models on rate limits

## Tech Stack

- **Backend**: ASP.NET Core 8 Minimal API, EF Core (SQLite for local dev, SQL Server/Azure SQL supported for production)
- **Frontend**: Angular 21, Angular Material
- **AI**: Google Gemini API (free tier — gemini-2.5-flash, gemini-2.5-flash-lite, gemini-3-flash)
- **Job Data**: SerpAPI (Google Jobs aggregator), Adzuna API
- **Testing**: xUnit, Moq, FluentAssertions

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [SerpAPI key](https://serpapi.com/) (free tier available)
- [Adzuna API credentials](https://developer.adzuna.com/) (free tier available)
- [Google Gemini API key](https://aistudio.google.com/apikey) (free tier)

### Setup

```bash
# Clone and build
git clone https://github.com/fchchen/career-agent.git
cd career-agent
dotnet build

# Configure API keys
dotnet user-secrets init --project src/Api
dotnet user-secrets set "SerpApi:ApiKey" "your-serp-key" --project src/Api
dotnet user-secrets set "Adzuna:AppId" "your-adzuna-app-id" --project src/Api
dotnet user-secrets set "Adzuna:AppKey" "your-adzuna-app-key" --project src/Api
dotnet user-secrets set "Gemini:ApiKey" "your-gemini-key" --project src/Api

# Install frontend dependencies
cd frontend && npm install && cd ..
```

### Run

```bash
# Start the API (in one terminal)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Api/CareerAgent.Api.csproj

# Start the frontend (in another terminal)
cd frontend && npm start
```

Open http://localhost:4200 in your browser.

### Test

```bash
dotnet test
```

## Deployment

- Azure free-tier deployment guide: `docs/deploy-azure-free-tier.md`
- Production config uses:
  - `Database__Provider=SqlServer` and `ConnectionStrings__SqlServer=...` for Azure SQL
  - SQL Server migrations are maintained via `SqlServerCareerAgentDbContext` in `src/Api/MigrationsSqlServer`
  - SQL Server tables are namespaced under schema `career` (e.g. `career.JobListings`)
  - Azure API deploy workflow uses OIDC (`azure/login`) and runs `dotnet ef database update` using `AZURE_SQL_CONNECTION_STRING`
  - `Cors__AllowedOriginsCsv=...` for frontend origin allowlist
  - Frontend `API_URL` can be injected at build time (see workflow secret `FRONTEND_API_URL`)

## Architecture

```
src/
  Api/                  ASP.NET Core 8 Minimal API
    Endpoints/          Route handlers (JobSearch, Resume, Dashboard, Profile)
    Services/           Business logic (composite job search, scoring, remote classification)
    Data/               EF Core DbContext (SQLite local, SQL Server/Azure SQL supported)
    Middleware/          Global exception handling
  Shared/               Models, DTOs, constants (SearchProfile, SkillTaxonomy, SearchDefaults)
frontend/               Angular 21 + Material Design
  features/
    dashboard/          Job stats overview
    job-search/         Search, filter, location-aware job browsing
    job-detail/         Full posting with skill match analysis
    resume-tailor/      AI-generated tailored resume + cover letter
    resume/             Master resume editor
    settings/           Profile configuration (skills, keywords, search prefs)
tests/
  Api.Tests/            xUnit + Moq + FluentAssertions
```

## License

MIT
