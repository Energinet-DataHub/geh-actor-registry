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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.EntityConfiguration
{
    public sealed class PermissionEntityConfiguration : IEntityTypeConfiguration<PermissionEntity>
    {
        public void Configure(EntityTypeBuilder<PermissionEntity> builder)
        {
            ArgumentNullException.ThrowIfNull(builder, nameof(builder));
            builder.ToTable("Permission");
            builder.HasKey(permission => permission.Id);
            builder.Property(permission => permission.Id).ValueGeneratedNever();
            builder.OwnsMany(permission => permission.EicFunctions, ConfigureEicFunctions);
        }

        private static void ConfigureEicFunctions(
            OwnedNavigationBuilder<PermissionEntity, PermissionEicFunctionEntity> eicBuilder)
        {
            eicBuilder.WithOwner().HasForeignKey("PermissionId");
            eicBuilder.ToTable("PermissionEicFunction");
            eicBuilder.Property<Guid>("Id").ValueGeneratedOnAdd();
            eicBuilder.Property(p => p.EicFunction).HasColumnName("EicFunction");
            eicBuilder.Property(p => p.PermissionId).HasColumnName("PermissionId");
        }
    }
}
