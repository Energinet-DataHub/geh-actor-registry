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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetActorCredentialsHandlerTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public GetActorCredentialsHandlerTests(MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task GetActorCredentials_WhenCalled_ReturnsActorCredentials()
    {
        // arrange
        var expectedActor = await _databaseFixture.PrepareActorAsync();
        var expectedThumbprint = Guid.NewGuid().ToString();
        await _databaseFixture.AssignActorCredentialsAsync(expectedActor.Id, expectedThumbprint, Guid.NewGuid().ToString());

        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var target = scope.ServiceProvider.GetRequiredService<IMediator>();

        // act
        var actual = await target.Send(new GetActorCredentialsCommand(expectedActor.Id));

        // assert
        Assert.Equal(expectedThumbprint, actual!.CredentialsDto.CertificateCredentials!.Thumbprint);
    }
}
