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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class OrganizationAuditLogEntryRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;
    private readonly Address _validAddress = new(
        "test Street",
        "1",
        "1111",
        "Test City",
        "Test Country");
    private readonly OrganizationDomain _validDomain = new(new MockedDomain());

    public OrganizationAuditLogEntryRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAsync_NoAuditLogs_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var contextGet = _fixture.DatabaseManager.CreateDbContext();
        var organizationAuditLogEntryRepository = new OrganizationAuditLogEntryRepository(contextGet);

        // Act
        var actual = await organizationAuditLogEntryRepository
            .GetAsync(new OrganizationId(Guid.NewGuid()));

        // Assert
        Assert.Empty(actual);
    }

    [Theory]
    [InlineData(OrganizationChangeType.Name, "New Name")]
    [InlineData(OrganizationChangeType.DomainChange, "NewDomain.dk")]
    public async Task GetAsync_WithAuditLogs_CanBeReadBack(OrganizationChangeType changeType, string newValue)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var user = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(user.Id);

        var testOrg = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress, _validDomain);

        await using var scope = host.BeginScope();
        var organizationRepository = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();

        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var organizationAuditLogEntryRepository = new OrganizationAuditLogEntryRepository(context);

        // Make an audited change.
        var orgId = await organizationRepository.AddOrUpdateAsync(testOrg);
        var organization = await organizationRepository.GetAsync(orgId.Value);
        string orgValue;

        switch (changeType)
        {
            case OrganizationChangeType.Name:
                orgValue = organization!.Name;
                organization.Name = newValue;
                break;
            case OrganizationChangeType.DomainChange:
                orgValue = organization!.Domain.Value;
                organization.Domain = new OrganizationDomain(newValue);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null);
        }

        await organizationRepository.AddOrUpdateAsync(organization);

        // Act
        var actual = await organizationAuditLogEntryRepository
            .GetAsync(orgId.Value);

        // Assert
        var organizationAuditLogs = actual.ToList();
        Assert.Equal(Enum.GetValues<OrganizationChangeType>().Length + 1, organizationAuditLogs.Count); // +1 as it should contain all the original values as well as the changed one.
        Assert.Contains(organizationAuditLogs, o => o.AuditIdentity.Value == user.Id);
        Assert.Contains(organizationAuditLogs, o => o.OrganizationChangeType == changeType);
        Assert.Contains(organizationAuditLogs, o => o.Value == newValue);
        Assert.Contains(organizationAuditLogs, o => o.Value == orgValue);
        Assert.Contains(organizationAuditLogs, o => o.OrganizationId == orgId.Value);
    }
}
