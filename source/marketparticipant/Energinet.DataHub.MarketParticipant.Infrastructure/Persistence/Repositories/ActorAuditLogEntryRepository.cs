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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.EntityConfiguration;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class ActorAuditLogEntryRepository : IActorAuditLogEntryRepository
    {
        private readonly IMarketParticipantDbContext _context;

        public ActorAuditLogEntryRepository(IMarketParticipantDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ActorAuditLogEntry>> GetAsync(ActorId actor)
        {
            var historicEntities = await _context.Actors
                .ReadAllHistoryForAsync(entity => entity.Id == actor.Value)
                .ConfigureAwait(false);

            var historicEntitiesContacts = await _context.ActorContacts
                .ReadAllHistoryForAsync(entity => entity.ActorId == actor.Value && entity.Category == ContactCategory.Default)
                .ConfigureAwait(false);

            var auditedProperties = new[]
            {
                new
                {
                    Property = ActorChangeType.Name,
                    ReadValue = new Func<ActorEntity, object?>(entity => entity.Name)
                },
                new
                {
                    Property = ActorChangeType.Status,
                    ReadValue = new Func<ActorEntity, object?>(entity => entity.Status)
                },
            };

            var auditedPropertiesContacts = new[]
            {
                new
                {
                    Property = ActorChangeType.ContactName,
                    ReadValue = new Func<ActorContactEntity, object?>(entity => entity.Name ?? string.Empty)
                },
                new
                {
                    Property = ActorChangeType.ContactEmail,
                    ReadValue = new Func<ActorContactEntity, object?>(entity => entity.Email ?? string.Empty)
                },
                new
                {
                    Property = ActorChangeType.ContactPhone,
                    ReadValue = new Func<ActorContactEntity, object?>(entity => entity.Phone ?? string.Empty)
                },
            };

            var auditEntries = new List<ActorAuditLogEntry>();

            for (var i = 0; i < historicEntities.Count; i++)
            {
                var isFirst = i == 0;
                var current = historicEntities[i];
                var previous = isFirst ? current : historicEntities[i - 1];

                foreach (var auditedProperty in auditedProperties)
                {
                    var currentValue = auditedProperty.ReadValue(current.Entity);
                    var previousValue = auditedProperty.ReadValue(previous.Entity);

                    if (!Equals(currentValue, previousValue) || isFirst)
                    {
                        auditEntries.Add(new ActorAuditLogEntry(
                            actor,
                            new AuditIdentity(current.Entity.ChangedByIdentityId),
                            auditedProperty.Property,
                            current.PeriodStart,
                            currentValue?.ToString() ?? string.Empty));
                    }
                }
            }

            for (var i = 0; i < historicEntitiesContacts.Count; i++)
            {
                var isFirst = i == 0;
                var current = historicEntitiesContacts[i];
                var previous = isFirst ? current : historicEntitiesContacts[i - 1];

                foreach (var auditedProperty in auditedPropertiesContacts)
                {
                    var currentValue = auditedProperty.ReadValue(current.Entity);
                    var previousValue = auditedProperty.ReadValue(previous.Entity);

                    if (!Equals(currentValue, previousValue) || isFirst)
                    {
                        auditEntries.Add(new ActorAuditLogEntry(
                            actor,
                            new AuditIdentity(current.Entity.ChangedByIdentityId),
                            auditedProperty.Property,
                            current.PeriodStart,
                            currentValue?.ToString() ?? string.Empty));
                    }
                }
            }

            return auditEntries.OrderBy(entry => entry.Timestamp).ToList();
        }
    }
}
