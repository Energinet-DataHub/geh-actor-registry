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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public PermissionRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<IEnumerable<PermissionDetails>> GetAllAsync()
    {
        var permissions = await BuildPermissionQuery(null).ToListAsync().ConfigureAwait(false);
        return permissions.Select(MapToPermissionDetails);
    }

    public async Task<IEnumerable<PermissionDetails>> GetToMarketRoleAsync(EicFunction eicFunction)
    {
        var permissions = await BuildPermissionQuery(eicFunction).ToListAsync().ConfigureAwait(false);
        return permissions.Select(MapToPermissionDetails);
    }

    public async Task<PermissionDetails?> GetAsync(Permission permission)
    {
        var permissionEntity = await _marketParticipantDbContext.Permissions
            .FirstOrDefaultAsync(p => p.Id == (int)permission)
            .ConfigureAwait(false);

        if (permissionEntity == null)
        {
            return null;
        }

        return MapToPermissionDetails(permissionEntity);
    }

    public async Task<IEnumerable<EicFunction>> GetAssignedToMarketRolesAsync(Permission permission)
    {
        var query =
            from p in _marketParticipantDbContext.Permissions
            where p.Id == (int)permission
            select p.EicFunctions;

        return (await query
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false))
        .SelectMany(x => x.Select(y => y.EicFunction))
        .Distinct();
    }

    public async Task UpdatePermissionAsync(PermissionDetails permissionDetails)
    {
        ArgumentNullException.ThrowIfNull(permissionDetails);
        var permissionId = (int)permissionDetails.Permission;
        var permission = await _marketParticipantDbContext.Permissions.FirstAsync(p => p.Id == permissionId).ConfigureAwait(false);
        permission.Description = permissionDetails.Description;
        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private PermissionDetails MapToPermissionDetails(PermissionEntity permissionEntity)
    {
        return new PermissionDetails(
            (Permission)permissionEntity.Id,
            permissionEntity.Description,
            permissionEntity.EicFunctions.Select(y => y.EicFunction),
            permissionEntity.Created);
    }

    private IQueryable<PermissionEntity> BuildPermissionQuery(EicFunction? eicFunction)
    {
        var query =
            from p in _marketParticipantDbContext.Permissions
            where eicFunction == null || p.EicFunctions.Any(x => x.EicFunction == eicFunction)
            select p;

        return query;
    }
}
