#!/bin/bash

echo "🎮 MatchMaking System Test Script"
echo "=================================="

# Base URL
BASE_URL="http://localhost:8080"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to make a request and show result
make_request() {
    local user_id=$1
    echo -e "${YELLOW}📤 Requesting match for $user_id${NC}"
    
    response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL/match/search?userId=$user_id")
    http_code="${response: -3}"
    
    if [ "$http_code" = "204" ]; then
        echo -e "${GREEN}✅ Request accepted${NC}"
    else
        echo -e "${RED}❌ Request failed (HTTP $http_code)${NC}"
    fi
}

# Function to check match info
check_match() {
    local user_id=$1
    echo -e "${YELLOW}🔍 Checking match info for $user_id${NC}"
    
    response=$(curl -s "$BASE_URL/match/info/$user_id")
    
    if echo "$response" | jq . >/dev/null 2>&1; then
        echo -e "${GREEN}✅ Match found:${NC}"
        echo "$response" | jq .
    else
        echo -e "${RED}❌ No match found or error occurred${NC}"
        echo "$response"
    fi
    echo ""
}

# Test 1: Basic matchmaking (3 players)
echo ""
echo "🧪 Test 1: Basic Matchmaking (3 players)"
echo "----------------------------------------"

make_request "alice"
sleep 0.2
make_request "bob"  
sleep 0.2
make_request "charlie"

echo ""
echo "⏳ Waiting 3 seconds for match processing..."
sleep 3

check_match "alice"
check_match "bob"
check_match "charlie"

# Test 2: Rate limiting
echo "🧪 Test 2: Rate Limiting"
echo "------------------------"
echo -e "${YELLOW}📤 First request for dave${NC}"
curl -s -X POST "$BASE_URL/match/search?userId=dave" -w "%{http_code}\n"

echo -e "${YELLOW}📤 Immediate second request for dave (should fail)${NC}"
curl -s -X POST "$BASE_URL/match/search?userId=dave" -w "%{http_code}\n"

echo ""
echo "⏳ Waiting 200ms..."
sleep 0.2

echo -e "${YELLOW}📤 Third request for dave (should work)${NC}"
curl -s -X POST "$BASE_URL/match/search?userId=dave" -w "%{http_code}\n"

# Test 3: Multiple matches
echo ""
echo "🧪 Test 3: Multiple Matches (6 players = 2 matches)"
echo "---------------------------------------------------"

players=("eve" "frank" "grace" "henry" "iris" "jack")

for player in "${players[@]}"; do
    make_request "$player"
    sleep 0.15
done

echo ""
echo "⏳ Waiting 5 seconds for match processing..."
sleep 5

for player in "${players[@]}"; do
    check_match "$player"
done

echo "🏁 Test completed!"