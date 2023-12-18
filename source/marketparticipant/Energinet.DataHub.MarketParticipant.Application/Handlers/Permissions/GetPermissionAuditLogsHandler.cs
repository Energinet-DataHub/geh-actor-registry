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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Permissions;

public sealed class GetPermissionAuditLogsHandler : IRequestHandler<GetPermissionAuditLogsCommand, PermissionAuditLogsResponse>
{
    private readonly IPermissionAuditLogRepository _permissionAuditLogsRepository;

    public GetPermissionAuditLogsHandler(IPermissionAuditLogRepository permissionAuditLogsRepository)
    {
        _permissionAuditLogsRepository = permissionAuditLogsRepository;
    }

    public async Task<PermissionAuditLogsResponse> Handle(GetPermissionAuditLogsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditLogs = await _permissionAuditLogsRepository
            .GetAsync((PermissionId)request.PermissionId)
            .ConfigureAwait(false);

        return new PermissionAuditLogsResponse(auditLogs.Select(log => new AuditLogDto<PermissionAuditedChange>(log)));
    }
}
