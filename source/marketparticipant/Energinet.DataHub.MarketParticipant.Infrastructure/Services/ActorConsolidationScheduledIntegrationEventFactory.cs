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
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using Google.Protobuf.WellKnownTypes;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

public sealed class ActorConsolidationScheduledIntegrationEventFactory : IIntegrationEventFactory<ActorConsolidationScheduled>
{
    public Task<IntegrationEvent> CreateAsync(ActorConsolidationScheduled domainEvent, int sequenceNumber)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var now = DateTime.UtcNow;
        var permission = KnownPermissions.All.Single(p => p.Id == PermissionId.ActorMasterDataManage).Claim;

        var integrationEvent = new IntegrationEvent(
            domainEvent.EventId,
            Model.Contracts.UserNotificationTriggered.EventName,
            Model.Contracts.UserNotificationTriggered.CurrentMinorVersion,
            new Model.Contracts.UserNotificationTriggered
            {
                ReasonIdentifier = "ActorConsolidationScheduled",
                TargetActorId = domainEvent.Recipient.ToString(),
                TargetPermissions = permission,
                RelatedId = domainEvent.AffectedActorId.Value.ToString(),
                OccurredAt = now.ToTimestamp(),
                ExpiresAt = domainEvent.ScheduledAt.ToTimestamp(),
            });

        return Task.FromResult(integrationEvent);
    }
}
