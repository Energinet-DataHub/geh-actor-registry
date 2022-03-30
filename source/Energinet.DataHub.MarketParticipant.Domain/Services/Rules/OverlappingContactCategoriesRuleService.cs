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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Utilities;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules
{
    public sealed class OverlappingContactCategoriesRuleService : IOverlappingContactCategoriesRuleService
    {
        public void ValidateCategoriesAcrossContacts(IEnumerable<Contact> contacts)
        {
            Guard.ThrowIfNull(contacts, nameof(contacts));

            var categories = new HashSet<ContactCategory>();

            foreach (var contact in contacts)
            {
                if (!categories.Add(contact.Category))
                {
                    throw new ValidationException($"Cannot add '{contact.Category}', as a contact with this category is already present.");
                }
            }
        }
    }
}
