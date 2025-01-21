module "tresrm_resource_group" {
  source   = "https://localhost:7056/api/v1/module/hanze/resource_group/1.0.0"
  name     = "rg-test"
  location = "westeurope"
  tags = {
    environment = "dev"
  }
}

