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
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public sealed class RevisionActivityWriter : IRevisionActivityWriter
{
    private readonly IRevisionActivityPublisher _revisionActivityPublisher;
    private readonly IAuditIdentityProvider _auditIdentityProvider;

    public RevisionActivityWriter(
        IRevisionActivityPublisher revisionActivityPublisher,
        IAuditIdentityProvider auditIdentityProvider)
    {
        _revisionActivityPublisher = revisionActivityPublisher;
        _auditIdentityProvider = auditIdentityProvider;
    }

    public Task WriteAsync(RevisionActivity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var message = new
        {
            LogId = Guid.NewGuid(),
            UserId = _auditIdentityProvider.IdentityId.Value,
            ActorId = Guid.Empty, // TODO: Do we extend IAuditIdentityProvider to know this?

            OccurredOn = SystemClock.Instance.GetCurrentInstant().ToString(),
            Activity = entity.ActivityName,
            AffectedEntityType = entity.EntityType,
            AffectedEntityKey = entity.EntityKey
        };

        var serializedMessage = JsonSerializer.Serialize(message);
        return _revisionActivityPublisher.PublishAsync(serializedMessage);
    }
}
