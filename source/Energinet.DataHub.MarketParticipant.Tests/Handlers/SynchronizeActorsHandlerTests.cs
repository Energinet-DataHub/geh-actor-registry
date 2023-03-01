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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents.ActorIntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class SynchronizeActorsHandlerTests
{
    [Fact]
    public async Task Handle_NoSync_DoesNothing()
    {
        // Arrange
        var target = new SynchronizeActorsHandler(
            UnitOfWorkProviderMock.Create(),
            new Mock<IActorRepository>().Object,
            new Mock<IActorIntegrationEventsQueueService>().Object,
            new Mock<IExternalActorIdConfigurationService>().Object,
            new Mock<IExternalActorSynchronizationRepository>().Object);

        // Act + Assert
        await target.Handle(new SynchronizeActorsCommand(), default);
    }

    [Fact]
    public async Task Handle_SingleSync_AssignsExternalId()
    {
        // Arrange
        var actorId = Guid.NewGuid();

        var actor = new Actor(
            new ActorId(actorId),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            Array.Empty<ActorMarketRole>(),
            new ActorName("fake_value"));

        var externalActorSynchronizationRepository = new Mock<IExternalActorSynchronizationRepository>();
        externalActorSynchronizationRepository
            .Setup(x => x.DequeueNextAsync())
            .ReturnsAsync(actorId);

        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
            .ReturnsAsync(actor);

        var externalActorIdConfigurationService = new Mock<IExternalActorIdConfigurationService>();

        var target = new SynchronizeActorsHandler(
            UnitOfWorkProviderMock.Create(),
            actorRepositoryMock.Object,
            new Mock<IActorIntegrationEventsQueueService>().Object,
            externalActorIdConfigurationService.Object,
            externalActorSynchronizationRepository.Object);

        // Act
        await target.Handle(new SynchronizeActorsCommand(), default);

        // Assert
        externalActorIdConfigurationService.Verify(x => x.AssignExternalActorIdAsync(actor), Times.Once);
    }

    [Fact]
    public async Task Handle_SingleSync_RaisesExternalIdChanged()
    {
        // Arrange
        var externalActorId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var actor = new Actor(
            new ActorId(actorId),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            Array.Empty<ActorMarketRole>(),
            new ActorName("fake_value"));

        var externalActorSynchronizationRepository = new Mock<IExternalActorSynchronizationRepository>();
        externalActorSynchronizationRepository
            .Setup(x => x.DequeueNextAsync())
            .ReturnsAsync(actorId);

        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
            .ReturnsAsync(actor);

        var externalActorIdConfigurationService = new Mock<IExternalActorIdConfigurationService>();
        externalActorIdConfigurationService
            .Setup(x => x.AssignExternalActorIdAsync(It.IsAny<Actor>()))
            .Callback((Actor a) => a.ExternalActorId = new ExternalActorId(externalActorId));

        var actorIntegrationEventsQueueService = new Mock<IActorIntegrationEventsQueueService>();

        var target = new SynchronizeActorsHandler(
            UnitOfWorkProviderMock.Create(),
            actorRepositoryMock.Object,
            actorIntegrationEventsQueueService.Object,
            externalActorIdConfigurationService.Object,
            externalActorSynchronizationRepository.Object);

        // Act
        await target.Handle(new SynchronizeActorsCommand(), default);

        // Assert
        actorIntegrationEventsQueueService.Verify(
            x => x.EnqueueActorUpdatedEventAsync(
                actor.Id,
                It.Is<IEnumerable<IIntegrationEvent>>(events => events.OfType<ActorExternalIdChangedIntegrationEvent>().Any())),
            Times.Once);
    }
}
