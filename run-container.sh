#!/bin/bash
STORAGE_ACCOUNTNAME=""
STORAGE_ACCESS_KEY=""
API_KEY=""
# Optional: Set APP_INSIGHT_KEY if you want to enable Application Insights
APP_INSIGHT_KEY=""

# Build the container if it doesn't exist
docker build -t tfprivate-api ./tfprivate.Api



# # Remove existing container if it exists
# docker rm -f tfprivate-api 2>/dev/null || true

# # Run the container
# docker run -d \
#   --name tfprivate-api \
#   --restart unless-stopped \
#   -p 445:443 \
#   -p 80:80 \
#   -v ${HOME}/.aspnet/https/:/root/.aspnet/https/ \
#   -e STORAGE_ACCOUNTNAME="$STORAGE_ACCOUNTNAME" \
#   -e STORAGE_ACCESS_KEY="$STORAGE_ACCESS_KEY" \
#   -e API_KEY="$API_KEY" \
#   ${APP_INSIGHT_KEY:+-e APP_INSIGHT_KEY="$APP_INSIGHT_KEY"} \
#   -e ASPNETCORE_ENVIRONMENT="Production" \
#   tfprivate-api

# # Show the logs
# docker logs -f tfprivate-api 