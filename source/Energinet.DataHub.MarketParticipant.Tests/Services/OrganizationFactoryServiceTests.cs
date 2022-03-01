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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services
{
    [UnitTest]
    public sealed class OrganizationFactoryServiceTests
    {
        [Fact]
        public async Task CreateAsync_NewOrganization_ReturnsOrganizationId()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new OrganizationFactoryService(
                organizationRepository.Object,
                new Mock<IOrganizationEventDispatcher>().Object,
                new Mock<IGlobalLocationNumberUniquenessService>().Object,
                new Mock<IActiveDirectoryService>().Object);

            var expectedId = Guid.NewGuid();

            organizationRepository
                .Setup(x => x.AddOrUpdateAsync(It.IsAny<Organization>()))
                .ReturnsAsync(new OrganizationId(expectedId));

            // Act
            var response = await target
                .CreateAsync(new GlobalLocationNumber("fake_value"), "fake_value")
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(expectedId, response.Id.Value);
        }

        [Fact]
        public async Task CreateAsync_NewOrganization_ValuesAreMapped()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new OrganizationFactoryService(
                organizationRepository.Object,
                new Mock<IOrganizationEventDispatcher>().Object,
                new Mock<IGlobalLocationNumberUniquenessService>().Object,
                new Mock<IActiveDirectoryService>().Object);

            var expectedId = Guid.NewGuid();

            const string orgName = "SomeName";
            const string orgGln = "SomeGln";

            organizationRepository
                .Setup(x => x.AddOrUpdateAsync(It.Is<Organization>(
                    o => o.Name == orgName && o.Gln.Value == orgGln)))
                .ReturnsAsync(new OrganizationId(expectedId));

            // Act
            var response = await target
                .CreateAsync(new GlobalLocationNumber(orgGln), orgName)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(expectedId, response.Id.Value);
        }

        [Fact]
        public async Task CreateAsync_NewOrganization_DispatchesEvent()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var organizationEventDispatcher = new Mock<IOrganizationEventDispatcher>();
            var target = new OrganizationFactoryService(
                organizationRepository.Object,
                organizationEventDispatcher.Object,
                new Mock<IGlobalLocationNumberUniquenessService>().Object,
                new Mock<IActiveDirectoryService>().Object);

            var expectedId = Guid.NewGuid();

            const string orgName = "SomeName";
            const string orgGln = "SomeGln";

            organizationRepository
                .Setup(x => x.AddOrUpdateAsync(It.Is<Organization>(
                    o => o.Name == orgName && o.Gln.Value == orgGln)))
                .ReturnsAsync(new OrganizationId(expectedId));

            // Act
            await target
                .CreateAsync(new GlobalLocationNumber(orgGln), orgName)
                .ConfigureAwait(false);

            // Assert
            organizationEventDispatcher.Verify(
                x => x.DispatchChangedEventAsync(It.Is<Organization>(
                    o => o.Id.Value == expectedId)),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NewOrganization_EnsuresGlnUniqueness()
        {
            // Arrange
            var globalLocationNumberUniquenessService = new Mock<IGlobalLocationNumberUniquenessService>();
            var target = new OrganizationFactoryService(
                new Mock<IOrganizationRepository>().Object,
                new Mock<IOrganizationEventDispatcher>().Object,
                globalLocationNumberUniquenessService.Object,
                new Mock<IActiveDirectoryService>().Object);

            const string orgName = "SomeName";
            const string orgGln = "SomeGln";
            var globalLocationNumber = new GlobalLocationNumber(orgGln);

            // Act
            await target
                .CreateAsync(globalLocationNumber, orgName)
                .ConfigureAwait(false);

            // Assert
            globalLocationNumberUniquenessService.Verify(
                x => x.EnsureGlobalLocationNumberAvailableAsync(globalLocationNumber),
                Times.Once);
        }
    }
}
