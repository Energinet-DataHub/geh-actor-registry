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
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Permissions;

public sealed class UpdatePermissionHandler : IRequestHandler<UpdatePermissionCommand>
{
    private readonly IPermissionRepository _permissionRepository;

    public UpdatePermissionHandler(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var permissionToUpdate = await _permissionRepository
            .GetAsync((PermissionId)request.PermissionId)
            .ConfigureAwait(false);

        if (permissionToUpdate == null)
        {
            throw new ValidationException($"Permission not found: {request.PermissionId}")
                .WithErrorCode("not_found")
                .WithArgs(("id", (PermissionId)request.PermissionId));
        }

        permissionToUpdate.Description = request.Description;

        await _permissionRepository
            .UpdatePermissionAsync(permissionToUpdate)
            .ConfigureAwait(false);
    }
}
