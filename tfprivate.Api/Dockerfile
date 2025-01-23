# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["tfprivate.Api.csproj", "./"]
RUN dotnet restore "tfprivate.Api.csproj"

# Copy the rest of the files and build
COPY . .
RUN dotnet build "tfprivate.Api.csproj" -c Release -o /app/build
RUN dotnet publish "tfprivate.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview
WORKDIR /app
COPY --from=build /app/publish .

# Create directory for the certificate
RUN mkdir -p /root/.aspnet/https/

# Expose only HTTPS port
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_URLS=https://+:443
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/root/.aspnet/https/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=SecurePassword123

# Note: Sensitive environment variables should be provided at runtime:
# - STORAGE_ACCOUNTNAME
# - STORAGE_ACCESS_KEY
# - API_KEY

ENTRYPOINT ["dotnet", "tfprivate.Api.dll"] 