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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class DomainEventRepository : IDomainEventRepository
    {
        private readonly IMarketParticipantDbContext _context;
        private readonly IDictionary<string, Type> _domainEventTypesByName;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public DomainEventRepository(IMarketParticipantDbContext context)
        {
            _context = context;
            _domainEventTypesByName = typeof(IIntegrationEvent).Assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(IIntegrationEvent))).ToDictionary(x => x.Name);
        }

        public async Task InsertAsync(DomainEvent domainEvent)
        {
            Guard.ThrowIfNull(domainEvent, nameof(domainEvent));

            using var ms = new MemoryStream();

            await JsonSerializer.SerializeAsync(ms, domainEvent.IntegrationEvent, _jsonSerializerOptions).ConfigureAwait(false);

            var entity = new DomainEventEntity
            {
                EntityId = domainEvent.DomainObjectId,
                EntityType = domainEvent.DomainObjectType,
                Timestamp = DateTimeOffset.UtcNow,
                Event = Encoding.UTF8.GetString(ms.ToArray()),
                EventTypeName = domainEvent.IntegrationEvent.GetType().Name
            };

            await _context.DomainEvents.AddAsync(entity).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async IAsyncEnumerable<DomainEvent> GetOldestUnsentDomainEventsAsync(int numberOfEvents)
        {
            var q = from x in _context.DomainEvents.AsQueryable()
                    where x.IsSent == false
                    orderby x.Id ascending
                    select x;

            await foreach (var x in q.Take(numberOfEvents).AsAsyncEnumerable())
            {
                if (!_domainEventTypesByName.TryGetValue(x.EventTypeName, out var type))
                {
                    throw new InvalidOperationException($"EventType {x.EventTypeName} not found in assembly");
                }

                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(x.Event));

                var domainEvent = await JsonSerializer.DeserializeAsync(ms, type, _jsonSerializerOptions).ConfigureAwait(false);

                if (domainEvent == null)
                {
                    throw new InvalidOperationException($"DomainEvent with ID: {x.Id} could not be deserialized");
                }

                yield return DomainEvent.Restore(x.Id, x.EntityId, x.EntityType, x.IsSent, (IIntegrationEvent)domainEvent);
            }
        }
    }
}
