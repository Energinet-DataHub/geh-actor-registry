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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class CreateActorHandlerTests
{
    [Fact]
    public async Task Handle_NewActor_ActorIdReturned()
    {
        // Arrange
        var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
        var actorFactory = new Mock<IActorFactoryService>();
        var target = new CreateActorHandler(
            organizationExistsHelperService.Object,
            actorFactory.Object);

        var organization = TestPreparationModels.MockedOrganization();
        var actor = TestPreparationModels.MockedActor(Guid.NewGuid(), organization.Id.Value);

        organizationExistsHelperService
            .Setup(x => x.EnsureOrganizationExistsAsync(organization.Id.Value))
            .ReturnsAsync(organization);

        actorFactory
            .Setup(x => x.CreateAsync(
                organization,
                It.Is<ActorNumber>(y => y.Value == actor.ActorNumber.Value),
                It.Is<ActorName>(y => y.Value == string.Empty),
                It.IsAny<ActorMarketRole?>()))
            .ReturnsAsync(actor);

        var command = new CreateActorCommand(new CreateActorDto(
            organization.Id.Value,
            new ActorNameDto(string.Empty),
            new ActorNumberDto(actor.ActorNumber.Value),
            Array.Empty<ActorMarketRoleDto>()));

        // Act
        var response = await target.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(actor.Id.Value, response.ActorId);
    }

    [Fact]
    public async Task Handle_NewActorWithMarketRoles_ActorIdReturned()
    {
        // Arrange
        string actorGln = new MockedGln();
        var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
        var actorFactory = new Mock<IActorFactoryService>();
        var target = new CreateActorHandler(
            organizationExistsHelperService.Object,
            actorFactory.Object);

        var organization = TestPreparationModels.MockedOrganization();
        var actor = TestPreparationModels.MockedActor(Guid.NewGuid(), organization.Id.Value);

        var marketRole = new ActorMarketRoleDto(EicFunction.BillingAgent, [], string.Empty);

        organizationExistsHelperService
            .Setup(x => x.EnsureOrganizationExistsAsync(organization.Id.Value))
            .ReturnsAsync(organization);

        actorFactory
            .Setup(x => x.CreateAsync(
                organization,
                It.Is<ActorNumber>(y => y.Value == actorGln),
                It.Is<ActorName>(y => y.Value == string.Empty),
                It.IsAny<ActorMarketRole?>()))
            .ReturnsAsync(actor);

        var command = new CreateActorCommand(new CreateActorDto(
            organization.Id.Value,
            new ActorNameDto(string.Empty),
            new ActorNumberDto(actorGln),
            [marketRole]));

        // Act
        var response = await target.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(actor.Id.Value, response.ActorId);
    }

    [Fact]
    public async Task Handle_NewActorWithMarketRoleGridAccessProvider_MultiGridAreas_ActorIdReturned()
    {
        // Arrange
        string actorGln = new MockedGln();
        var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
        var actorFactory = new Mock<IActorFactoryService>();
        var target = new CreateActorHandler(
            organizationExistsHelperService.Object,
            actorFactory.Object);

        var organization = TestPreparationModels.MockedOrganization();
        var actor = TestPreparationModels.MockedActor(Guid.NewGuid(), organization.Id.Value);
        var validMeteringPointTypes = new[] { MeteringPointType.D05NetProduction.ToString() };

        var validGridAreas = new List<ActorGridAreaDto>
        {
            new(Guid.NewGuid(), validMeteringPointTypes),
            new(Guid.NewGuid(), validMeteringPointTypes),
            new(Guid.NewGuid(), validMeteringPointTypes)
        };

        var marketRole = new ActorMarketRoleDto(EicFunction.GridAccessProvider, validGridAreas, string.Empty);

        organizationExistsHelperService
            .Setup(x => x.EnsureOrganizationExistsAsync(organization.Id.Value))
            .ReturnsAsync(organization);

        actorFactory
            .Setup(x => x.CreateAsync(
                organization,
                It.Is<ActorNumber>(y => y.Value == actorGln),
                It.Is<ActorName>(y => y.Value == string.Empty),
                It.IsAny<ActorMarketRole?>()))
            .ReturnsAsync(actor);

        var command = new CreateActorCommand(new CreateActorDto(
            organization.Id.Value,
            new ActorNameDto(string.Empty),
            new ActorNumberDto(actorGln),
            [marketRole]));

        // Act
        var response = await target.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(actor.Id.Value, response.ActorId);
    }
}
