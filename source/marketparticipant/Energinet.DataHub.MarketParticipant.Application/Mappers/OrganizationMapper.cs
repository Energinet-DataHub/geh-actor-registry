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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organizations;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Application.Mappers;

public static class OrganizationMapper
{
    public static OrganizationDto Map(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization, nameof(organization));
        return new OrganizationDto(
            organization.Id.Value,
            organization.Name,
            organization.BusinessRegisterIdentifier.Identifier,
            organization.Domains.Select(d => d.Value),
            organization.Status.ToString(),
            Map(organization.Address));
    }

    public static ActorDto Map(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor, nameof(actor));
        return new ActorDto(
            actor.Id.Value,
            actor.OrganizationId.Value,
            actor.Status.ToString(),
            new ActorNumberDto(actor.ActorNumber.Value),
            new ActorNameDto(actor.Name.Value),
            actor.MarketRole is not null ? [Map(actor.MarketRole)] : []);
    }

    private static AddressDto Map(Address address)
    {
        return new AddressDto(
            address.StreetName,
            address.Number,
            address.ZipCode,
            address.City,
            address.Country);
    }

    private static ActorMarketRoleDto Map(ActorMarketRole marketRole)
    {
        return new ActorMarketRoleDto(
            marketRole.Function,
            marketRole.GridAreas.Select(e => new ActorGridAreaDto(e.Id.Value, e.MeteringPointTypes.Select(m => m.ToString()))),
            marketRole.Comment);
    }
}
