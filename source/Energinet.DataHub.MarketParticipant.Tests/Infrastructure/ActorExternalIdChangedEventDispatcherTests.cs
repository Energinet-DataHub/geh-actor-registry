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
using Energinet.DataHub.MarketParticipant.Client.Models;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents.ActorIntegrationEvents;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Actor;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Infrastructure;

[UnitTest]
public sealed class ActorExternalIdChangedEventDispatcherTests
{
    [Fact]
    public async Task TryDispatchAsync_integrationEventNull_ThrowsException()
    {
        // Arrange
        await using var serviceBusSenderMock = new MockedServiceBusSender();
        var serviceBusClient = new Mock<IMarketParticipantServiceBusClient>();
        serviceBusClient.Setup(x => x.CreateSender()).Returns(serviceBusSenderMock);

        var eventParser = new ActorExternalIdChangedIntegrationEventParser();
        var sut = new ActorExternalIdChangedEventDispatcher(
            eventParser,
            serviceBusClient.Object);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.TryDispatchAsync(null!));
    }

    [Fact]
    public async Task TryDispatchAsync_SendingCorrectEvent_ReturnsTrue()
    {
        // Arrange
        await using var serviceBusSenderMock = new MockedServiceBusSender();
        var serviceBusClient = new Mock<IMarketParticipantServiceBusClient>();
        serviceBusClient.Setup(x => x.CreateSender()).Returns(serviceBusSenderMock);

        var actorStatusChangedEventParser = new ActorExternalIdChangedIntegrationEventParser();
        var eventParser = new SharedIntegrationEventParser();
        var sut = new ActorExternalIdChangedEventDispatcher(
            actorStatusChangedEventParser,
            serviceBusClient.Object);

        var integrationEvent = new ActorExternalIdChangedIntegrationEvent
        {
            OrganizationId = Guid.NewGuid(),
            ActorId = Guid.NewGuid(),
            ExternalActorId = Guid.NewGuid()
        };

        // Act
        var result = await sut.TryDispatchAsync(integrationEvent).ConfigureAwait(false);
        var message = serviceBusSenderMock.SentMessages.Single();
        var actualEvent =
            eventParser.Parse(message.Body.ToArray()) as
                Integration.Model.Dtos.ActorExternalIdChangedIntegrationEvent;

        // Assert
        Assert.True(result);
        Assert.NotNull(actualEvent);
        Assert.Equal(integrationEvent.Id, actualEvent!.Id);
        Assert.Equal(integrationEvent.OrganizationId, actualEvent.OrganizationId);
        Assert.Equal(integrationEvent.ActorId, actualEvent.ActorId);
        Assert.Equal(integrationEvent.ExternalActorId.ToString(), actualEvent.ExternalActorId.ToString());
    }

    [Fact]
    public async Task TryDispatchAsync_SendingWrongEvent_ReturnsFalse()
    {
        // Arrange
        await using var serviceBusSenderMock = new MockedServiceBusSender();
        var serviceBusClient = new Mock<IMarketParticipantServiceBusClient>();
        serviceBusClient.Setup(x => x.CreateSender()).Returns(serviceBusSenderMock);

        var eventParser = new ActorExternalIdChangedIntegrationEventParser();
        var sut = new ActorExternalIdChangedEventDispatcher(
            eventParser,
            serviceBusClient.Object);

        var integrationEvent = new MarketRoleRemovedFromActorIntegrationEvent
        {
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            ActorId = Guid.NewGuid(),
            BusinessRole = BusinessRoleCode.Ddq,
            MarketRole = EicFunction.EnergySupplier
        };

        // Act
        var result = await sut.TryDispatchAsync(integrationEvent).ConfigureAwait(false);

        // Assert
        Assert.False(result);
    }
}
