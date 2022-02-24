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
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class GridAreaRepositoryTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;

        public GridAreaRepositoryTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddOrUpdateAsync_GridNotExists_ReturnsNull()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);

            // Act
            var testOrg = await orgRepository
                .GetAsync(new OrganizationId(Guid.NewGuid()))
                .ConfigureAwait(false);

            // Assert
            Assert.Null(testOrg);
        }

        [Fact]
        public async Task AddOrUpdateAsync_OneGridArea_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var gridRepository = new GridAreaRepository(context);
            var testGrid = new GridArea(
                new GridAreaId(Guid.Empty),
                new GridAreaName("Test Grid Area"),
                new GridAreaCode("801"));

            // Act
            var gridId = await gridRepository.AddOrUpdateAsync(testGrid).ConfigureAwait(false);
            var newGrid = await gridRepository.GetAsync(gridId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(newGrid);
            Assert.NotEqual(Guid.Empty, newGrid?.Id.Value);
            Assert.Equal(testGrid.Name.Value, newGrid?.Name.Value);
            Assert.Equal(testGrid.Code, newGrid?.Code);
        }

        [Fact]
        public async Task AddOrUpdateAsync_GridAreaChanged_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var gridRepository = new GridAreaRepository(context);
            var testGrid = new GridArea(
                new GridAreaId(Guid.NewGuid()),
                new GridAreaName("Test Grid Area"),
                new GridAreaCode("801"));

            // Act
            var gridId = await gridRepository.AddOrUpdateAsync(testGrid).ConfigureAwait(false);
            var newGrid = await gridRepository.GetAsync(gridId).ConfigureAwait(false);
            newGrid = newGrid! with { Code = new GridAreaCode("234"), Name = new GridAreaName("NewName") };
            await gridRepository.AddOrUpdateAsync(newGrid).ConfigureAwait(false);
            newGrid = await gridRepository.GetAsync(gridId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(newGrid);
            Assert.NotEqual(Guid.Empty, newGrid?.Id.Value);
            Assert.Equal("234", newGrid?.Code.Value);
            Assert.Equal("NewName", newGrid?.Name.Value);
        }
    }
}
