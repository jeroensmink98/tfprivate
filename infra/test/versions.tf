terraform {
  required_version = ">= 1.0.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}


provider "azurerm" {
  features {}
  subscription_id = "fe049d94-a236-4d98-bad0-7d6af5faa66e"
  client_id       = "2cd2023c-2f7b-429a-8b88-a59133d48840"
  client_secret   = "Do78Q~TQnHx~2Ckzjx1RqjyROy-WVuHGJTtnGb-M"
  tenant_id       = "efd34f9b-d9c9-4fdf-8c33-ccfc19e7238e"
}
