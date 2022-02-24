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
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services
{
    public sealed class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly IConfiguration _configuration;

        public ActiveDirectoryService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<Guid> EnsureAppRegistrationIdAsync(string gln)
        {
            const string param = "GLN";
            const string query = @"SELECT TOP 1 [Id]
                        FROM  [dbo].[ActorInfo]
                        WHERE [dbo].[IdentificationNumber] = @" + param;

            await using var connection = new SqlConnection(_configuration.GetConnectionString("SQL_MP_DB_CONNECTION_STRING"));
            await connection.OpenAsync().ConfigureAwait(false);

            await using var command = new SqlCommand(query, connection)
            {
                Parameters = { new SqlParameter(param, gln) }
            };

            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var record = ((IDataRecord)reader)!;

                return Guid.Parse(record.GetString(0));
            }

            throw new InvalidOperationException("Actor not found");
        }
    }
}
