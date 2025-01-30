# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["tfprivate.Api.csproj", "./"]
RUN dotnet restore "tfprivate.Api.csproj"

# Copy the rest of the files and build
COPY . .
RUN dotnet build "tfprivate.Api.csproj" -c Release -o /app/build
RUN dotnet publish "tfprivate.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "tfprivate.Api.dll"] 