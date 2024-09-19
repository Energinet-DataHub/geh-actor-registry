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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class CutoffRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public CutoffRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetCutoff_NeverSet_ReturnsUnixEpoch()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = new CutoffRepository(context);

        // act
        var result = await target.GetCutoffAsync((CutoffType)256);

        // assert
        Assert.Equal(Instant.FromUnixTimeTicks(0), result);
    }

    [Fact]
    public async Task GetCutoff_PreviouslySet_ReturnsSetCutoff()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var target = new CutoffRepository(context);
        var expected = DateTimeOffset.Now.ToInstant();
        await target.UpdateCutoffAsync((CutoffType)512, expected);

        // act
        var result = await target.GetCutoffAsync((CutoffType)512);

        // assert
        Assert.Equal(expected, result);
    }
}
