# Terraform Private Registry API

A private registry API for hosting and managing Terraform modules. This API implements the [Terraform Registry Protocol](https://developer.hashicorp.com/terraform/registry/api-docs) for private modules.

I was annoyed with there not being a good open source Terraform private registry that I could host on my own infrastructure. so I made this.

```mermaid
graph LR
A[Terraform] --> B[tfprivate.API]
B --> C[Azure Blob Storage]
```

## Table of Contents

- [Features](#features)
- [Getting Started](#getting-started)
  - [1. Azure Storage Setup](#1-azure-storage-setup)
  - [2. Configuration](#2-configuration)
  - [3. Run the Application](#3-run-the-application)
  - [4. Using the Registry](#4-using-the-registry)
- [API Endpoints](#api-endpoints)
- [Authentication](#authentication)
- [Application Insights](#application-insights)
- [Docker Setup](#docker-setup)
- [Configuration](#configuration)
- [Using with Terraform](#using-with-terraform)
- [Module Structure Requirements](#module-structure-requirements)

## Features

- Upload and download Terraform modules
- Version management for modules
- Secure API key authentication
- Azure Blob Storage backend
- Docker support
- Optional Application Insights monitoring

## Getting Started

### 1. Azure Storage Setup

1. Create an Azure Storage account in your Azure subscription
2. Copy the storage account name and primary access key from the "Access keys" section
3. These values will be needed for the `STORAGE_ACCOUNTNAME` and `STORAGE_ACCESS_KEY` environment variables

### 2. Configuration

Create a `.env` file with the following content (add to .gitignore):

```env
STORAGE_ACCOUNTNAME=your_account_name
STORAGE_ACCESS_KEY=your_access_key
API_KEY=your_api_key # use `openssl rand -base64 32` to generate
APP_INSIGHT_KEY=your_app_insights_key #optional
```

### 3. Run the Application

Choose one of these methods to run the application:

#### Using Docker (Recommended)

```bash
# Build the image
docker build -t tfprivate-api .

# Run with environment file
docker run --env-file .env -p 80:80 -p 443:443 tfprivate-api
```

#### Using .NET directly

```bash
cd tfprivate.Api
dotnet run
```

The API will be available at:

- HTTP: http://localhost:80
- HTTPS: https://localhost:443
- Swagger UI (dev): https://localhost:443/swagger

### 4. Using the Registry

#### Upload a Module

```bash
# Create a module archive
tar -czf my-module.tgz my-module/

# Upload using curl
curl -X POST \
  -H "X-API-Key: your-api-key" \
  -F "file=@my-module.tgz" \
  https://localhost:443/v1/module/myorg/my-module/1.0.0
```

#### Reference in Terraform

Add the module to your Terraform configuration:

```hcl
module "example" {
  source  = "https://your-registry-url/v1/module/acme/my_module/1.0.0"

  # Module inputs
  name     = "example"
  location = "westeurope"
}
```

Configure authentication in `~/.terraformrc`:

```hcl
credentials "your-registry-url" {
  token = "your-api-key"
}
```

## API Endpoints

- List Modules: `GET /v1/modules/{namespace}`
  Lists all modules in a namespace.
  Example: `GET /v1/modules/acme`

- Get Latest Module Version: `GET /v1/module/{namespace}/{module_name}`
  Returns the download URL for the latest version of a module.
  Example: `GET /v1/module/acme/my_module`

- Get Specific Module Version: `GET /v1/module/{namespace}/{module_name}/{version}`
  Returns the download URL for a specific version of a module.
  Example: `GET /v1/module/acme/my_module/1.0.0`

- Upload Module: `POST /v1/module/{namespace}/{module_name}/{version}`
  Upload a new module version (requires API key).
  Example:

  ```bash
  curl -X POST \
    -H "X-API-Key: your-api-key" \
    -F "file=@module.tgz" \
    https://your-registry-url/v1/module/acme/my_module/1.0.0
  ```

- List Module Versions: `GET /v1/modules/{namespace}/{module_name}/versions`
  Lists all available versions for a specific module.
  Example: `GET /v1/modules/acme/my_module/versions`

## Authentication

All write operations require an API key to be provided in the `X-API-Key` header.

## Application Insights

The API supports Azure Application Insights monitoring. You can enable it by either:

1. Setting the connection string in `appsettings.json` under `Azure:AzureMonitor:ConnectionString`
2. Setting the environment variable `APP_INSIGHT_KEY` with your Application Insights connection string

If neither is provided, Application Insights monitoring will be disabled.

## Docker Setup

### Building the Image

```bash
docker build -t tfprivate-api .
```

### Running the Container

You can run the container in several ways:

1. Using environment variables:

```bash
docker run -p 80:80 -p 443:443 \
  -e STORAGE_ACCOUNTNAME=your_account_name \
  -e STORAGE_ACCESS_KEY=your_access_key \
  -e API_KEY=your_api_key \
  -e APP_INSIGHT_KEY=your_app_insights_key \
  tfprivate-api
```

The `API_KEY` will be used to secure the POST request to upload a module.

2. Using an environment file:

```bash
docker run --env-file .env -p 80:80 -p 443:443 tfprivate-api
```

3. Using docker-compose:

```yaml
version: "3"
services:
  api:
    build: .
    ports:
      - "80:80"
      - "443:443"
    env_file:
      - .env
```

4. Using docker-compose with Caddy reverse proxy (Recommended for production):

The repository includes a `docker-compose.yml` and `Caddyfile` configuration that sets up the API behind a Caddy reverse proxy. This provides automatic HTTPS with Let's Encrypt certificates.

```bash
# Start the services
docker compose up -d

# View logs
docker compose logs -f
```

The Caddy reverse proxy will automatically handle HTTPS certificates and forward requests to the API service. Make sure to update the domain in the `Caddyfile` to match your setup.

## Configuration

### Environment Variables

| Variable               | Description                                                                   | Required |
| ---------------------- | ----------------------------------------------------------------------------- | -------- |
| STORAGE_ACCOUNTNAME    | Azure Storage account name                                                    | Yes      |
| STORAGE_ACCESS_KEY     | Azure Storage access key                                                      | Yes      |
| API_KEY                | API key for authentication (can be generated using `openssl rand -base64 32`) | Yes      |
| APP_INSIGHT_KEY        | Application Insights connection string                                        | No       |
| ASPNETCORE_ENVIRONMENT | Runtime environment (defaults to Production)                                  | No       |

### Environment File (.env)

Create a `.env` file with the following content (add to .gitignore):

```env
STORAGE_ACCOUNTNAME=your_account_name
STORAGE_ACCESS_KEY=your_access_key
API_KEY=your_api_key
APP_INSIGHT_KEY=your_app_insights_key #optional
```

## Using with Terraform

The API follows the Terraform Registry Protocol and URL structure.

the url structure is: `https://{hostname}/v1/module/{namespace}/{module_name}/{version}`

- `hostname` is the hostname of the API, e.g. `localhost:5057`
- `namespace` is the namespace of the module, e.g. `acme`
- `module_name` is the name of the module, e.g. `my_module`
- `version` is the version of the module, e.g. `1.0.0`

Add the registry to your Terraform configuration:

```hcl
module "example" {
  source  = "https://your-registry-url/v1/module/acme/my_module/1.0.0"
}
```

Configure the registry in your Terraform CLI config (~/.terraformrc):

```hcl
credentials "your-registry-url" {
  token = "your-api-key"
}
```

## Module Structure Requirements

When uploading a module to the registry, it must follow a specific structure. The module can be organized in one of two ways:

### 1. Files in Root Directory

All required files must be directly in the root of the `.tgz` archive:

```
module.tgz
├── main.tf           (required)
├── providers.tf      (required)
├── variables.tf      (required - can also be named variable.tf)
├── outputs.tf        (required - can also be named output.tf)
└── README.md         (optional)
```

### 2. Files in Subdirectory

Alternatively, all required files can be in a subdirectory within the `.tgz` archive:

```
module.tgz
└── my-module/
    ├── main.tf           (required)
    ├── providers.tf      (required)
    ├── variables.tf      (required - can also be named variable.tf)
    ├── outputs.tf        (required - can also be named output.tf)
    └── README.md         (optional)
```

### Required Files

- `main.tf`: Main Terraform configuration file
- `providers.tf`: Provider configuration
- Either `variables.tf` or `variable.tf`: Variable definitions
- Either `outputs.tf` or `output.tf`: Output definitions

### Optional Files

- `README.md` or `readme.md`: Module documentation

### Creating the Archive

To create a valid module archive:

1. Ensure all required files are present
2. Create a tar archive: `tar -czf module.tgz <files or directory>`
3. Upload using the registry API
