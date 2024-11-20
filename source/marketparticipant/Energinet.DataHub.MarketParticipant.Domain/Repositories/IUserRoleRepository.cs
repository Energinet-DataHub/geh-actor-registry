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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

/// <summary>
/// Provides access to user roles.
/// </summary>
public interface IUserRoleRepository
{
    /// <summary>
    /// Returns all existing user roles.
    /// </summary>
    /// <returns>The all existing user roles.</returns>
    Task<IEnumerable<UserRole>> GetAllAsync();

    /// <summary>
    /// Gets the user role having the specified external id.
    /// </summary>
    /// <param name="userRoleId">The id of the user role.</param>
    /// <returns>The role if it exists; otherwise null.</returns>
    Task<UserRole?> GetAsync(UserRoleId userRoleId);

    /// <summary>
    /// Gets the user roles by name.
    /// </summary>
    /// <param name="userRoleName">The name to search for.</param>
    /// <param name="marketRole">The market role to search for.</param>
    /// <returns>A list of user roles with the specified name and market role.</returns>
    Task<IEnumerable<UserRole>> GetByNameInMarketRoleAsync(string userRoleName, EicFunction marketRole);

    /// <summary>
    /// Gets user roles that support the specified EIC-functions.
    /// </summary>
    /// <param name="eicFunctions">The list of EIC-functions the user role must support.</param>
    /// <returns>A list of user roles.</returns>
    Task<IEnumerable<UserRole>> GetAsync(IEnumerable<EicFunction> eicFunctions);

    /// <summary>
    /// Create a new user role.
    /// </summary>
    /// <param name="userRole">The user role to Create.</param>
    /// <returns>The id of the created role.</returns>
    Task<UserRoleId> AddAsync(UserRole userRole);

    /// <summary>
    /// Updates an existing user role.
    /// </summary>
    /// <param name="userRoleUpdate">The user role to update.</param>
    Task UpdateAsync(UserRole userRoleUpdate);

    /// <summary>
    /// Gets user roles that have the given permission assigned to them.
    /// </summary>
    /// <param name="permission">The permission you want to get user roles for.</param>
    /// <returns>A list of user roles.</returns>
    Task<IEnumerable<UserRole>> GetAsync(PermissionId permission);
}
