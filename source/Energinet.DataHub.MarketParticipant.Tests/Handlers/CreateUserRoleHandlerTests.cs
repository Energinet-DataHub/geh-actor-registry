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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actor;
using Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class CreateUserRoleHandlerTests
    {
        private const string ValidPermission = nameof(Core.App.Common.Security.Permission.ActorManage);

        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // Arrange
            var target = new CreateUserRoleHandler(new Mock<IUserRoleRepository>().Object);

            // Act + Assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NewUserRole_UserRoleIdReturned()
        {
            // Arrange
            var repositoryMock = new Mock<IUserRoleRepository>();
            var target = new CreateUserRoleHandler(repositoryMock.Object);
            var roleId = Guid.NewGuid();
            var userRole = new UserRole(
                new UserRoleId(roleId),
                "fake_value",
                "fake_value",
                UserRoleStatus.Active,
                new List<Permission>(),
                EicFunction.Consumer);

            repositoryMock
                .Setup(x => x.AddAsync(It.IsAny<UserRole>()))
                .ReturnsAsync(userRole.Id);

            var command = new CreateUserRoleCommand(new CreateUserRoleDto(
                "fake_value",
                "fake_value",
                nameof(UserRoleStatus.Active),
                nameof(EicFunction.Consumer),
                new Collection<string> { ValidPermission }));

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(userRole.Id.Value, response.UserRoleId);
        }
    }
}
