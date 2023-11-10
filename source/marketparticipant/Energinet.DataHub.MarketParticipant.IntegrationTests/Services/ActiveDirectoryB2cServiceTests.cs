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
using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services
{
    [Collection(nameof(IntegrationTestCollectionFixture))]
    [IntegrationTest]
    public sealed class ActiveDirectoryB2CServiceTests
    {
        private readonly IActiveDirectoryB2CService _sut;
        private readonly GraphServiceClientFixture _graphServiceClientFixture;

        public ActiveDirectoryB2CServiceTests(GraphServiceClientFixture graphServiceClientFixture, B2CFixture b2CFixture)
        {
            _graphServiceClientFixture = graphServiceClientFixture;
#pragma warning disable CA1062
            _sut = b2CFixture.B2CService;
#pragma warning restore CA1062
        }

        [Fact]
        public async Task CreateConsumerAppRegistrationAsync_AppIsRegistered_Success()
        {
            ExternalActorId? cleanupId = null;

            try
            {
                // Arrange
                var roles = new List<EicFunction>
                {
                    EicFunction.SystemOperator // transmission system operator
                };

                // Act
                var response = await _sut
                    .CreateAppRegistrationAsync(new MockedGln(), roles);

                cleanupId = response.ExternalActorId;

                // Assert
                var app = await _sut.GetExistingAppRegistrationAsync(
                        new AppRegistrationObjectId(Guid.Parse(response.AppObjectId)),
                        new AppRegistrationServicePrincipalObjectId(response.ServicePrincipalObjectId));

                Assert.Equal(response.ExternalActorId.Value.ToString(), app.AppId);
            }
            finally
            {
                await CleanupAsync(cleanupId);
            }
        }

        [Fact]
        public async Task GetExistingAppRegistrationAsync_AddTwoRolesToAppRegistration_Success()
        {
            ExternalActorId? cleanupId = null;

            try
            {
                // Arrange
                var roles = new List<EicFunction>
                {
                    EicFunction.SystemOperator, // transmission system operator
                    EicFunction.MeteredDataResponsible
                };

                var createAppRegistrationResponse = await _sut
                    .CreateAppRegistrationAsync(new MockedGln(), roles);

                cleanupId = createAppRegistrationResponse.ExternalActorId;

                // Act
                var app = await _sut.GetExistingAppRegistrationAsync(
                        new AppRegistrationObjectId(Guid.Parse(createAppRegistrationResponse.AppObjectId)),
                        new AppRegistrationServicePrincipalObjectId(createAppRegistrationResponse.ServicePrincipalObjectId));

                // Assert
                Assert.Equal("d82c211d-cce0-e95e-bd80-c2aedf99f32b", app.AppRoles.First().RoleId);
                Assert.Equal("00e32df2-b846-2e18-328f-702cec8f1260", app.AppRoles.ElementAt(1).RoleId);
            }
            finally
            {
                await CleanupAsync(cleanupId);
            }
        }

        [Fact]
        public async Task DeleteConsumerAppRegistrationAsync_DeleteCreatedAppRegistration_ServiceException404IsThrownWhenTryingToGetTheDeletedApp()
        {
            ExternalActorId? cleanupId = null;

            try
            {
                // Arrange
                var roles = new List<EicFunction>
                {
                    EicFunction.SystemOperator // transmission system operator
                };

                var createAppRegistrationResponse = await _sut.CreateAppRegistrationAsync(
                        new MockedGln(),
                        roles);

                cleanupId = createAppRegistrationResponse.ExternalActorId;

                // Act
                await _sut
                    .DeleteAppRegistrationAsync(createAppRegistrationResponse.ExternalActorId);

                cleanupId = null;

                // Assert
                var ex = await Assert.ThrowsAsync<ODataError>(async () => await _sut
                        .GetExistingAppRegistrationAsync(
                            new AppRegistrationObjectId(Guid.Parse(createAppRegistrationResponse.AppObjectId)),
                            new AppRegistrationServicePrincipalObjectId(createAppRegistrationResponse.ServicePrincipalObjectId)));

                Assert.Equal((int)HttpStatusCode.NotFound, ex.ResponseStatusCode);
            }
            finally
            {
                await CleanupAsync(cleanupId);
            }
        }

        [Fact]
        public async Task AddSecretToAppRegistration_ReturnsPassword_AndAppHasPassword()
        {
            ExternalActorId? cleanupId = null;

            try
            {
                // Arrange
                var roles = new List<EicFunction>
                {
                    EicFunction.SystemOperator // transmission system operator
                };

                var createAppRegistrationResponse = await _sut.CreateAppRegistrationAsync(
                    new MockedGln(),
                    roles);

                cleanupId = createAppRegistrationResponse.ExternalActorId;

                // Act
                var result = await _sut
                    .CreateSecretForAppRegistrationAsync(createAppRegistrationResponse.ExternalActorId);
                var existing = await GetExistingAppAsync(createAppRegistrationResponse.ExternalActorId);

                // Assert
                Assert.NotEmpty(result.SecretText);
                Assert.NotEmpty(result.SecretId.ToString());
                Assert.NotNull(existing);
                Assert.True(existing.PasswordCredentials is { Count: >0 });
            }
            finally
            {
                await CleanupAsync(cleanupId);
            }
        }

        [Fact]
        public async Task RemoveSecretFromAppRegistration_DoesNotThrow_And_PasswordIsRemoved()
        {
            ExternalActorId? cleanupId = null;

            try
            {
                // Arrange
                var roles = new List<EicFunction>
                {
                    EicFunction.SystemOperator // transmission system operator
                };

                var createAppRegistrationResponse = await _sut.CreateAppRegistrationAsync(
                    new MockedGln(),
                    roles);

                cleanupId = createAppRegistrationResponse.ExternalActorId;

                await _sut
                    .CreateSecretForAppRegistrationAsync(createAppRegistrationResponse.ExternalActorId);

                // Act
                var exceptions = await Record.ExceptionAsync(() => _sut.RemoveSecretsForAppRegistrationAsync(createAppRegistrationResponse.ExternalActorId));
                var existing = await GetExistingAppAsync(createAppRegistrationResponse.ExternalActorId);

                // Assert
                Assert.Null(exceptions);
                Assert.NotNull(existing);
                Assert.True(existing.PasswordCredentials is { Count: 0 });
            }
            finally
            {
                await CleanupAsync(cleanupId);
            }
        }

        private async Task CleanupAsync(ExternalActorId? externalActorId)
        {
            if (externalActorId == null)
                return;

            await _sut
                .DeleteAppRegistrationAsync(externalActorId)
                .ConfigureAwait(false);
        }

        private async Task<Microsoft.Graph.Models.Application?> GetExistingAppAsync(ExternalActorId externalActorId)
        {
            var appId = externalActorId.Value.ToString();
            var applicationUsingAppId = await _graphServiceClientFixture.Client
                .Applications
                .GetAsync(x => { x.QueryParameters.Filter = $"appId eq '{appId}'"; })
                .ConfigureAwait(false);

            var applications = await applicationUsingAppId!
                .IteratePagesAsync<Microsoft.Graph.Models.Application, ApplicationCollectionResponse>(_graphServiceClientFixture.Client)
                .ConfigureAwait(false);

            return applications.SingleOrDefault();
        }
    }
}
