variable "subscription_id" {
  type        = string
  description = "Azure subscription ID"
}

variable "tenant_id" {
  type        = string
  description = "Azure tenant ID"
}

variable "arm_client_id" {
  type        = string
  description = "Azure ARM client ID"
}

variable "arm_client_secret" {
  type        = string
  description = "Azure ARM client secret"
}

variable "environment" {
  type        = string
  description = "Environment name"
}

variable "location" {
  type        = string
  description = "Azure region where resources will be created"
}
