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

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;

public enum PermissionId
{
    OrganizationsManage = 2,
    GridAreasManage = 3,
    ActorsManage = 4,
    UsersManage = 5,
    UsersView = 6,
    UserRolesManage = 7,
    PermissionsManage = 8,
    CalculationsManage = 9,
    SettlementReportsManage = 10,
    ESettExchangeManage = 11,
    RequestAggregatedMeasureData = 12,
    ActorCredentialsManage = 13
}
