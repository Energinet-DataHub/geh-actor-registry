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
using System.Net.Mail;

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    public sealed record EmailAddress : IComparable<EmailAddress>
    {
        private readonly MailAddress _mailAddress;

        public EmailAddress(string address)
        {
            _mailAddress = ValidateAddress(address);
        }

        public string Address => _mailAddress.Address;

        public static bool operator <(EmailAddress left, EmailAddress right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(EmailAddress left, EmailAddress right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(EmailAddress left, EmailAddress right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(EmailAddress left, EmailAddress right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }

        private static MailAddress ValidateAddress(string address)
        {
            return !string.IsNullOrWhiteSpace(address) && MailAddress.TryCreate(address, out var parsedAddress)
                ? parsedAddress
                : throw new ValidationException($"The provided e-mail '{address}' is not valid.");
        }

        public int CompareTo(EmailAddress? other)
        {
            ArgumentNullException.ThrowIfNull(other);
            return string.Compare(other?._mailAddress.Address, _mailAddress.Address, StringComparison.Ordinal);
        }
    }
}
