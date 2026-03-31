# CookDating — Dating App Prototype

Throwaway prototype for a dating app built as a **.NET 10 modular monolith** following Domain-Driven Design, with a **React 19** single-page application and **AWS services** emulated locally via [floci](https://github.com/hectorvent/floci). Local orchestration is handled by **.NET Aspire**.

> **Status:** Prototype / proof-of-concept — not intended for production use.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        React SPA (Vite)                         │
│               Discover · Matches · Chat · Profile               │
└──────────────┬──────────────────────────┬───────────────────────┘
           REST│                          │WebSocket
               ▼                          ▼
┌──────────────────────────────────────────────────────────────────┐
│                    BFF — ASP.NET Web API                         │
│              Minimal API endpoints · SignalR hubs                │
└──────┬──────────────┬───────────────────┬───────────────────────┘
       │              │                   │
       ▼              ▼                   ▼
  ┌──────────┐  ┌───────────┐  ┌────────────────┐
  │ Profile  │  │ Matching  │  │ Conversation   │
  │ Context  │  │ Context   │  │ Context        │
  └────┬─────┘  └─────┬─────┘  └───────┬────────┘
       │              │                 │
       ▼              ▼                 ▼
┌──────────────────────────────────────────────────────────────────┐
│                   AWS (floci emulator)                           │
│          DynamoDB · SQS / SNS · Cognito                         │
└──────────────────────────────────────────────────────────────────┘
       ▲              ▲
       │              │
  ┌────┴─────┐  ┌─────┴──────────┐
  │ Matching │  │ Conversation   │
  │ Worker   │  │ Worker         │
  └──────────┘  └────────────────┘
```

The system is organised as a **modular monolith** with three bounded contexts that communicate through domain events published over SNS/SQS. The **BFF** acts purely as an integration/anti-corruption layer — it is not a bounded context itself.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 19, TypeScript 5.9, Vite 8, React Router 7 |
| Real-time | SignalR (WebSockets) |
| Backend API | ASP.NET Core Minimal APIs (.NET 10) |
| Domain libraries | C# / .NET 10 with DDD building blocks |
| Persistence | Amazon DynamoDB (via floci) |
| Messaging | Amazon SQS / SNS (via floci) |
| Authentication | Amazon Cognito (via floci) |
| Orchestration | .NET Aspire |
| Unit tests | NUnit 4 |
| BDD / E2E tests | Reqnroll 3 (Gherkin) + Playwright |
| CI | GitHub Actions |

---

## Prerequisites

| Requirement | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 |
| [Node.js](https://nodejs.org/) | 22+ |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest |

Docker is required to run the **floci** container that emulates AWS services.

---

## Getting Started

```bash
# Clone the repository
git clone https://github.com/<your-org>/throwaway-cook-dating.git
cd throwaway-cook-dating

# Install client dependencies
cd src/client-app && npm install && cd ../..

# Run with Aspire
dotnet run --project src/CookDating.AppHost
```

Aspire will orchestrate the full stack automatically:

1. **floci container** — AWS emulator (DynamoDB, SQS/SNS, Cognito) on port `4566`
2. **BFF API** — ASP.NET Core backend with SignalR hubs
3. **Matching Worker** — background service consuming SQS messages for match processing
4. **Conversation Worker** — background service consuming SQS messages for chat processing
5. **React dev server** — Vite on `http://localhost:5173`

Open the **Aspire dashboard** (printed to console on startup) to monitor all resources.

---

## Project Structure

```
throwaway-cook-dating/
├── src/
│   ├── CookDating.AppHost/             # .NET Aspire orchestrator
│   ├── CookDating.ServiceDefaults/     # Shared Aspire service configuration
│   ├── CookDating.SharedKernel/        # DDD building blocks & AWS infra
│   │   ├── Domain/                     #   Entity, AggregateRoot, ValueObject, DomainEvent
│   │   └── Infrastructure/             #   DynamoDB, SNS publisher, SQS consumer, bootstrapper
│   ├── CookDating.Profile/             # Profile bounded context
│   │   ├── Domain/                     #   UserProfile, DatingPreferences, LookingStatus
│   │   ├── Application/                #   CreateProfile, UpdateProfile, SetLookingStatus
│   │   └── Infrastructure/             #   DynamoDB repository
│   ├── CookDating.Matching/            # Matching bounded context
│   │   ├── Domain/                     #   Swipe, Match, MatchCandidate
│   │   ├── Application/                #   Swipe, GetCandidates, ProcessProfileCreated
│   │   └── Infrastructure/             #   DynamoDB repositories
│   ├── CookDating.Conversation/        # Conversation bounded context
│   │   ├── Domain/                     #   Conversation, Message
│   │   ├── Application/                #   SendMessage, StartConversation, MarkMessagesRead
│   │   └── Infrastructure/             #   DynamoDB repository
│   ├── CookDating.Bff/                 # Backend-for-Frontend (API + SignalR)
│   │   ├── Hubs/                       #   SignalR hubs for real-time features
│   │   ├── Dtos/                       #   Request/response DTOs
│   │   └── Mapping/                    #   DTO ↔ domain mapping
│   ├── CookDating.Matching.Worker/     # Background worker for match processing
│   ├── CookDating.Conversation.Worker/ # Background worker for chat processing
│   └── client-app/                     # React SPA
│       └── src/
│           ├── components/             #   SwipeCard, ChatBubble, MatchListItem, etc.
│           ├── hooks/                  #   useAuth, useConversationHub, useMatchingHub
│           ├── pages/                  #   Discover, Matches, Chat, Profile, Auth
│           └── services/              #   REST API client, SignalR connection
├── tests/
│   ├── CookDating.UnitTests/           # NUnit domain model tests
│   └── CookDating.BddTests/           # Reqnroll + Playwright E2E tests
│       ├── Features/                   #   Gherkin feature files
│       ├── StepDefinitions/            #   Step bindings
│       └── Hooks/                      #   Test lifecycle hooks
└── .github/workflows/ci.yml           # CI pipeline
```

---

## Bounded Contexts

### Profile

User registration, dating preferences, and looking status. Publishes `ProfileCreated` and `LookingStatusChanged` domain events consumed by the Matching context to maintain its candidate pool.

### Matching

Swipe candidates, mutual-like detection, and match creation. Listens for profile events to build the candidate list. When two users like each other, a `MatchCreated` event is raised and published.

### Conversation

Real-time chat between matched users. **Chat is gated on match status** — the domain enforces that a conversation can only be started between users who have an active match.

---

## Running Tests

```bash
# Unit tests (domain model)
dotnet test tests/CookDating.UnitTests/

# BDD E2E tests (requires Docker for the floci container)
dotnet test tests/CookDating.BddTests/
```

Unit tests cover domain invariants across all three bounded contexts. BDD tests use Gherkin feature files for end-to-end scenarios:

| Feature file | Covers |
|---|---|
| `SignUp.feature` | User registration flow |
| `Profile.feature` | Profile management |
| `Swiping.feature` | Swipe interactions |
| `Matching.feature` | Mutual like → match creation |
| `Conversation.feature` | Chat between matched users |

---

## CI Pipeline

The GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every **push to `main`** and **pull request targeting `main`**:

1. Start a **floci** service container (AWS emulator)
2. Set up .NET 10 SDK and Node.js 22
3. Install client dependencies and build the React app
4. Restore and build the .NET solution
5. Run **unit tests** (NUnit)
6. Install Playwright browsers (Chromium)
7. Run **BDD E2E tests** (Reqnroll + Playwright)
8. Upload test result artifacts (`.trx` files)

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| **BFF as integration layer, not a bounded context** | The BFF only translates between the SPA and domain libraries — it holds no domain logic of its own. |
| **Reqnroll instead of SpecFlow** | SpecFlow does not support .NET 10. Reqnroll is its community-driven successor with full .NET 10 compatibility. |
| **floci instead of LocalStack** | floci is free and requires no authentication tokens, making local development and CI simpler. |
| **Chat gated on match status** | Enforced at the domain level — `Conversation` can only be created when a valid `Match` exists between both users. |
| **SignalR for real-time** | Provides WebSocket transport for live match notifications and chat messaging with minimal client setup. |
| **Modular monolith** | Keeps deployment simple for a prototype while maintaining clear bounded-context boundaries that could be split into separate services later. |
