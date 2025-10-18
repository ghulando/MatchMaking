# Clean Architecture Implementation

This project now follows Clean Architecture principles with clear separation of concerns across multiple layers.

## Architecture Layers

### 1. Domain Layer (`MatchMaking.Domain`)

The core layer containing business entities and repository interfaces. This layer has no dependencies on other layers.

**Key Components:**
- **Entities**: `Match`, `MatchRequest` - Domain models with business rules

- **Repository Interfaces**: 
  - `IMatchRepository` - Match data access
  - `IRateLimitRepository` - Rate limiting logic
  - `IPendingPlayersRepository` - Player queue management
  - `IMessagePublisher` - Message broker abstraction

### 2. Application Layer (`MatchMaking.Application`)

Contains business logic and use cases. Depends only on the Domain layer.

**Key Components:**

- **Use Cases**:

  - `RequestMatchUseCase` - Handle match search requests
  - `GetMatchInfoUseCase` - Retrieve match information
  - `StoreMatchInfoUseCase` - Store match results
  - `ProcessMatchRequestUseCase` - Process incoming match requests
  - `CreateMatchUseCase` - Create matches from pending players
- **DTOs**: `MatchInfoDto` - Data transfer objects

### 3. Infrastructure Layer (`MatchMaking.Infrastructure`)

Implements repository interfaces using external technologies (Redis, Kafka). Depends on Domain layer.

**Key Components:**

- **Repositories**:

  - `RedisMatchRepository` - Redis-based match storage
  - `RedisRateLimitRepository` - Redis-based rate limiting
  - `RedisPendingPlayersRepository` - Redis-based player queue
- **Messaging**:
  - `KafkaMessagePublisher` - Kafka-based message publishing

### 4. Presentation Layer (`MatchMaking.Service`, `MatchMaking.Worker`)

API and worker implementations that consume the Application layer.

**MatchMaking.Service:**

- REST API controllers
- Background consumer service
- Dependency injection configuration

**MatchMaking.Worker:**

- Background worker for match processing
- Kafka consumer implementation

## Benefits of This Architecture

1. **Separation of Concerns**: Each layer has a clear responsibility
2. **Testability**: Business logic is isolated and easily testable
3. **Maintainability**: Changes to infrastructure don't affect business logic
4. **Flexibility**: Easy to swap out implementations (e.g., replace Redis with another storage)
5. **Dependency Inversion**: High-level modules don't depend on low-level modules

## Dependency Flow

``` bash
Presentation → Application → Domain ← Infrastructure
```

- Domain layer is independent
- Application layer depends on Domain
- Infrastructure implements Domain interfaces
- Presentation uses Application and Infrastructure through dependency injection
