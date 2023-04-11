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
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class UpdateUserRolesHandler
    : IRequestHandler<UpdateUserRoleAssignmentsCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleAssignmentAuditLogEntryRepository _userRoleAssignmentAuditLogEntryRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserContext<FrontendUser> _userContext;

    public UpdateUserRolesHandler(
        IUserRepository userRepository,
        IUserRoleAssignmentAuditLogEntryRepository userRoleAssignmentAuditLogEntryRepository,
        IUserContext<FrontendUser> userContext,
        IUserRoleRepository userRoleRepository)
    {
        _userRepository = userRepository;
        _userRoleAssignmentAuditLogEntryRepository = userRoleAssignmentAuditLogEntryRepository;
        _userContext = userContext;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<Unit> Handle(
        UpdateUserRoleAssignmentsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository
            .GetAsync(new UserId(request.UserId))
            .ConfigureAwait(false);

        if (user == null)
        {
            throw new NotFoundValidationException(request.UserId);
        }

        foreach (var addRequest in request.Assignments.Added)
        {
            var userRoleId = new UserRoleId(addRequest);
            var userRoleAssignment = new UserRoleAssignment(new ActorId(request.ActorId), userRoleId);
            if (user.RoleAssignments.Contains(userRoleAssignment))
                continue;

            var userRole = await _userRoleRepository.GetAsync(userRoleId).ConfigureAwait(false);
            switch (userRole)
            {
                case null:
                    throw new ValidationException($"User role with id {userRoleId} does not exist and can't be added as a role");
                case { Status: not UserRoleStatus.Active }:
                    throw new ValidationException($"User role with name {userRole.Name} is not in active status and can't be added as a role");
                case { Status: UserRoleStatus.Active }:
                    {
                        user.RoleAssignments.Add(userRoleAssignment);

                        await AuditRoleAssignmentAsync(
                                user,
                                userRoleAssignment,
                                UserRoleAssignmentTypeAuditLog.Added)
                            .ConfigureAwait(false);
                        break;
                    }
            }
        }

        foreach (var removeRequest in request.Assignments.Removed)
        {
            var userRoleAssignment = new UserRoleAssignment(new ActorId(request.ActorId), new UserRoleId(removeRequest));
            if (user.RoleAssignments.Remove(userRoleAssignment))
            {
                await AuditRoleAssignmentAsync(
                        user,
                        userRoleAssignment,
                        UserRoleAssignmentTypeAuditLog.Removed)
                    .ConfigureAwait(false);
            }
        }

        await _userRepository.AddOrUpdateAsync(user).ConfigureAwait(false);

        return Unit.Value;
    }

    private async Task AuditRoleAssignmentAsync(
        Domain.Model.Users.User user,
        UserRoleAssignment userRoleAssignment,
        UserRoleAssignmentTypeAuditLog userRoleAssignmentTypeAuditLog)
    {
        await _userRoleAssignmentAuditLogEntryRepository.InsertAuditLogEntryAsync(
            user.Id,
            new UserRoleAssignmentAuditLogEntry(
                user.Id,
                userRoleAssignment.ActorId,
                userRoleAssignment.UserRoleId,
                new UserId(_userContext.CurrentUser.UserId),
                DateTimeOffset.UtcNow,
                userRoleAssignmentTypeAuditLog)).ConfigureAwait(false);
    }
}
