# tfprivate - Private Terraform Module Registry

A private Terraform module registry implementation that allows you to host and manage your own Terraform modules. The registry provides a REST API interface compatible with Terraform's registry protocol, backed by Azure Blob Storage for module storage.

## Features

- Private module hosting with Azure Blob Storage
- REST API compatible with Terraform's registry protocol
- Semantic versioning support for modules
- Azure Application Insights integration for monitoring
- Secure module access with SAS token generation
- Support for organization-based module grouping

## Architecture

The system consists of:

- .NET Core Web API (tfprivate.Api)
- Azure Storage Account for module storage
- Azure Application Insights for monitoring
- Azure Log Analytics workspace for logging

## Prerequisites

- .NET 7.0 or higher
- Azure subscription
- Azurite (for local development)
- Terraform 1.0 or higher

## Local Development Setup

1. Start Azurite for local storage emulation:

```bash
azurite --silent --location . --debug .
```

2. Configure environment variables or update `appsettings.json` with:

   - Azure Storage connection string
   - Azure Application Insights connection string

3. Run the API:

```bash
dotnet run --project tfprivate.Api
```

The API will be available at:

- HTTPS: https://localhost:7056
- HTTP: http://localhost:5260

## Infrastructure Deployment

The project includes Terraform configurations to deploy the required Azure infrastructure:

1. Navigate to the infrastructure directory:

```bash
cd infra/dev
```

2. Initialize Terraform:

```bash
terraform init
```

3. Create a `dev.tfvars` file with your Azure credentials:

```hcl
subscription_id     = "your-subscription-id"
tenant_id          = "your-tenant-id"
arm_client_id      = "your-client-id"
arm_client_secret  = "your-client-secret"
environment        = "dev"
location           = "West Europe"
```

4. Add the registry source url

```hcl
module "tresrm_resource_group" {
  source   = "https://localhost:7056/api/v1/module/hanze/resource_group/1.0.0"
  name     = "rg-test"
  location = "westeurope"
  tags = {
    environment = "dev"
  }
}


```

5. Deploy the infrastructure:

```bash
terraform apply -var-file=dev.tfvars
```

## API Endpoints

### List Modules

```
GET /api/v1/modules/{org}
```

Lists all modules for a specific organization.

### Get Module

```
GET /api/v1/module/{org}/{module}/{version}
```

Retrieves a specific version of a module. Version must follow semantic versioning (e.g., 1.0.0).

## Module Structure

Modules should be packaged as `.tgz` archives with the following structure:

```
{org}/{module}/v{version}/module.tgz
```

Example:

```
tres/tresrm_resource_group/v0.1.0/module.tgz
```

## CI/CD Integration

During the CI/CD pipeline:

1. Package your Terraform module as a `.tgz` archive
2. Upload the archive to the storage account using the provided API
3. Version the module following semantic versioning principles

## Security

- All module access is controlled through Azure Storage SAS tokens
- Modules are stored in a private Azure Storage container
- HTTPS is enforced for all API communications
- Azure AD integration can be implemented for additional security

## Monitoring

The application uses Azure Application Insights for:

- Request tracking
- Performance monitoring
- Error logging
- Custom metrics

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

[Add your license information here]
