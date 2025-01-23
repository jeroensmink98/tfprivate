module "my_module" {
  source   = "https://localhost/v1/module/acme/something/2.0.0"
  name     = "rg-test"
  location = "westeurope"
  tags = {
    environment = "dev"
  }
}

