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
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;

internal static class ActorMapper
{
    public static void MapToEntity(Actor from, ActorEntity to)
    {
        to.Id = from.Id.Value;
        to.OrganizationId = from.OrganizationId.Value;
        to.ActorId = from.ExternalActorId?.Value;
        to.ActorNumber = from.ActorNumber.Value;
        to.Status = from.Status;
        to.Name = from.Name.Value;

        // Market roles are currently treated as value types, so they are deleted and recreated with each update.
        to.MarketRoles.Clear();
        foreach (var marketRole in from.MarketRoles)
        {
            var marketRoleEntity = new MarketRoleEntity
            {
                Function = marketRole.Function,
                Comment = marketRole.Comment
            };

            foreach (var marketRoleGridArea in marketRole.GridAreas)
            {
                var gridAreaEntity = new MarketRoleGridAreaEntity
                {
                    GridAreaId = marketRoleGridArea.Id.Value
                };

                foreach (var meteringPointType in marketRoleGridArea.MeteringPointTypes)
                {
                    gridAreaEntity.MeteringPointTypes.Add(new MeteringPointTypeEntity
                    {
                        MeteringTypeId = (int)meteringPointType
                    });
                }

                marketRoleEntity.GridAreas.Add(gridAreaEntity);
            }

            to.MarketRoles.Add(marketRoleEntity);
        }
    }

    public static Actor MapFromEntity(ActorEntity from)
    {
        var marketRoles = from.MarketRoles.Select(marketRole =>
        {
            var function = marketRole.Function;
            var gridAreas = marketRole
                .GridAreas
                .Select(grid => new ActorGridArea(
                    new GridAreaId(grid.GridAreaId),
                    grid.MeteringPointTypes.Select(e => (MeteringPointType)e.MeteringTypeId)));

            return new ActorMarketRole(function, gridAreas.ToList(), marketRole.Comment);
        });

        var actorNumber = ActorNumber.Create(from.ActorNumber);
        var actorStatus = from.Status;
        var actorName = new ActorName(string.IsNullOrWhiteSpace(from.Name) ? "-" : from.Name); // TODO: This check should be removed once we are on new env.

        return new Actor(
            new ActorId(from.Id),
            new OrganizationId(from.OrganizationId),
            from.ActorId.HasValue ? new ExternalActorId(from.ActorId.Value) : null,
            actorNumber,
            actorStatus,
            marketRoles,
            actorName,
            new List<ActorCredentials>() { Map(from.CertificateCredential), Map(from.ClientSecretCredential) });
    }

    private static ActorCredentials Map(ActorClientSecretCredentialsEntity from)
    {
        return new ActorClientSecretCredentials(new CredentialsId(from.Id), new ActorId(from.ActorId));
    }

    private static ActorCredentials Map(ActorCertificateCredentialsEntity from)
    {
        return new ActorCertificateCredentials(
            new CredentialsId(from.Id),
            new ActorId(from.ActorId),
            from.CertificateThumbprint,
            from.KeyVaultSecretIdentifier);
    }
}
