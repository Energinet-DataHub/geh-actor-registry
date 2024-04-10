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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class GetActorHandlerTests
{
    [Fact]
    public async Task Handle_NoActor_ThrowsNotFoundException()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();
        var target = new GetActorHandler(actorRepositoryMock.Object);

        var actorId = Guid.NewGuid();

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(new ActorId(actorId)))
            .ReturnsAsync((Actor?)null);

        var command = new GetSingleActorCommand(actorId);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HasActor_ReturnsActor()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();
        var target = new GetActorHandler(actorRepositoryMock.Object);

        var actorId = Guid.NewGuid();
        var actor = TestPreparationModels.MockedActor(actorId);

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(actor.Id))
            .ReturnsAsync(actor);

        var command = new GetSingleActorCommand(actorId);

        // Act
        var response = await target.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response.Actor);
        Assert.Equal(actorId, response.Actor.ActorId);
    }
}
