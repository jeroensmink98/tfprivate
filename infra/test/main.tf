module "tresrm_resource_group" {
  source   = "https://localhost/v1/module/tres/something/1.0.0"
  name     = "rg-test"
  location = "westeurope"
  tags = {
    environment = "dev"
  }
}

