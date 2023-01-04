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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserRoleRepository : IUserRoleRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public UserRoleRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<IEnumerable<UserRole>> GetAllAsync()
    {
        var queryable = BuildUserRoleQuery();

        var selectedUserRoleFunctions = queryable.SelectMany(e => e.EicFunctions.Select(r => new { r.EicFunction, r.UserRoleId, e.Id, e.Name, e.Description, e.Status }));

        var userRoles = selectedUserRoleFunctions
            .Select(r => new UserRole(
                new UserRoleId(r.Id),
                r.Name,
                r.Description,
                r.Status,
                new List<Permission>(),
                r.EicFunction));

        var list = await userRoles.ToListAsync().ConfigureAwait(false);
        return list;
    }

    public async Task<UserRole?> GetAsync(UserRoleId userRoleId)
    {
        var userRole = await BuildUserRoleQuery()
            .SingleOrDefaultAsync(t => t.Id == userRoleId.Value)
            .ConfigureAwait(false);

        return userRole == null
            ? null
            : MapUserRole(userRole);
    }

    public async Task<IEnumerable<UserRole>> GetAsync(IEnumerable<EicFunction> eicFunctions)
    {
        var userRoles = await BuildUserRoleQuery()
            .Where(t => t
                .EicFunctions
                .Select(f => f.EicFunction)
                .All(f => eicFunctions.Contains(f)))
            .ToListAsync()
            .ConfigureAwait(false);

        return userRoles.Select(MapUserRole);
    }

    public async Task<UserRole> CreateAsync(string name, string description, UserRoleStatus status, EicFunction eicFunction)
    {
        ArgumentNullException.ThrowIfNull(eicFunction);
        var role = new UserRoleEntity() { Name = name, Description = description, Status = status };
        role.EicFunctions.Add(new UserRoleEicFunctionEntity() { EicFunction = eicFunction });
        _marketParticipantDbContext.UserRoles.Add(role);
        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
        return MapUserRole(role);
    }

    private static UserRole MapUserRole(UserRoleEntity userRole)
    {
        return new UserRole(
            new UserRoleId(userRole.Id),
            userRole.Name,
            userRole.Description,
            userRole.Status,
            userRole.Permissions.Select(p => p.Permission),
            userRole.EicFunctions.First().EicFunction);
    }

    private IQueryable<UserRoleEntity> BuildUserRoleQuery()
    {
        return _marketParticipantDbContext
            .UserRoles
            .Include(x => x.EicFunctions)
            .Include(x => x.Permissions);
    }
}
