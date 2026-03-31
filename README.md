# CookDating — Dating App Prototype

Throwaway prototype for a dating app built as a **.NET 10 modular monolith** following Domain-Driven Design, with a **React 19** single-page application and **AWS services** emulated locally via [floci](https://github.com/hectorvent/floci). Local orchestration is handled by **.NET Aspire**.

> **Status:** Prototype / proof-of-concept — not intended for production use.

---

## The Matching Journey

A visual walkthrough of the full user experience — from signing up to chatting with a match.

### 1. Authentication

<p float="left">
  <img src="docs/screenshots/01-signin.png" width="200" alt="Sign in" />
  <img src="docs/screenshots/02-signup.png" width="200" alt="Sign up (empty)" />
  <img src="docs/screenshots/03-signup-filled.png" width="200" alt="Sign up (filled)" />
</p>

Users register with an email, password, display name, date of birth, gender, and dating preferences (preferred gender, age range, maximum distance). Authentication is handled by **Amazon Cognito** (emulated via floci).

### 2. Profile & Looking Status

<p float="left">
  <img src="docs/screenshots/04-profile.png" width="200" alt="Profile (Not Looking)" />
  <img src="docs/screenshots/05-actively-looking.png" width="200" alt="Profile (Actively Looking)" />
</p>

After signing up, users land on their **Profile** page. The looking status toggle controls whether you appear in other users' discover feeds. Changing it publishes a `LookingStatusChanged` domain event via SNS → SQS to the Matching Worker, which activates or deactivates the user in the candidate pool.

### 3. Discovering Candidates

<p float="left">
  <img src="docs/screenshots/06-discover-empty.png" width="200" alt="No candidates" />
  <img src="docs/screenshots/08-discover-candidate.png" width="200" alt="Candidate card" />
  <img src="docs/screenshots/10-alice-sees-bob.png" width="200" alt="Another candidate" />
</p>

The **Discover** tab connects to the `MatchingHub` via SignalR. Candidates are loaded from DynamoDB, filtered to exclude users you've already swiped on. Each card shows the candidate's name, gender, and **Pass** / **Like** buttons.

### 4. Matching

<p float="left">
  <img src="docs/screenshots/09-bob-swiped.png" width="200" alt="After swiping right" />
  <img src="docs/screenshots/11-match.png" width="200" alt="It's a match!" />
</p>

When you swipe **Like** on someone who has already liked you, a **match** is created. The domain detects the mutual like, raises a `MatchCreated` event, and the BFF pushes an **"It's a Match!"** modal to both users in real-time via SignalR. A conversation is created immediately so the matched pair can start chatting.

### 5. Matches List & Chat

<p float="left">
  <img src="docs/screenshots/12-matches-list.png" width="200" alt="Matches list" />
  <img src="docs/screenshots/13-chat-empty.png" width="200" alt="Empty chat" />
</p>

The **Matches** tab lists all conversations. Tapping a match opens the chat view, which connects to the `ConversationHub` via SignalR.

### 6. Messaging

<p float="left">
  <img src="docs/screenshots/14-chat-message.png" width="200" alt="Sent message" />
  <img src="docs/screenshots/15-bob-sees-message.png" width="200" alt="Received message" />
  <img src="docs/screenshots/16-chat-conversation.png" width="200" alt="Full conversation" />
</p>

Messages are sent and received in **real-time** via SignalR WebSockets. The domain enforces that only match participants can send messages, and message content is capped at 2,000 characters.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        React SPA (Vite)                         │
│               Discover · Matches · Chat · Profile               │
└──────────────┬──────────────────────────┬───────────────────────┘
        REST / │                          │ WebSocket
        HTTP   │                          │ (SignalR)
               ▼                          ▼
┌──────────────────────────────────────────────────────────────────┐
│                    BFF — ASP.NET Web API                         │
│         REST controllers · SignalR hubs (Matching, Chat)         │
│             Anti-corruption layer — no domain logic              │
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
│     DynamoDB · SNS topics · SQS queues · Cognito user pool      │
└──────────────────────────────────────────────────────────────────┘
       ▲              ▲
       │              │
  ┌────┴─────┐  ┌─────┴──────────┐
  │ Matching │  │ Conversation   │
  │ Worker   │  │ Worker         │
  └──────────┘  └────────────────┘
```

The system is organised as a **modular monolith** with three bounded contexts that communicate through domain events published over **SNS → SQS**. The **BFF** acts purely as an integration / anti-corruption layer — it translates between the React SPA and domain libraries but holds no domain logic of its own.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 19, TypeScript 5.9, Vite 8, React Router 7 |
| Real-time | SignalR (WebSockets) |
| Backend API | ASP.NET Core (.NET 10) |
| Domain libraries | C# / .NET 10 — DDD building blocks |
| Persistence | Amazon DynamoDB (via floci) |
| Messaging | Amazon SNS / SQS (via floci) |
| Authentication | Amazon Cognito (via floci) |
| Orchestration | .NET Aspire 13.2 |
| Logging | `[LoggerMessage]` source-generated structured logging |
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

| Resource | Description |
|---|---|
| `floci` | AWS emulator container (DynamoDB, SNS, SQS, Cognito) on port `4566` |
| `bff` | ASP.NET Core backend — REST API + SignalR hubs |
| `matching-worker` | Background service consuming SQS messages for match processing |
| `conversation-worker` | Background service consuming SQS messages for chat processing |
| `client-app` | React dev server (Vite) — proxies `/api` and `/hubs` to the BFF |

Open the **Aspire dashboard** (URL printed to console on startup) to monitor all resources, view structured logs, and inspect traces.

---

## Project Structure

```
throwaway-cook-dating/
├── src/
│   ├── CookDating.AppHost/             # .NET Aspire orchestrator
│   ├── CookDating.ServiceDefaults/     # Shared Aspire service configuration
│   ├── CookDating.SharedKernel/        # DDD building blocks & AWS infrastructure
│   │   ├── Domain/                     #   Entity, AggregateRoot, ValueObject, IDomainEvent
│   │   └── Infrastructure/             #   DynamoDB repos, SNS publisher, SQS consumer
│   ├── CookDating.Profile/             # Profile bounded context
│   │   ├── Domain/                     #   UserProfile, DatingPreferences, Gender, LookingStatus
│   │   ├── Application/                #   Commands + handlers
│   │   └── Infrastructure/             #   DynamoDbProfileRepository
│   ├── CookDating.Matching/            # Matching bounded context
│   │   ├── Domain/                     #   MatchCandidate, Swipe, Match, SwipeDirection
│   │   ├── Application/                #   Commands + handlers
│   │   └── Infrastructure/             #   DynamoDbMatchCandidateRepository, DynamoDbMatchRepository
│   ├── CookDating.Conversation/        # Conversation bounded context
│   │   ├── Domain/                     #   Conversation, Message
│   │   ├── Application/                #   Commands + handlers
│   │   └── Infrastructure/             #   DynamoDbConversationRepository
│   ├── CookDating.Bff/                 # Backend-for-Frontend
│   │   ├── Controllers/                #   AuthController, ProfileController
│   │   ├── Hubs/                       #   MatchingHub, ConversationHub (SignalR)
│   │   ├── Dtos/                       #   Request/response DTOs
│   │   └── Infrastructure/             #   Middleware, Cognito settings
│   ├── CookDating.Matching.Worker/     # SQS consumer: profile-events → matching-queue
│   ├── CookDating.Conversation.Worker/ # SQS consumer: matching-events → conversation-queue
│   └── client-app/                     # React SPA
│       └── src/
│           ├── components/             #   SwipeCard, ChatBubble, MatchListItem, etc.
│           ├── hooks/                  #   useAuth, useConversationHub, useMatchingHub
│           ├── pages/                  #   DiscoverTab, MatchesTab, ChatView, ProfileTab
│           └── services/              #   REST API client, SignalR connection
├── tests/
│   ├── CookDating.UnitTests/           # NUnit domain model tests
│   └── CookDating.BddTests/           # Reqnroll + Playwright E2E tests
│       ├── Features/                   #   Gherkin feature files
│       ├── StepDefinitions/            #   Step bindings
│       ├── Hooks/                      #   AspireHook, LogWatcherHook
│       └── Support/                    #   LogCollector (resource log monitoring)
├── docs/screenshots/                   # App screenshots for this README
└── .github/workflows/ci.yml           # CI pipeline
```

---

## Bounded Contexts

### Profile

Manages user registration, profile editing, dating preferences, and looking status.

**Domain model:** `UserProfile` (aggregate root) with `DatingPreferences` (value object).

**Publishes:**
- `ProfileCreated` — when a new user signs up
- `LookingStatusChanged` — when a user toggles actively looking on/off

**Validation rules:** Users must be 18+, display name is required, age range min ≥ 18, max distance > 0.

### Matching

Maintains a candidate pool, records swipes, and detects mutual likes.

**Domain model:** `MatchCandidate` (aggregate root) containing `Swipe` value objects, and `Match` (aggregate root).

**Consumes:** `ProfileCreated`, `LookingStatusChanged` (from Profile context via SQS)

**Publishes:**
- `SwipeRecorded` — on every swipe
- `MatchCreated` — when a mutual like is detected

**Key invariants:** Cannot swipe on yourself, cannot swipe on the same user twice, inactive candidates don't appear in the discover feed.

### Conversation

Real-time chat between matched users. **Chat is gated on match status** — the domain enforces that a conversation can only be started between users who have an active match.

**Domain model:** `Conversation` (aggregate root) containing `Message` entities.

**Consumes:** `MatchCreated` (from Matching context via SQS)

**Key invariants:** Only match participants can send messages, message content max 2,000 characters, only participants can read messages.

---

## Commands & Queries

All application logic flows through command handlers. The BFF translates HTTP/SignalR requests into commands, dispatches them to the appropriate handler, and returns the result.

### Profile Commands

| Command | Trigger | What it does | Events raised |
|---|---|---|---|
| `CreateProfileCommand` | `POST /api/auth/signup` | Creates a `UserProfile` aggregate, persists to DynamoDB | `ProfileCreated` |
| `UpdateProfileCommand` | `PUT /api/profile` | Updates name, bio, photos, date of birth, gender, and/or dating preferences | — |
| `SetLookingStatusCommand` | `PUT /api/profile/status` | Toggles between `ActivelyLooking` and `NotLooking` | `LookingStatusChanged` |

### Matching Commands

| Command | Trigger | What it does | Events raised |
|---|---|---|---|
| `GetCandidatesCommand` | `MatchingHub.GetCandidates()` | Returns active candidates the user hasn't swiped on yet | — |
| `SwipeCommand` | `MatchingHub.Swipe()` | Records a swipe (left/right). If mutual like → creates `Match` | `SwipeRecorded`, `MatchCreated` (if mutual) |
| `ProcessProfileCreatedCommand` | Matching Worker (SQS) | Creates a `MatchCandidate` entry from a `ProfileCreated` event | — |
| `ProcessLookingStatusCommand` | Matching Worker (SQS) | Activates/deactivates a candidate based on looking status | — |

### Conversation Commands

| Command | Trigger | What it does | Events raised |
|---|---|---|---|
| `StartConversationCommand` | BFF (on match) + Conversation Worker (SQS) | Creates a `Conversation` for a match | `ConversationStarted` |
| `GetConversationsCommand` | `ConversationHub.GetConversations()` | Lists all conversations for a user | — |
| `GetConversationCommand` | `ConversationHub.JoinConversation()` | Loads a single conversation (with auth check) | — |
| `SendMessageCommand` | `ConversationHub.SendMessage()` | Adds a message to the conversation | `MessageSent` |
| `MarkMessagesReadCommand` | `ConversationHub.MarkRead()` | Marks incoming messages as read | — |

---

## BFF Endpoints & Hubs

The BFF is a thin integration layer — it maps HTTP requests and SignalR messages to domain commands and returns results. It holds **no domain logic**.

### REST Controllers

#### `AuthController` — `/api/auth`

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/auth/signup` | Register user in Cognito, create profile, sync candidate to matching |
| `POST` | `/api/auth/signin` | Authenticate with Cognito, return JWT (falls back to prototype token if Cognito is unavailable) |

#### `ProfileController` — `/api/profile` (requires auth)

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/profile` | Fetch current user's profile |
| `PUT` | `/api/profile` | Update profile details (name, bio, DOB, gender, preferences) |
| `PUT` | `/api/profile/status` | Toggle looking status (`ActivelyLooking` ↔ `NotLooking`) |

### SignalR Hubs

#### `MatchingHub` — `/hubs/matching` (requires auth)

| Method | Parameters | Description | Client callback |
|---|---|---|---|
| `GetCandidates` | — | Load swipeable candidates | `ReceiveCandidates` |
| `Swipe` | `{ TargetUserId, Direction }` | Record swipe; detect match; create conversation | `MatchFound` (sent to both users) |

#### `ConversationHub` — `/hubs/conversation` (requires auth)

| Method | Parameters | Description | Client callback |
|---|---|---|---|
| `GetConversations` | — | List all conversations | `ReceiveConversations` |
| `JoinConversation` | `conversationId` | Load messages, join SignalR group | `ReceiveMessages` |
| `LeaveConversation` | `conversationId` | Leave SignalR group | — |
| `SendMessage` | `conversationId, content` | Send message (broadcast to group) | `ReceiveMessage` |
| `MarkRead` | `conversationId` | Mark messages as read | — |

---

## Domain Events & Messaging

Events flow between bounded contexts via **SNS → SQS**:

```
Profile Context                              Matching Context                         Conversation Context
──────────────                               ────────────────                         ────────────────────

 ProfileCreated ───┐                                                                  
                   ├──→ SNS: profile-events ──→ SQS: matching-queue ──→ Matching Worker
LookingStatusChanged┘        │                        │
                             │               ProcessProfileCreatedCommand
                             │               ProcessLookingStatusCommand
                             │
                             │                SwipeRecorded ───┐
                             │                                 ├──→ SNS: matching-events ──→ SQS: conversation-queue ──→ Conversation Worker
                             │                MatchCreated ────┘                                       │
                             │                                                            StartConversationCommand
```

### SNS Topics

| Topic | Published by | Events |
|---|---|---|
| `profile-events` | Profile command handlers | `ProfileCreated`, `LookingStatusChanged` |
| `matching-events` | Matching command handlers | `SwipeRecorded`, `MatchCreated` |

### SQS Queues

| Queue | Subscribed to | Consumer | Processes |
|---|---|---|---|
| `matching-queue` | `profile-events` | `MatchingEventConsumer` (Matching Worker) | Creates/activates/deactivates match candidates |
| `conversation-queue` | `matching-events` | `ConversationEventConsumer` (Conversation Worker) | Creates conversations for new matches |

---

## AWS Infrastructure (DynamoDB)

All persistence uses DynamoDB tables, bootstrapped automatically when the app starts.

| Table | Primary Key | GSIs | Stores |
|---|---|---|---|
| `Profiles` | `UserId` (S) | — | `UserProfile` aggregates |
| `MatchCandidates` | `UserId` (S) | — | `MatchCandidate` aggregates (includes embedded swipes) |
| `Matches` | `MatchId` (S) | `User1Id-index`, `User2Id-index` | `Match` aggregates |
| `Conversations` | `ConversationId` (S) | `MatchIdIndex`, `Participant1IdIndex`, `Participant2IdIndex` | `Conversation` aggregates (includes embedded messages) |

---

## Running Tests

```bash
# Unit tests (domain model)
dotnet test tests/CookDating.UnitTests/

# BDD E2E tests (requires Docker for the floci container)
dotnet test tests/CookDating.BddTests/
```

### Unit Tests

NUnit tests covering domain invariants across all three bounded contexts — profile validation, swipe rules, match detection, message constraints, etc.

### BDD Tests

Reqnroll (Gherkin) feature files exercised end-to-end with Playwright against a real Aspire-hosted stack:

| Feature file | Covers |
|---|---|
| `SignUp.feature` | User registration flow |
| `Profile.feature` | Profile editing, looking status toggle, preferences, gender reset |
| `Swiping.feature` | Swipe interactions |
| `Matching.feature` | Mutual like → match creation |
| `Conversation.feature` | Chat between matched users |

### Log Watcher

BDD tests include a **log watcher hook** that monitors Aspire resource logs (BFF, Matching Worker, Conversation Worker) during each scenario. If any unexpected error-level logs are detected, the scenario is automatically failed. Known expected warnings (Cognito emulator fallbacks, etc.) are allowlisted in `LogCollector.cs`.

---

## Structured Logging

All services use .NET's `[LoggerMessage]` source-generated structured logging with named event IDs for efficient, zero-allocation log output. Every HTTP request is enriched with a `UserId` scope via `UserIdLoggingScopeMiddleware`, making it easy to trace a user's journey through the Aspire dashboard.

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
| **No infrastructure dependencies in domain layers** | Domain projects (Profile, Matching, Conversation) have zero NuGet dependencies — pure C# with DDD building blocks from SharedKernel. |
| **Reqnroll instead of SpecFlow** | SpecFlow does not support .NET 10. Reqnroll is its community-driven successor with full .NET 10 compatibility. |
| **floci instead of LocalStack** | floci is free and requires no authentication tokens, making local development and CI simpler. |
| **Chat gated on match status** | Enforced at the domain level — `Conversation` can only be created when a valid `Match` exists between both users. |
| **SignalR for real-time** | Provides WebSocket transport for live match notifications and chat messaging with minimal client setup. |
| **Modular monolith** | Keeps deployment simple for a prototype while maintaining clear bounded-context boundaries that could be split into separate services later. |
| **`[LoggerMessage]` source generation** | Zero-allocation structured logging with named event IDs for efficient diagnostics on Aspire. |
