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

using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers
{
    internal sealed class UserRoleTemplateMapper
    {
        public static void MapToEntity(UserRoleTemplate from, UserRoleTemplateEntity to)
        {
            to.Id = from.Id;
            to.Name = from.Name;

            var permissionEntities = to.Permissions.ToDictionary(x => x.Id);
            var toAdd = new List<UserRoleTemplatePermissionEntity>();
            // foreach (var permission in from.Permissions)
            // {
            //     if (permissionEntities.TryGetValue(permission.Id, out var existing))
            //     {
            //         toAdd.Add(existing);
            //         continue;
            //     }
            //
            //     var test2 = new UserRoleTemplatePermissionEntity(permission.PermissionId, to.Id);
            //     toAdd.Add(test2);
            // }
            to.Permissions.Clear();
            toAdd.ForEach(x => to.Permissions.Add(x));
        }

        // public static UserRoleTemplate MapFromEntity(UserRoleTemplateEntity from)
        // {
        //     return new UserRoleTemplate(
        //         from.Id,
        //         from.Name,
        //         from.Permissions.Select(x => new UserRolePermission() { PermissionId = x.PermissionId }));
        // }
    }
}
