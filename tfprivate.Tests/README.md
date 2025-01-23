# Terraform Private Registry Tests

This test suite contains integration tests for the Terraform Private Registry API.

## Prerequisites

1. Azure Storage Account with connection string configured
2. .NET 9.0 SDK installed

## Running the Tests

1. First, start the API in development mode:

   ```bash
   cd ../tfprivate.Api
   dotnet run
   ```

2. In a separate terminal, run the tests:
   ```bash
   cd tfprivate.Tests
   dotnet test
   ```

## Test Configuration

The tests expect:

- API running on `http://localhost:5260` (default development URL)
- API key set to `test-api-key`
- Azure Storage connection string configured in `appsettings.Development.json` or environment variables

## Test Categories

1. Module Upload Tests

   - Valid module structure
   - Invalid module structure
   - API key authentication
   - Version validation
   - Duplicate version handling

2. Module Download Tests
   - Download specific version
   - Get latest version
   - List module versions
   - Pagination

## Troubleshooting

1. If tests fail with connection refused:

   - Ensure the API is running on port 5260
   - Check if the port is configured correctly in `launchSettings.json`

2. If tests fail with unauthorized:

   - Verify the API key in the API's configuration matches `test-api-key`

3. If tests fail with storage errors:
   - Verify Azure Storage connection string is configured correctly
   - Ensure the storage account exists and is accessible
