# MatchMaking System

A simple and scalable matchmaking system built with .NET 9, Kafka, and Redis. This solution provides HTTP APIs for match requests and retrieval, with background workers handling the actual match formation.

## Architecture

### Components
- **MatchMaking.Service**: HTTP API service for match requests and info retrieval
- **MatchMaking.Worker**: Background workers that process match requests (2 instances)
- **Kafka**: Message broker for asynchronous communication
- **Redis**: Data storage for rate limiting and match information
- **Zookeeper**: Kafka coordination service

### Data Flow
1. User makes match search request → Service validates rate limit → Publishes to Kafka
2. Workers consume requests → Accumulate players → Form matches when enough players available
3. Match completion published to Kafka → Service stores match info in Redis
4. Users can retrieve their match information via HTTP API

## Features

- **Rate Limiting**: Maximum 1 request per 100ms per user
- **Scalable Workers**: Multiple worker instances for high availability
- **Atomic Operations**: Thread-safe match formation using Redis Lua scripts
- **Simple Configuration**: Configurable players per match (default: 3)
- **Comprehensive Logging**: Detailed logs for monitoring and debugging

## Prerequisites

- Docker and Docker Compose
- .NET 9 SDK (for local development)

## Quick Start

### Using Docker Compose (Recommended)

1. **Clone the repository and navigate to the project directory**
   ```bash
   cd MatchMaking
   ```

2. **Start all services**
   ```bash
   docker-compose up --build
   ```

   This will start:
   - Zookeeper (port 2181)
   - Kafka (port 9092)
   - Redis (port 6379)
   - MatchMaking.Service (port 8080)
   - MatchMaking.Worker instances (2)

3. **Wait for all services to be healthy** (approximately 30-60 seconds)

## API Usage

### Base URL
```
http://localhost:8080
```

### Endpoints

#### 1. Request Match
**POST** `/match/search?userId={userId}`

Request a new match for a user.

**Parameters:**
- `userId` (string, required): Unique identifier for the user

**Responses:**
- `204 No Content`: Request accepted and queued for processing
- `400 Bad Request`: Invalid request or rate limit exceeded

**Example:**
```bash
curl -X POST "http://localhost:8080/match/search?userId=player1"
```

#### 2. Get Match Information
**GET** `/match/info/{userId}`

Retrieve match information for a user's last successful search request.

**Parameters:**
- `userId` (string, required): Unique identifier for the user

**Responses:**
- `200 OK`: Match information found
- `404 Not Found`: No match found for this user
- `400 Bad Request`: Invalid request

**Response Body Example:**
```json
{
  "matchId": "45ae548e-d72f-438d-bf1a-f1692a699a81",
  "userIds": ["player1", "player2", "player3"]
}
```

**Example:**
```bash
curl "http://localhost:8080/match/info/player1"
```

## Testing the System

### Manual Testing

1. **Start the system:**
   ```bash
   docker-compose up --build
   ```

2. **Request matches for multiple users:**
   ```bash
   # Request match for first player
   curl -X POST "http://localhost:8080/match/search?userId=player1"
   
   # Request match for second player
   curl -X POST "http://localhost:8080/match/search?userId=player2"
   
   # Request match for third player (this should trigger match formation)
   curl -X POST "http://localhost:8080/match/search?userId=player3"
   ```

3. **Wait a few seconds for match processing**

4. **Check match information:**
   ```bash
   # Check match for any of the players
   curl "http://localhost:8080/match/info/player1"
   curl "http://localhost:8080/match/info/player2" 
   curl "http://localhost:8080/match/info/player3"
   ```

### Rate Limiting Test

```bash
# Send multiple requests quickly (second request should be rejected)
curl -X POST "http://localhost:8080/match/search?userId=player1"
curl -X POST "http://localhost:8080/match/search?userId=player1"
```

### Automated Testing Script

Create a test script to simulate multiple users:

```bash
#!/bin/bash

echo "Testing MatchMaking System..."

# Test multiple players
for i in {1..6}; do
  echo "Requesting match for player$i"
  curl -X POST "http://localhost:8080/match/search?userId=player$i"
  sleep 0.2
done

echo "Waiting for matches to be processed..."
sleep 3

# Check results
for i in {1..6}; do
  echo "Checking match info for player$i:"
  curl -s "http://localhost:8080/match/info/player$i" | jq .
  echo ""
done
```

## Configuration

### Environment Variables

**MatchMaking.Service:**
- `Kafka__BootstrapServers`: Kafka connection string
- `Redis__ConnectionString`: Redis connection string
- `RateLimit__WindowMs`: Rate limit window in milliseconds (default: 100)

**MatchMaking.Worker:**
- `Kafka__BootstrapServers`: Kafka connection string  
- `Redis__ConnectionString`: Redis connection string
- `MatchMaking__PlayersPerMatch`: Number of players per match (default: 3)

### Modifying Players Per Match

To change the number of players required per match, update the `docker-compose.yml`:

```yaml
matchmaking-worker-1:
  environment:
    - MatchMaking__PlayersPerMatch=5  # Change to desired number
```

## Monitoring

### Logs

View logs for all services:
```bash
docker-compose logs -f
```

View logs for specific service:
```bash
docker-compose logs -f matchmaking-service
docker-compose logs -f matchmaking-worker-1
```

### Health Checks

- **Service Health**: `http://localhost:8080/match/info/test` (should return 404)
- **Kafka**: Check if topics exist:
  ```bash
  docker exec kafka kafka-topics --bootstrap-server localhost:9092 --list
  ```
- **Redis**: 
  ```bash
  docker exec redis redis-cli ping
  ```

## Development

### Local Development Setup

1. **Start infrastructure only:**
   ```bash
   docker-compose up kafka redis zookeeper kafka-init
   ```

2. **Run services locally:**
   ```bash
   # Terminal 1 - Service
   cd src/MatchMaking.Service
   dotnet run

   # Terminal 2 - Worker 1  
   cd src/MatchMaking.Worker
   dotnet run

   # Terminal 3 - Worker 2
   cd src/MatchMaking.Worker  
   dotnet run
   ```

### Code Style

The project follows these conventions:
- Records for POCO classes
- Nullable reference types enabled
- Implicit usings enabled
- Warnings as errors for nullable
- Comprehensive logging for operations

## Troubleshooting

### Common Issues

1. **Services not starting**: Wait for Kafka initialization (check logs)
2. **Connection refused**: Ensure all containers are running and healthy
3. **No matches formed**: Check worker logs for processing errors
4. **Rate limiting**: Wait 100ms between requests for same user

### Reset System

```bash
# Stop and remove all containers and volumes
docker-compose down -v

# Restart fresh
docker-compose up --build
```

## Production Considerations

- Add authentication and authorization
- Implement proper error handling and retry mechanisms  
- Add metrics and monitoring (Prometheus, Grafana)
- Configure Kafka partitions for better scalability
- Add database persistence for match history
- Implement graceful shutdown handling
- Add health check endpoints
- Configure proper logging aggregation