# Terraform Private Registry API

A private registry API for hosting and managing Terraform modules. This API implements the [Terraform Registry Protocol](https://developer.hashicorp.com/terraform/registry/api-docs) for private modules.

## Features

- Upload and download Terraform modules
- Version management for modules
- Secure API key authentication
- Azure Blob Storage backend
- Docker support
- Optional Application Insights monitoring

## API Endpoints

### List Modules

```http
GET /v1/modules/{namespace}
```

Lists all modules in a namespace.

Example:

```http
GET /v1/modules/acme
```

### Get Latest Module Version

```http
GET /v1/module/{namespace}/{module_name}
```

Returns the download URL for the latest version of a module.

Example:

```http
GET /v1/module/acme/my_module
```

### Get Specific Module Version

```http
GET /v1/module/{namespace}/{module_name}/{version}
```

Returns the download URL for a specific version of a module.

Example:

```http
GET /v1/module/acme/my_module/1.0.0
```

### Upload Module

```http
POST /v1/module/{namespace}/{module_name}/{version}
```

Upload a new module version (requires API key).

Example:

```bash
curl -X POST \
  -H "X-API-Key: your-api-key" \
  -F "file=@module.tgz" \
  https://your-registry-url/v1/module/acme/my_module/1.0.0
```

### List Module Versions

```http
GET /v1/modules/{namespace}/{module_name}/versions
```

Lists all available versions for a specific module.

Example:

```http
GET /v1/modules/acme/my_module/versions
```

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

## Configuration

### Environment Variables

| Variable               | Description                                  | Required |
| ---------------------- | -------------------------------------------- | -------- |
| STORAGE_ACCOUNTNAME    | Azure Storage account name                   | Yes      |
| STORAGE_ACCESS_KEY     | Azure Storage access key                     | Yes      |
| API_KEY                | API key for authentication                   | Yes      |
| APP_INSIGHT_KEY        | Application Insights connection string       | No       |
| ASPNETCORE_ENVIRONMENT | Runtime environment (defaults to Production) | No       |

### Environment File (.env)

Create a `.env` file with the following content (add to .gitignore):

```env
STORAGE_ACCOUNTNAME=your_account_name
STORAGE_ACCESS_KEY=your_access_key
API_KEY=your_api_key
APP_INSIGHT_KEY=your_app_insights_key (optional)
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
  source  = "your-registry-url/acme/my_module/azure"
  version = "1.0.0"
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
