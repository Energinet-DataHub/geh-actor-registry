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

namespace Energinet.DataHub.MarketParticipant.Domain.Model;

public enum ActorAuditedChange
{
    Name = 1,
    Status = 2,
    ContactName = 3,
    ContactEmail = 4,
    ContactPhone = 5,
    ContactCategoryAdded = 6,
    ContactCategoryRemoved = 7,
    CertificateCredentials = 8,
    ClientSecretCredentials = 9,
    DelegationStart = 10,
    DelegationStop = 11,
    ConsolidationRequested = 12,
    ConsolidationCompleted = 13
}
