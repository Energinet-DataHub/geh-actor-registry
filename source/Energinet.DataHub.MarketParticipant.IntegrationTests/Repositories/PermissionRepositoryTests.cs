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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class PermissionRepositoryTests : IAsyncLifetime
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public PermissionRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddOrUpdateAsync_PermissionAdded_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);
        var permissionRepository2 = new PermissionRepository(context2);
        var testPerm = new Permission("Test_Permission", "Test 1");

        // Act
        await permissionRepository.AddOrUpdateAsync(testPerm);
        var newPermission = await permissionRepository2.GetAsync(testPerm.Id);

        // Assert
        Assert.NotNull(newPermission);
        Assert.NotEqual(string.Empty, newPermission?.Id);
        Assert.Equal(testPerm.Id, newPermission?.Id);
        Assert.Equal(testPerm.Description, newPermission?.Description);
    }

    [Fact]
    public async Task AddOrUpdateAsync_PermissionUpdated_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);
        var permissionRepository2 = new PermissionRepository(context2);
        var testPerm = new Permission("Test_Permission", "Test 1");
        var testUpdatedPerm = new Permission("Test_Permission", "Test 2");

        // Act
        await permissionRepository.AddOrUpdateAsync(testPerm);
        await permissionRepository.AddOrUpdateAsync(testUpdatedPerm);
        var expected = await permissionRepository2.GetAsync(testPerm.Id);
        var all = await permissionRepository2.GetAsync();

        // Assert
        Assert.NotNull(expected);
        Assert.NotEqual(string.Empty, expected?.Id);
        Assert.Single(all);
        Assert.Equal(testPerm.Id, expected?.Id);
        Assert.Equal(testUpdatedPerm.Description, expected?.Description);
    }

    [Fact]
    public async Task AddOrUpdateAsync_MultiplePermissions_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);
        var permissionRepository2 = new PermissionRepository(context2);
        var testPerm = new Permission("Test_Permission", "Test 1");
        var testPerm2 = new Permission("Test_Permission2", "Test 2");

        // Act
        await permissionRepository.AddOrUpdateAsync(testPerm);
        await permissionRepository.AddOrUpdateAsync(testPerm2);
        var expected = await permissionRepository2.GetAsync(testPerm.Id);
        var expected2 = await permissionRepository2.GetAsync(testPerm2.Id);
        var all = await permissionRepository2.GetAsync();

        // Assert
        Assert.NotNull(expected);
        Assert.NotNull(expected2);
        Assert.NotEqual(string.Empty, expected?.Id);
        Assert.NotEqual(string.Empty, expected2?.Id);
        Assert.Equal(2, all.Count());
        Assert.Equal(testPerm.Id, expected?.Id);
        Assert.Equal(testPerm2.Id, expected2?.Id);
        Assert.Equal(testPerm.Description, expected?.Description);
        Assert.Equal(testPerm2.Description, expected2?.Description);
    }

    [Fact]
    public async Task AddOrUpdateAsync_MultiplePermissionsUpdated_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var permissionRepository = new PermissionRepository(context);
        var permissionRepository2 = new PermissionRepository(context2);
        var testPerm = new Permission("Test_Permission", "Test 1");
        var testPerm2 = new Permission("Test_Permission2", "Test 2");
        var testPermUpdated = new Permission("Test_Permission", "Test 1 - Updated");
        var testPermUpdated2 = new Permission("Test_Permission2", "Test 2 - Updated");

        // Act
        await permissionRepository.AddOrUpdateAsync(testPerm);
        await permissionRepository.AddOrUpdateAsync(testPerm2);
        await permissionRepository.AddOrUpdateAsync(testPermUpdated);
        await permissionRepository.AddOrUpdateAsync(testPermUpdated2);
        var expected = await permissionRepository2.GetAsync(testPerm.Id);
        var expected2 = await permissionRepository2.GetAsync(testPerm2.Id);
        var all = await permissionRepository2.GetAsync();

        // Assert
        Assert.NotNull(expected);
        Assert.NotNull(expected2);
        Assert.NotEqual(string.Empty, expected?.Id);
        Assert.NotEqual(string.Empty, expected2?.Id);
        Assert.Equal(2, all.Count());
        Assert.Equal(testPerm.Id, expected?.Id);
        Assert.Equal(testPerm2.Id, expected2?.Id);
        Assert.Equal(testPermUpdated.Description, expected?.Description);
        Assert.Equal(testPermUpdated2.Description, expected2?.Description);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var perm1 = await context.Permissions.FindAsync("Test_Permission");
        var perm2 = await context.Permissions.FindAsync("Test_Permission2");
        if (perm1 is not null)
        {
            context.Permissions.Remove(perm1);
        }

        if (perm2 is not null)
        {
            context.Permissions.Remove(perm2);
        }

        await context.SaveChangesAsync();
    }
}
