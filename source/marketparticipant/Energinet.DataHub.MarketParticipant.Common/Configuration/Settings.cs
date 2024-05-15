﻿// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Energinet.DataHub.MarketParticipant.Common.Configuration;

#pragma warning disable CA1724
public static class Settings
#pragma warning restore CA1724
{
    public static Setting<string> UserInviteFlow { get; }
        = new("USER_INVITE_FLOW");
    public static Setting<string> EnvironmentDescription { get; }
        = new("ENVIRONMENT_DESC");

    public static Setting<string> ServiceBusTopicConnectionString { get; }
        = new("SERVICE_BUS_CONNECTION_STRING");
    public static Setting<string> ServiceBusHealthConnectionString { get; }
        = new("SERVICE_BUS_HEALTH_CHECK_CONNECTION_STRING");
    public static Setting<string> ServiceBusTopicName { get; }
        = new("SBT_MARKET_PARTICIPANT_CHANGED_NAME");

    public static Setting<bool> Enforce2Fa { get; }
        = new("ENFORCE_2FA", true);

    public static Setting<string> CvrBaseAddress { get; }
        = new("CVR_BASE_ADDRESS");
    public static Setting<string> CvrUsername { get; }
        = new("CVR_USERNAME");
    public static Setting<string> CvrPassword { get; }
        = new("CVR_PASSWORD");
    public static Setting<string> OrganizationIdentityUpdateNotificationToEmail { get; }
        = new("CVR_ORG_UPDATED_NOTIFICATION_TO_EMAIL");
    public static Setting<string> BalanceResponsiblePartiesChangedNotificationToEmail { get; }
        = new("BALANCE_RESPONSIBLE_CHANGED_NOTIFICATION_TO_EMAIL");
}
