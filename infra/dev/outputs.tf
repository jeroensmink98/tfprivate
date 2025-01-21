output "storage_account_name" {
  value     = azurerm_storage_account.main.name
  sensitive = false
}

output "container_name" {
  value     = azurerm_storage_container.main.name
  sensitive = false
}

output "storage_account_key" {
  value     = azurerm_storage_account.main.primary_access_key
  sensitive = true
}
