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

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Common.Logging;

#pragma warning disable CA1062

/// <summary>
/// Middleware for setting up the root logging scope for ASP.NET Core and Function request logging.
/// IMPORTANT: This middleware must be registered before any other middleware that uses logging.
/// </summary>
public sealed class ScopedLoggingMiddleware : IMiddleware, IFunctionsWorkerMiddleware
{
    private static readonly RootLoggingScope _rootLoggingScope = new();
    private readonly ILogger _logger;

    public ScopedLoggingMiddleware(ILogger<ScopedLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        using (_logger.BeginScope(_rootLoggingScope))
        {
            await next(context).ConfigureAwait(false);
        }
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        using (_logger.BeginScope(_rootLoggingScope))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}

#pragma warning restore CA1062
