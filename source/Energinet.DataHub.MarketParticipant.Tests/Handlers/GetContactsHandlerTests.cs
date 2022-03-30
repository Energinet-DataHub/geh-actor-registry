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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class GetContactsHandlerTests
    {
        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // Arrange
            var target = new GetContactsHandler(
                new Mock<IOrganizationRepository>().Object,
                new Mock<IContactRepository>().Object);

            // Act + Assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NoOrganization_ThrowsNotFoundException()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new GetContactsHandler(
                organizationRepository.Object,
                new Mock<IContactRepository>().Object);

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync((Organization?)null);

            var command = new GetContactsCommand(Guid.NewGuid());

            // Act + Assert
            await Assert
                .ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_HasContacts_ReturnsContacts()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var contactRepository = new Mock<IContactRepository>();
            var target = new GetContactsHandler(
                organizationRepository.Object,
                contactRepository.Object);

            var organizationId = new OrganizationId(Guid.NewGuid());

            var organization = new Organization(
                organizationId,
                "fake_value",
                Enumerable.Empty<Actor>());

            organizationRepository
                .Setup(x => x.GetAsync(organizationId))
                .ReturnsAsync(organization);

            var expected = new Contact(
                new ContactId(Guid.NewGuid()),
                organizationId,
                "fake_value",
                ContactCategory.EndOfSupply,
                new EmailAddress("fake@value"),
                new PhoneNumber("1234"));

            contactRepository
                .Setup(x => x.GetAsync(organizationId))
                .ReturnsAsync(new[] { expected });

            var command = new GetContactsCommand(organizationId.Value);

            // Act
            var response = await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotEmpty(response.Contacts);

            var actualContact = response.Contacts.Single();
            Assert.Equal(expected.Id.Value, actualContact.ContactId);
            Assert.Equal(expected.Name, actualContact.Name);
            Assert.Equal(expected.Category.Name, actualContact.Category);
            Assert.Equal(expected.Email.Address, actualContact.Email);
            Assert.Equal(expected.Phone?.Number, actualContact.Phone);
        }
    }
}
