# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Build and Run Commands

```bash
# Build entire solution
dotnet build

# Run API (from project root)
dotnet run --project src/Api/CareerAgent.Api.csproj

# Run Angular frontend
cd frontend && npm start

# Run tests
dotnet test

# Run single test file
dotnet test --filter "FullyQualifiedName~JobScoringServiceTests"
```

## Architecture Overview

AI-powered job search and resume tailoring agent:

- **src/Api** - ASP.NET Core 8 Minimal API
  - `Endpoints/` - API route handlers (JobSearch, Resume, Dashboard)
  - `Services/` - Business logic with interface abstractions
  - `Middleware/` - Global exception handling
  - `Data/` - EF Core DbContext + SQLite
  - Background service for periodic job fetching

- **src/Shared** - Shared library: models, DTOs, constants (SkillTaxonomy, SearchDefaults)

- **frontend** - Angular 21 standalone components with Material Design

## External APIs

1. **SerpAPI** - Google Jobs aggregator (LinkedIn, Indeed, Glassdoor)
2. **Claude API** - Raw HttpClient to `POST /v1/messages` for resume tailoring

## Key Integration Points

1. **SQLite via EF Core**: Local database for job listings, resumes, tailored documents
2. **Conditional Storage**: InMemoryStorageService (tests/CI) vs SqliteStorageService (runtime)
3. **CORS**: API allows `http://localhost:4200` for Angular dev server

## Testing Patterns

- xUnit + Moq + FluentAssertions
- Services use interface abstractions for mockability
- JobScoringService: pure logic tests (no mocks)
- External API services: mocked HttpClient
- Integration tests use `WebApplicationFactory<Program>`

## Configuration

API keys via `dotnet user-secrets`:
```bash
dotnet user-secrets init --project src/Api
dotnet user-secrets set "SerpApi:ApiKey" "your-key" --project src/Api
dotnet user-secrets set "Claude:ApiKey" "your-key" --project src/Api
```
