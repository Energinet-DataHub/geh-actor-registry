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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class CreateUserRoleHandler : IRequestHandler<CreateUserRoleCommand, CreateUserRoleResponse>
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUniqueUserRoleNameRuleService _uniqueUserRoleNameRuleService;
    private readonly IAllowedPermissionsForUserRoleRuleService _allowedPermissionsForUserRoleRuleService;

    public CreateUserRoleHandler(
        IUserRoleRepository userRoleRepository,
        IUniqueUserRoleNameRuleService uniqueUserRoleNameRuleService,
        IAllowedPermissionsForUserRoleRuleService allowedPermissionsForUserRoleRuleService)
    {
        _userRoleRepository = userRoleRepository;
        _uniqueUserRoleNameRuleService = uniqueUserRoleNameRuleService;
        _allowedPermissionsForUserRoleRuleService = allowedPermissionsForUserRoleRuleService;
    }

    public async Task<CreateUserRoleResponse> Handle(
        CreateUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userRole = new UserRole(
            request.UserRoleDto.Name,
            request.UserRoleDto.Description,
            request.UserRoleDto.Status,
            request.UserRoleDto.Permissions.Select(x => (PermissionId)x),
            request.UserRoleDto.EicFunction);

        await _allowedPermissionsForUserRoleRuleService
            .ValidateUserRolePermissionsAsync(userRole)
            .ConfigureAwait(false);

        await _uniqueUserRoleNameRuleService
            .ValidateUserRoleNameAsync(userRole)
            .ConfigureAwait(false);

        var createdUserRoleId = await _userRoleRepository
            .AddAsync(userRole)
            .ConfigureAwait(false);

        return new CreateUserRoleResponse(createdUserRoleId.Value);
    }
}
