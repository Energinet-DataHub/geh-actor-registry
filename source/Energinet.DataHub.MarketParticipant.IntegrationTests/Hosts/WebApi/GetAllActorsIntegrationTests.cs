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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class GetAllActorsIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public GetAllActorsIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAllActors_WithAnActor_ReturnsTheActor()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();

        var actor = await _fixture.PrepareActorAsync();

        var mediator = scope.GetInstance<IMediator>();

        var command = new GetAllActorsCommand();

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.NotEmpty(response.Actors);
        Assert.Contains(response.Actors, a => a.ActorId == actor.Id.ToString());
    }
}
