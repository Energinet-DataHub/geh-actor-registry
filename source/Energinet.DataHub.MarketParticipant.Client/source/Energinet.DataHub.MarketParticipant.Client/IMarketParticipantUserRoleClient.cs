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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Client.Models;

namespace Energinet.DataHub.MarketParticipant.Client
{
    /// <summary>
    /// Manages user roles.
    /// </summary>
    public interface IMarketParticipantUserRoleClient
    {
        /// <summary>
        /// Gets the specified user role.
        /// </summary>
        /// <param name="userRoleId">The id of the user role.</param>
        /// <returns>The specified user role.</returns>
        Task<UserRoleWithPermissionsDto> GetAsync(Guid userRoleId);

        /// <summary>
        /// Returns all user roles with basic information
        /// </summary>
        /// <returns>User roles</returns>
        Task<IEnumerable<UserRoleDto>> GetAllAsync();

        /// <summary>
        /// Gets user roles assigned to the specified user and actor.
        /// </summary>
        /// <param name="actorId">The id of the actor.</param>
        /// <param name="userId">The id of the user.</param>
        /// <returns>The list of currently assigned user roles.</returns>
        Task<IEnumerable<UserRoleDto>> GetAsync(Guid actorId, Guid userId);

        /// <summary>
        /// Gets all user roles that can be assigned to the specified actor.
        /// </summary>
        /// <param name="actorId">The id of the actor.</param>
        /// <returns>The list of assignable user roles.</returns>
        Task<IEnumerable<UserRoleDto>> GetAssignableAsync(Guid actorId);

        /// <summary>
        /// Creates a new user role
        /// </summary>
        /// <param name="userRoleDto">Details for the user role that is to be created</param>
        /// <returns>The id <see cref="Guid"/> of the user role created</returns>
        Task<Guid> CreateAsync(CreateUserRoleDto userRoleDto);

        /// <summary>
        /// Update a user role
        /// </summary>
        /// <param name="userRoleId">user role id for the entity to update</param>
        /// <param name="userRoleUpdateDto">Details for the user role to update</param>
        Task UpdateAsync(Guid userRoleId, UpdateUserRoleDto userRoleUpdateDto);

        /// <summary>
        /// Returns user role audit logs
        /// </summary>
        /// <param name="userRoleId">user role id for audit logs to return</param>
        /// <returns>Collections of user role audit logs</returns>
        Task<IEnumerable<UserRoleAuditLogEntryDto>> GetUserRoleAuditLogsAsync(Guid userRoleId);

        /// <summary>
        /// Returns all the permissions in the system that can be selected when creating a user role
        /// </summary>
        /// <param name="eicFunction">The Eic Function to get permissions for</param>
        /// <returns>Collection of permission</returns>
        Task<IEnumerable<PermissionDetailsDto>> GetPermissionDetailsAsync(EicFunction eicFunction);

        /// <summary>
        /// Gets user roles that have the given permission assigned to them
        /// </summary>
        /// <param name="permissionId">The permission you want to get user roles for.</param>
        /// <returns>A list of user roles.</returns>
        Task<IEnumerable<UserRoleDto>> GetAssignedToPermissionAsync(int permissionId);
    }
}
