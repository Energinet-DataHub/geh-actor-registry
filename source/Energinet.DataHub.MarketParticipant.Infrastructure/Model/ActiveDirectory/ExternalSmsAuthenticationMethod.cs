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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Microsoft.Graph;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Model.ActiveDirectory;

public sealed class ExternalSmsAuthenticationMethod : IExternalAuthenticationMethod
{
    private readonly SmsAuthenticationMethod _smsAuthenticationMethod;

    public ExternalSmsAuthenticationMethod(SmsAuthenticationMethod smsAuthenticationMethod)
    {
        _smsAuthenticationMethod = smsAuthenticationMethod;
    }

    public Task AssignAsync(IAuthenticationRequestBuilder authenticationBuilder)
    {
        ArgumentNullException.ThrowIfNull(authenticationBuilder);

        return authenticationBuilder
            .PhoneMethods
            .Request()
            .AddAsync(new PhoneAuthenticationMethod
            {
                PhoneNumber = _smsAuthenticationMethod.PhoneNumber.Number,
                PhoneType = AuthenticationPhoneType.Mobile
            });
    }

    public async Task<bool> VerifyAsync(IAuthenticationRequestBuilder authenticationBuilder)
    {
        ArgumentNullException.ThrowIfNull(authenticationBuilder);

        var collection = await authenticationBuilder
            .PhoneMethods
            .Request()
            .GetAsync()
            .ConfigureAwait(false);

        var phoneMethods = await collection
            .IteratePagesAsync(authenticationBuilder.Client)
            .ConfigureAwait(false);

        return phoneMethods
            .Any(method =>
                method.PhoneType == AuthenticationPhoneType.Mobile &&
                method.PhoneNumber == _smsAuthenticationMethod.PhoneNumber.Number);
    }
}
