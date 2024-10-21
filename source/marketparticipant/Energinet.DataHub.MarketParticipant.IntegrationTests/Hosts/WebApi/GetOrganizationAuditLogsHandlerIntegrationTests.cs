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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organizations;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetOrganizationAuditLogsHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public GetOrganizationAuditLogsHandlerIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public Task GetAuditLogs_ChangeName_IsAudited()
    {
        var expected = Guid.NewGuid().ToString();

        return TestAuditOfOrganizationChangeAsync(
            response =>
            {
                var expectedLog = response
                    .AuditLogs
                    .Where(log => log.AuditIdentityId != KnownAuditIdentityProvider.TestFramework.IdentityId.Value)
                    .Single(log => log.Change == OrganizationAuditedChange.Name);

                Assert.Equal(expected, expectedLog.CurrentValue);
            },
            organization =>
            {
                organization.Name = expected;
            });
    }

    [Fact]
    public Task GetAuditLogs_ChangeDomain_IsAudited()
    {
        var expected = new MockedDomain();

        return TestAuditOfOrganizationChangeAsync(
            response =>
            {
                var expectedLog = response
                    .AuditLogs
                    .Where(log => log.AuditIdentityId != KnownAuditIdentityProvider.TestFramework.IdentityId.Value)
                    .Single(log => log.Change == OrganizationAuditedChange.Domain);

                Assert.Equal(expected, expectedLog.CurrentValue);
            },
            organization =>
            {
                organization.Domains = [expected];
            });
    }

    private async Task TestAuditOfOrganizationChangeAsync(
        Action<GetOrganizationAuditLogsResponse> assert,
        params Action<Domain.Model.Organization>[] changeActions)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actorEntity = await _databaseFixture.PrepareActorAsync();

        var userContext = new Mock<IUserContext<FrontendUser>>();

        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        await using var scope = host.BeginScope();

        var organizationRepository = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new GetOrganizationAuditLogsCommand(actorEntity.OrganizationId);
        var auditLogsProcessed = 2; // First 2 logs are created by the test.

        foreach (var action in changeActions)
        {
            var auditedUser = await _databaseFixture.PrepareUserAsync();

            userContext
                .Setup(uc => uc.CurrentUser)
                .Returns(new FrontendUser(auditedUser.Id, actorEntity.OrganizationId, actorEntity.Id, false));

            var organization = await organizationRepository.GetAsync(new OrganizationId(actorEntity.OrganizationId));
            Assert.NotNull(organization);

            action(organization);
            await organizationRepository.AddOrUpdateAsync(organization);

            var auditLogs = await mediator.Send(command);

            foreach (var actorAuditLog in auditLogs.AuditLogs.Skip(auditLogsProcessed))
            {
                Assert.Equal(auditedUser.Id, actorAuditLog.AuditIdentityId);
                Assert.True(actorAuditLog.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
                Assert.True(actorAuditLog.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));

                auditLogsProcessed++;
            }
        }

        // Act
        var actual = await mediator.Send(command);

        // Assert
        assert(actual);
    }
}
