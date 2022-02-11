﻿// // Copyright 2020 Energinet DataHub A/S
// //
// // Licensed under the Apache License, Version 2.0 (the "License2");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.

using System;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class OrganizationRepositoryTests
    {

        [Fact]
        public async Task SaveAsync_OneOrganization_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            var orgRepository = scope.GetInstance<IOrganizationRepository>();
            var testOrg = new Organization(
                new Uuid(Guid.NewGuid()),
                new GlobalLocationNumber("123"),
                "Test"
            );

            // Act
            await orgRepository.SaveAsync(testOrg);
            var (uuid, globalLocationNumber, name) = await orgRepository.GetFromIdAsync(testOrg.Id);
            Assert.Equal(testOrg.Id.ToString(), uuid.ToString());
            Assert.Equal(testOrg.Gln.Value, globalLocationNumber.Value);
            Assert.Equal(testOrg.Name, name);
        }
    }

}
