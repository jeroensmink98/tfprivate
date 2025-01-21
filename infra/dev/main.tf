resource "random_string" "storage_account_suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "azurerm_resource_group" "main" {
  name     = "rg-tfmodules"
  location = "West Europe"

  tags = {
    environment = "dev"
  }
}

resource "azurerm_storage_account" "main" {
  name                     = "satfmodules${random_string.storage_account_suffix.result}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "main" {
  name                  = "modules"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}
