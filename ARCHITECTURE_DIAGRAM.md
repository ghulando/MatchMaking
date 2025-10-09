# Clean Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                           │
│  ┌──────────────────────┐         ┌──────────────────────────┐    │
│  │  MatchMaking.Service │         │   MatchMaking.Worker     │    │
│  │  - Controllers       │         │   - Worker Service       │    │
│  │  - Background Service│         │                          │    │
│  └──────────┬───────────┘         └───────────┬──────────────┘    │
│             │                                  │                    │
└─────────────┼──────────────────────────────────┼───────────────────┘
              │                                  │
              ▼                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        Application Layer                            │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                        Use Cases                             │  │
│  │  - RequestMatchUseCase                                       │  │
│  │  - GetMatchInfoUseCase                                       │  │
│  │  - StoreMatchInfoUseCase                                     │  │
│  │  - ProcessMatchRequestUseCase                                │  │
│  │  - CreateMatchUseCase                                        │  │
│  └──────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                          DTOs                                │  │
│  │  - MatchInfoDto                                              │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         Domain Layer (Core)                         │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                        Entities                              │  │
│  │  - Match                                                     │  │
│  │  - MatchRequest                                              │  │
│  └──────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                   Repository Interfaces                      │  │
│  │  - IMatchRepository                                          │  │
│  │  - IRateLimitRepository                                      │  │
│  │  - IPendingPlayersRepository                                 │  │
│  │  - IMessagePublisher                                         │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────┬───────────────────────────────────────────┘
                          ▲
                          │ implements
                          │
┌─────────────────────────────────────────────────────────────────────┐
│                      Infrastructure Layer                           │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                     Repositories                             │  │
│  │  - RedisMatchRepository                                      │  │
│  │  - RedisRateLimitRepository                                  │  │
│  │  - RedisPendingPlayersRepository                             │  │
│  └──────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                      Messaging                               │  │
│  │  - KafkaMessagePublisher                                     │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  External Dependencies: Redis, Kafka                                │
└─────────────────────────────────────────────────────────────────────┘
```

## Dependency Flow

The dependency rule states that source code dependencies can only point inwards:

- **Presentation Layer** depends on **Application Layer**
- **Application Layer** depends on **Domain Layer**
- **Infrastructure Layer** implements interfaces from **Domain Layer**
- **Domain Layer** has NO dependencies (pure business logic)

This ensures that:
1. Business rules are independent of frameworks
2. Infrastructure can be easily replaced
3. Code is highly testable
4. Changes propagate from outer layers to inner layers only
