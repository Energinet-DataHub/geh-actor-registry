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

using System.ComponentModel.DataAnnotations;

namespace Energinet.DataHub.MarketParticipant.Domain.Model;

public sealed record PhoneNumber
{
    private static readonly PhoneAttribute _validator = new();

    public PhoneNumber(string number)
    {
        Number = ValidateNumber(number);
    }

    public string Number { get; }

    public override string ToString()
    {
        return Number;
    }

    private static string ValidateNumber(string number)
    {
        return !string.IsNullOrWhiteSpace(number) && number.Length <= 30 && _validator.IsValid(number)
            ? number
            : throw new ValidationException($"The provided phone number '{number}' is not valid.");
    }
}
