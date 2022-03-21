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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers
{
    public sealed class GetOrganizationsHandler : IRequestHandler<GetOrganizationsCommand, GetOrganizationsResponse>
    {
        private readonly IOrganizationRepository _organizationRepository;

        public GetOrganizationsHandler(IOrganizationRepository organizationRepository)
        {
            _organizationRepository = organizationRepository;
        }

        public async Task<GetOrganizationsResponse> Handle(GetOrganizationsCommand request, CancellationToken cancellationToken)
        {
            var organizations = await _organizationRepository
                .GetAsync()
                .ConfigureAwait(false);

            var mapped = organizations
                .Select(Map)
                .ToList();

            return new GetOrganizationsResponse(mapped);
        }

        private static OrganizationDto Map(Organization organization)
        {
            return new OrganizationDto(
                organization.Id.ToString(),
                organization.Name,
                organization.Actors.Select(Map).ToList());
        }

        private static ActorDto Map(Actor actor)
        {
            return new ActorDto(
                actor.Id.ToString(),
                actor.ExternalActorId.ToString(),
                new GlobalLocationNumberDto(actor.Gln.ToString()),
                actor.Status.ToString(),
                actor.MarketRoles.Select(Map).ToList());
        }

        private static MarketRoleDto Map(MarketRole marketRole)
        {
            return new MarketRoleDto(marketRole.Function.ToString());
        }
    }
}
