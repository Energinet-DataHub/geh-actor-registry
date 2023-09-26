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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class UserRoleAuditLogIntegrationTest : WebApiIntegrationTestsBase
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserRoleAuditLogIntegrationTest(MarketParticipantDatabaseFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    private static UserRoleEntity ValidUserRole => new()
    {
        Name = "Integration Test User Role - Audit Log",
        Description = "Integration Test User Role Description",
        Status = UserRoleStatus.Active,
        ChangedByIdentityId = KnownAuditIdentityProvider.TestFramework.IdentityId.Value,
        EicFunctions =
        {
            new UserRoleEicFunctionEntity { EicFunction = EicFunction.DataHubAdministrator }
        },
        Permissions =
        {
            new UserRolePermissionEntity
            {
                Permission = PermissionId.ActorsManage,
                ChangedByIdentityId = KnownAuditIdentityProvider.TestFramework.IdentityId.Value,
            }
        }
    };

    [Fact]
    public async Task Create_UserRole_AuditLogSaved()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleAuditLogEntryRepository = new UserRoleAuditLogEntryRepository(context);

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var user = await _fixture.PrepareUserAsync().ConfigureAwait(false);

        const string name = "Create_UserRole_AuditLogSaved";
        var createUserRoleDto = new CreateUserRoleDto(
            name,
            "description",
            UserRoleStatus.Active,
            EicFunction.DataHubAdministrator,
            new Collection<int> { (int)PermissionId.OrganizationsView, (int)PermissionId.UsersManage });

        var createUserRoleCommand = new CreateUserRoleCommand(user.Id, createUserRoleDto);

        // Act
        var response = await mediator.Send(createUserRoleCommand).ConfigureAwait(false);
        var result = await userRoleAuditLogEntryRepository.GetAsync(new UserRoleId(response.UserRoleId)).ConfigureAwait(false);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList.Where(e => e.UserRoleId.Value == response.UserRoleId));
        Assert.Contains(resultList, e => e.ChangeType == UserRoleChangeType.Created);
        resultList.First().Name.Should().Be(createUserRoleDto.Name);
        resultList.First().Description.Should().Be(createUserRoleDto.Description);
        resultList.First().EicFunction.Should().Be(createUserRoleDto.EicFunction);
        resultList.First().Status.Should().Be(createUserRoleDto.Status);
        resultList.First().Permissions.Should().HaveCount(2);
        resultList.First().Permissions.Select(e => (int)e).OrderBy(e => e).Should().BeEquivalentTo(createUserRoleDto.Permissions.OrderBy(e => e));
    }

    [Fact]
    public async Task Update_UserRole_AllChanges()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleAuditLogEntryRepository = new UserRoleAuditLogEntryRepository(context);

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync();

        const string nameUpdate = "Update_UserRole_AllChanges_NameChangedAuditLog";
        const string descriptionUpdate = "Update_UserRole_AllChanges_DescriptionChangedAuditLog";
        const UserRoleStatus userRoleStatusUpdate = UserRoleStatus.Inactive;
        var userRolePermissionsUpdate = new Collection<int> { (int)PermissionId.UsersView, (int)PermissionId.UsersManage };

        var updateUserRoleDto = new UpdateUserRoleDto(
            nameUpdate,
            descriptionUpdate,
            userRoleStatusUpdate,
            userRolePermissionsUpdate);

        var updateUserRoleCommand = new UpdateUserRoleCommand(user.Id, userRole.Id, updateUserRoleDto);

        // Act
        await mediator.Send(updateUserRoleCommand);
        var auditLogs = await userRoleAuditLogEntryRepository.GetAsync(new UserRoleId(userRole.Id));

        // Assert
        var resultList = auditLogs.ToList();
        Assert.Single(resultList, e => e.ChangeType == UserRoleChangeType.Created);
        Assert.Single(resultList, e => e.ChangeType == UserRoleChangeType.NameChange);
        Assert.Single(resultList, e => e.ChangeType == UserRoleChangeType.DescriptionChange);
        Assert.Single(resultList, e => e.ChangeType == UserRoleChangeType.StatusChange);
        Assert.Single(resultList, e => e.ChangeType == UserRoleChangeType.PermissionsChange);
        resultList.Single(e => e.ChangeType == UserRoleChangeType.NameChange).Name.Should().Be(nameUpdate);
        resultList.Single(e => e.ChangeType == UserRoleChangeType.DescriptionChange).Description.Should().Be(descriptionUpdate);
        resultList.Single(e => e.ChangeType == UserRoleChangeType.StatusChange).Status.Should().Be(userRoleStatusUpdate);
        resultList.Single(e => e.ChangeType == UserRoleChangeType.PermissionsChange).Permissions.Should().HaveCount(2);
        resultList.Single(e => e.ChangeType == UserRoleChangeType.PermissionsChange)
            .Permissions
            .Select(e => (int)e).OrderBy(e => e).Should()
            .BeEquivalentTo(userRolePermissionsUpdate.OrderBy(e => e));
    }

    [Fact]
    public async Task Update_UserRole_PermissionChanges_FiveTimes()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleAuditLogEntryRepository = new UserRoleAuditLogEntryRepository(context);
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var user = await _fixture.PrepareUserAsync().ConfigureAwait(false);
        var userRole = await _fixture.PrepareUserRoleAsync(ValidUserRole).ConfigureAwait(false);

        var userRolePermissionsUpdates = new List<Collection<int>>
        {
           new() { (int)PermissionId.UsersView, (int)PermissionId.UsersManage },
           new() { (int)PermissionId.PermissionsManage },
           new(),
           new() { (int)PermissionId.UsersView, (int)PermissionId.UsersManage, (int)PermissionId.SettlementReportsManage, (int)PermissionId.PermissionsManage, (int)PermissionId.GridAreasManage, (int)PermissionId.ActorsManage },
           new() { (int)PermissionId.OrganizationsView, (int)PermissionId.OrganizationsManage }
        };

        var updateUserRoleDto = new UpdateUserRoleDto(userRole.Name, userRole.Description ?? string.Empty, userRole.Status, new Collection<int>());

        // Act
        foreach (var permissions in userRolePermissionsUpdates)
        {
            var updateUserRoleCommand = new UpdateUserRoleCommand(user.Id, userRole.Id, updateUserRoleDto with { Permissions = permissions });
            await mediator.Send(updateUserRoleCommand);
        }

        var auditLogs = await userRoleAuditLogEntryRepository.GetAsync(new UserRoleId(userRole.Id));

        // Assert
        var resultList = auditLogs.ToList();
        resultList.Should().HaveCount(6);
        resultList.Where(e => e.ChangeType == UserRoleChangeType.PermissionsChange).Should().HaveCount(5);
        resultList.Where(e => e.ChangeType == UserRoleChangeType.Created).Should().HaveCount(1);
        var permissionChanges = resultList.Where(e => e.ChangeType == UserRoleChangeType.PermissionsChange).ToList();
        for (var i = 0; i < userRolePermissionsUpdates.Count; i++)
        {
            permissionChanges[i].Permissions
                .Select(e => (int)e).OrderBy(e => e).Should()
                .BeEquivalentTo(userRolePermissionsUpdates[i].OrderBy(e => e));
        }
    }
}
