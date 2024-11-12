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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class DomainEventRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public DomainEventRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EnqueueAsync_NoEvents_DoesNothing()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IDomainEventRepository>();

        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.Active,
            null,
            new ActorName(string.Empty),
            null);

        // Act
        await target.EnqueueAsync(actor);

        // Assert
        await context.DomainEvents.AllAsync(domainEvent => domainEvent.EntityId != actor.Id.Value);
    }

    [Fact]
    public async Task EnqueueAsync_WithEvents_EventsSaved()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IDomainEventRepository>();

        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            null,
            new ActorName(string.Empty),
            null);

        actor.SetMarketRole(new ActorMarketRole(EicFunction.GridAccessProvider, new[]
        {
            new ActorGridArea(new GridAreaId(Guid.NewGuid()), Array.Empty<MeteringPointType>())
        }));

        actor.Activate();

        // Act
        await target.EnqueueAsync(actor);

        // Assert
        await context.DomainEvents.SingleAsync(domainEvent =>
            domainEvent.EntityId == actor.Id.Value &&
            domainEvent.EntityType == "Actor" &&
            domainEvent.EventTypeName == "GridAreaOwnershipAssigned");
    }

    [Fact]
    public async Task EnqueueAsync_WithEvents_MultiGridArea_EventsSaved()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IDomainEventRepository>();

        var actor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            null,
            new ActorName(string.Empty),
            null);

        actor.SetMarketRole(new ActorMarketRole(EicFunction.GridAccessProvider, new[]
        {
            new ActorGridArea(new GridAreaId(Guid.NewGuid()), Array.Empty<MeteringPointType>()),
            new ActorGridArea(new GridAreaId(Guid.NewGuid()), Array.Empty<MeteringPointType>())
        }));

        actor.Activate();

        // Act
        await target.EnqueueAsync(actor);

        // Assert
        var events = context.DomainEvents.Where(domainEvent =>
            domainEvent.EntityId == actor.Id.Value &&
            domainEvent.EntityType == "Actor" &&
            domainEvent.EventTypeName == "GridAreaOwnershipAssigned");

        Assert.Equal(2, events.Count());
    }

    [Fact]
    public async Task EnqueueAsync_NotificationEvent_EventsSaved()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = scope.ServiceProvider.GetRequiredService<IDomainEventRepository>();
        var notification = new BalanceResponsibilityValidationFailed(new ActorId(Guid.NewGuid()), new MockedGln(), true);

        // Act
        await target.EnqueueAsync(notification);

        // Assert
        var events = context.DomainEvents.Where(domainEvent =>
            domainEvent.EntityId == notification.EventId &&
            domainEvent.EntityType == nameof(NotificationEvent) &&
            domainEvent.EventTypeName == "BalanceResponsibilityValidationFailed");

        Assert.Single(events);
    }
}
