# Copyright 2020 Energinet DataHub A/S
#
# Licensed under the Apache License, Version 2.0 (the "License2");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
data "azurerm_key_vault" "kv_shared_resources" {
  name                = var.shared_resources_keyvault_name
  resource_group_name = var.shared_resources_resource_group_name
}

data "azurerm_key_vault_secret" "appi_instrumentation_key" {
  name         = "appi-shared-instrumentation-key"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "mssql_data_admin_name" {
  name         = "mssql-data-admin-user-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "mssql_data_admin_password" {
  name         = "mssql-data-admin-user-password"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "mssql_data_url" {
  name         = "mssql-data-url"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "mssql_data_name" {
  name         = "mssql-data-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "sb_domain_relay_send_connection_string" {
  name         = "sb-domain-relay-send-connection-string"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "sbt-market-participant-changed-name" {
  name         = "sbt-market-participant-changed-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "plan_shared_id" {
  name         = "plan-shared-id"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "pdns_resource_group_name" {
  name         = "pdns-resource-group-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "snet_private_endoints_id" {
  name         = "snet-private-endoints-id"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "snet_vnet_integrations_id" {
  name         = "snet-vnet-integrations-id"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}