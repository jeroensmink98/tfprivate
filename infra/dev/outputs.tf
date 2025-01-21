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

output "connection_string" {
  value     = azurerm_storage_account.main.primary_connection_string
  sensitive = true
}

output "application_insights_connection_string" {
  value     = azurerm_application_insights.main.connection_string
  sensitive = true
}
