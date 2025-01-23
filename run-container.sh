#!/bin/bash
STORAGE_ACCOUNTNAME="satfmodulestolx00"
STORAGE_ACCESS_KEY="13qxndyQZ6qnOLnX3ZFDVUD11GoVZk0vrD5E1nRX0HS60GwfssDcAJW3NMr1bS5feJ2LITtftycb+AStze3edQ=="
API_KEY="Cd16694YKkKxvjJC1rzCfe0T/fmQLTajDND3vQ2ic1I="
# Optional: Set APP_INSIGHT_KEY if you want to enable Application Insights
APP_INSIGHT_KEY=""

# Build the container if it doesn't exist
docker build -t tfprivate-api ./tfprivate.Api

# Remove existing container if it exists
docker rm -f tfprivate-api 2>/dev/null || true

# Run the container
docker run -d \
  --name tfprivate-api \
  --restart unless-stopped \
  -p 445:443 \
  -p 80:80 \
  -v ${HOME}/.aspnet/https/:/root/.aspnet/https/ \
  -e STORAGE_ACCOUNTNAME="$STORAGE_ACCOUNTNAME" \
  -e STORAGE_ACCESS_KEY="$STORAGE_ACCESS_KEY" \
  -e API_KEY="$API_KEY" \
  ${APP_INSIGHT_KEY:+-e APP_INSIGHT_KEY="$APP_INSIGHT_KEY"} \
  -e ASPNETCORE_ENVIRONMENT="Production" \
  tfprivate-api

# Show the logs
docker logs -f tfprivate-api 