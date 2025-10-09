# MatchMaking System

A matchmaking system built with .NET 9, Kafka, and Redis for grouping players into matches, following Clean Architecture principles.

## Overview

This solution implements the MatchMaking test task requirements:

**Core Requirements:**

1. Users can request to search for a new match (max 1 request per 100ms)
2. Users can retrieve information about their last successful match

**Technical Implementation:**

- **MatchMaking.Service**: HTTP API with two endpoints
- **MatchMaking.Worker**: Background workers (2 instances) for match processing
- **Communication**: Kafka topics (`matchmaking.request` and `matchmaking.complete`)
- **Storage**: Redis for rate limiting and match data

**Architecture:**

This project follows **Clean Architecture** principles with clear separation of concerns:
- **Domain Layer**: Core business entities and interfaces
- **Application Layer**: Use cases and business logic
- **Infrastructure Layer**: External integrations (Redis, Kafka)
- **Presentation Layer**: API and Worker services

For detailed architecture documentation, see [CLEAN_ARCHITECTURE.md](CLEAN_ARCHITECTURE.md)

## Infrastructure Setup

The system uses Docker Compose with the following components:

- MatchMaking.Service (1 instance)
- MatchMaking.Worker (2 instances)
- Kafka
- Redis

## How to Run

### Prerequisites

- Docker and Docker Compose

### Start the System

```bash
docker compose up --build
```

Wait 30-60 seconds for all services to initialize.

## API Specification

### Base URL

``` bash
http://localhost:8080
```

### Endpoints

#### 1. Match Search Request

``` bash
POST /match/search?userId={userId}
```

**Parameters:**

- `userId` (string): User identifier

**Responses:**

- `204`: Request accepted
- `400`: Invalid request or rate limit exceeded

#### 2. Retrieve Match Information

``` bash
GET /match/info/{userId}
```

**Parameters:**

- `userId` (string): User identifier

**Responses:**

- `200`: Match found
- `404`: No match found
- `400`: Invalid request

**Response Body:**

```json
{
  "matchId": "45ae548e-d72f-438d-bf1a-f1692a699a81",
  "userIds": ["user1", "user2", "user3"]
}
```

## Configuration

**Players per match**: Default 3, configurable in `appsettings.json`:

```json
{
  "MatchMaking": {
    "PlayersPerMatch": 3
  }
}
```

## Testing Guide

1. Start the system: `docker compose up --build`
2. Wait 30-60 seconds for initialization
3. Verify services are running: `docker ps`

### Test 1: Basic Match Formation (3 Players)

Step 1: Request matches for 3 players

```bash
# Player 1
curl -X POST "http://localhost:8080/match/search?userId=alice"
# Expected: HTTP 204

# Player 2  
curl -X POST "http://localhost:8080/match/search?userId=bob"
# Expected: HTTP 204

# Player 3 (triggers match formation)
curl -X POST "http://localhost:8080/match/search?userId=charlie"
# Expected: HTTP 204
```

Step 2: Wait for processing

```bash
sleep 3
```

Step 3: Check match results

```bash
# All three should return the same matchId
curl "http://localhost:8080/match/info/alice"
curl "http://localhost:8080/match/info/bob"  
curl "http://localhost:8080/match/info/charlie"
```

**Expected Response:**

```json
{
  "matchId": "some-guid",
  "userIds": ["alice", "bob", "charlie"]
}
```

### Test 2: Rate Limiting

```bash
# First request (should succeed)
curl -X POST "http://localhost:8080/match/search?userId=dave"
# Expected: HTTP 204

# Immediate second request (should fail)
curl -X POST "http://localhost:8080/match/search?userId=dave"
# Expected: HTTP 400

# Wait and try again (should succeed)
sleep 0.2
curl -X POST "http://localhost:8080/match/search?userId=dave"
# Expected: HTTP 204
```

### Test 3: Error Cases

```bash
# Invalid user ID
curl -X POST "http://localhost:8080/match/search?userId="
# Expected: HTTP 400

# Non-existent match
curl "http://localhost:8080/match/info/nonexistent"
# Expected: HTTP 404
```
