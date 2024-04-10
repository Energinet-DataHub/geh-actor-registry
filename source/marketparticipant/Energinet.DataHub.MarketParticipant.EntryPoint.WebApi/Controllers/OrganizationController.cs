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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organizations;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrganizationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserContext<FrontendUser> _userContext;

        public OrganizationController(IMediator mediator, IUserContext<FrontendUser> userContext)
        {
            _mediator = mediator;
            _userContext = userContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrganizationDto>>> ListAllAsync()
        {
            var getOrganizationsCommand = new GetOrganizationsCommand(null);

            var response = await _mediator
                .Send(getOrganizationsCommand)
                .ConfigureAwait(false);

            return Ok(response.Organizations);
        }

        [HttpGet("{organizationId:guid}")]
        public async Task<ActionResult<OrganizationDto>> GetSingleOrganizationAsync(Guid organizationId)
        {
            var getSingleOrganizationCommand = new GetSingleOrganizationCommand(organizationId);

            var response = await _mediator
                .Send(getSingleOrganizationCommand)
                .ConfigureAwait(false);

            return Ok(response.Organization);
        }

        [HttpPost]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult<Guid>> CreateOrganizationAsync(CreateOrganizationDto organization)
        {
            if (!_userContext.CurrentUser.IsFas)
                return Unauthorized();

            var createOrganizationCommand = new CreateOrganizationCommand(organization);

            var response = await _mediator
                .Send(createOrganizationCommand)
                .ConfigureAwait(false);

            return Ok(response.OrganizationId);
        }

        [HttpPut("{organizationId:guid}")]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult> UpdateOrganizationAsync(
            Guid organizationId,
            ChangeOrganizationDto organization)
        {
            if (!_userContext.CurrentUser.IsFas)
                return Unauthorized();

            var updateOrganizationCommand =
                new UpdateOrganizationCommand(organizationId, organization);

            await _mediator
                .Send(updateOrganizationCommand)
                .ConfigureAwait(false);

            return Ok();
        }

        [HttpGet("{organizationId:guid}/actor")]
        public async Task<ActionResult<IEnumerable<ActorDto>>> GetActorsAsync(Guid organizationId)
        {
            var getActorsCommand = new GetActorsCommand(organizationId);

            var response = await _mediator
                .Send(getActorsCommand)
                .ConfigureAwait(false);

            return Ok(response.Actors);
        }

        [HttpGet("{organizationId:guid}/audit")]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult<IEnumerable<AuditLogDto<OrganizationAuditedChange>>>> GetAuditAsync(Guid organizationId)
        {
            var command = new GetOrganizationAuditLogsCommand(organizationId);

            var response = await _mediator
                .Send(command)
                .ConfigureAwait(false);

            return Ok(response.AuditLogs);
        }

        [HttpGet("{businessRegisterIdentifier}/identity")]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult<GetOrganizationIdentityResponse>> GetOrganizationIdentityAsync(string businessRegisterIdentifier)
        {
            var command = new GetOrganizationIdentityCommand(businessRegisterIdentifier);

            var response = await _mediator
                .Send(command)
                .ConfigureAwait(false);

            return Ok(response);
        }
    }
}
