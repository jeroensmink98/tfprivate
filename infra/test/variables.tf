variable "subscription_id" {
  type        = string
  description = "The Azure subscription ID"
  sensitive   = true
}

variable "client_id" {
  type        = string
  description = "The Azure client/application ID"
  sensitive   = true
}

variable "client_secret" {
  type        = string
  description = "The Azure client secret"
  sensitive   = true
}

variable "tenant_id" {
  type        = string
  description = "The Azure tenant ID"
  sensitive   = true
}
