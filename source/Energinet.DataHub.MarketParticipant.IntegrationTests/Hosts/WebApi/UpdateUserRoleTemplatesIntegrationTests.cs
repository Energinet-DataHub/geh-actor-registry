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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoleTemplates;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UpdateUserRoleTemplatesIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UpdateUserRoleTemplatesIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateUserRoleTemplates_NewTemplate_ReturnsNewTemplate()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var (actorId, userId) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var templateId = await _fixture
            .DatabaseManager
            .CreateRoleTemplate();

        var updateDto = new UpdateUserRoleTemplatesDto(
            UserRoleTemplateAssignments: new Dictionary<Guid, List<UserRoleTemplateId>>
            {
                { actorId, new List<UserRoleTemplateId>() { templateId } }
            });

        var updateCommand = new UpdateUserRoleTemplatesCommand(userId, updateDto);
        var getCommand = new GetUserRoleTemplatesCommand(actorId, userId);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getCommand);

        // Assert
        Assert.NotEmpty(response.Templates);
        Assert.Equal("fake_value", response.Templates.First().Name);
    }

    [Fact]
    public async Task GetUserRoleTemplates_HasTwoTemplates_ReturnsBoth()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var (actorId, userId) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId, userId, new[] { Permission.UsersManage });

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId, userId, new[] { Permission.OrganizationView });

        var command = new GetUserRoleTemplatesCommand(actorId, userId);

        // Act
        var response = await mediator.Send(command);

        // Assert
        Assert.Equal(2, response.Templates.Count());
    }

    [Fact]
    public async Task GetUserRoleTemplates_HasTwoActors_ReturnsTemplateFromEach()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        var mediator = scope.GetInstance<IMediator>();

        var (actorId1, userId) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        var (actorId2, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId1, userId, new[] { Permission.UsersManage });

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId2, userId, new[] { Permission.OrganizationView });

        await _fixture
            .DatabaseManager
            .AddUserPermissionsAsync(actorId2, userId, new[] { Permission.GridAreasManage });

        var command1 = new GetUserRoleTemplatesCommand(actorId1, userId);
        var command2 = new GetUserRoleTemplatesCommand(actorId2, userId);

        // Act
        var response1 = await mediator.Send(command1);
        var response2 = await mediator.Send(command2);

        // Assert
        Assert.Single(response1.Templates);
        Assert.Equal(2, response2.Templates.Count());
    }
}
