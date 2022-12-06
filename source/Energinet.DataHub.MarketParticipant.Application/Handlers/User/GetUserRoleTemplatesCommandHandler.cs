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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoleTemplates;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class GetUserRoleTemplatesCommandHandler
    : IRequestHandler<GetUserRoleTemplatesCommand, GetUserRoleTemplatesResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleTemplateRepository _userRoleTemplateRepository;

    public GetUserRoleTemplatesCommandHandler(
        IUserRepository userRepository,
        IUserRoleTemplateRepository userRoleTemplateRepository)
    {
        _userRepository = userRepository;
        _userRoleTemplateRepository = userRoleTemplateRepository;
    }

    public async Task<GetUserRoleTemplatesResponse> Handle(
        GetUserRoleTemplatesCommand request,
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

        var assignments = user
            .RoleAssignments
            .Where(a => a.ActorId == request.ActorId)
            .Select(x => x.TemplateId)
            .Distinct();

        var templates = new List<UserRoleTemplateDto>();

        foreach (var assignment in assignments)
        {
            var template = await _userRoleTemplateRepository
                .GetAsync(assignment)
                .ConfigureAwait(false);

            if (template != null)
            {
                var userRoleTemplate = new UserRoleTemplateDto(template.Id.Value, template.Name);
                templates.Add(userRoleTemplate);
            }
        }

        return new GetUserRoleTemplatesResponse(templates);
    }
}
