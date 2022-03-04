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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services
{
    [UnitTest]
    public sealed class GlobalLocationNumberUniquenessServiceTests
    {
        [Fact]
        public async Task EnsureGlobalLocationNumberAvailableAsync_GlnAvailable_DoesNothing()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new GlobalLocationNumberUniquenessService(organizationRepository.Object);

            var gln = new GlobalLocationNumber("fake_value");

            organizationRepository
                .Setup(x => x.GetAsync(gln))
                .ReturnsAsync(Enumerable.Empty<Organization>());

            // Act + Assert
            await target
                .EnsureGlobalLocationNumberAvailableAsync(gln)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task EnsureGlobalLocationNumberAvailableAsync_GlnNotAvailable_ThrowsException()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new GlobalLocationNumberUniquenessService(organizationRepository.Object);

            var gln = new GlobalLocationNumber("fake_value");

            organizationRepository
                .Setup(x => x.GetAsync(gln))
                .ReturnsAsync(new[] { new Organization(Guid.NewGuid(), gln, "fake_value") });

            // Act + Assert
            await Assert
                .ThrowsAsync<ValidationException>(() => target.EnsureGlobalLocationNumberAvailableAsync(gln))
                .ConfigureAwait(false);
        }
    }
}
