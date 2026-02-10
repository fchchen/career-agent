# Career Agent

AI-powered job search and resume tailoring tool. Aggregates job listings from Google Jobs, scores them against your profile, and uses Google Gemini to generate tailored resumes and cover letters.

## Features

- **Job Search** — Searches Google Jobs via SerpAPI across LinkedIn, Indeed, Glassdoor, and more
- **Relevance Scoring** — Automatically scores and ranks jobs against your skills and experience
- **Resume Tailoring** — AI rewrites your resume to match specific job descriptions using Google Gemini (free tier)
- **Cover Letter Generation** — Generates targeted cover letters alongside each tailored resume
- **PDF Export** — Download tailored resumes as PDF
- **Model Fallback** — Automatically cycles through multiple Gemini models on rate limits

## Tech Stack

- **Backend**: ASP.NET Core 8 Minimal API, EF Core, SQLite
- **Frontend**: Angular 21, Angular Material
- **AI**: Google Gemini API (free tier — gemini-2.5-flash, gemini-2.5-flash-lite, gemini-3-flash)
- **Job Data**: SerpAPI (Google Jobs aggregator)
- **Testing**: xUnit, Moq, FluentAssertions

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [SerpAPI key](https://serpapi.com/) (free tier available)
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

## Architecture

```
src/
  Api/                  ASP.NET Core 8 Minimal API
    Endpoints/          Route handlers (JobSearch, Resume, Dashboard)
    Services/           Business logic with interface abstractions
    Data/               EF Core DbContext + SQLite
    Middleware/          Global exception handling
  Shared/               Models, DTOs, constants
frontend/               Angular 21 + Material Design
tests/
  Api.Tests/            xUnit + Moq + FluentAssertions
```

## License

MIT
