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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using AppPermission = Energinet.DataHub.Core.App.Common.Security.Permission;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services.ActiveDirectory
{
    public sealed class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly AzureAdConfig _azureAdConfig;
        private readonly IBusinessRoleCodeDomainService _businessRoleCodeDomainService;
        private readonly IActiveDirectoryB2CRolesProvider _activeDirectoryB2CRolesProvider;
        private readonly ILogger<ActiveDirectoryService> _logger;

        public ActiveDirectoryService(
            GraphServiceClient graphClient,
            AzureAdConfig config,
            IBusinessRoleCodeDomainService businessRoleCodeDomainService,
            IActiveDirectoryB2CRolesProvider activeDirectoryB2CRolesProvider,
            ILogger<ActiveDirectoryService> logger)
        {
            _graphClient = graphClient;
            _azureAdConfig = config;
            _businessRoleCodeDomainService = businessRoleCodeDomainService;
            _activeDirectoryB2CRolesProvider = activeDirectoryB2CRolesProvider;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> ListAppRegistrationsAsync()
        {
            var appsCollection = await _graphClient.Applications.Request().GetAsync().ConfigureAwait(false);
            var result = new List<string>();
            var pageIterator = PageIterator<Application>.CreatePageIterator(
                _graphClient,
                appsCollection,
                application =>
                {
                    result.Add(application.AppId);
                    return true;
                });
            await pageIterator.IterateAsync().ConfigureAwait(false);
            return result;
        }

        public async Task<IEnumerable<AppPermission>> GetPermissionsToAppRegistrationAsync(Guid appRegistrationId)
        {
            var app = await _graphClient.Applications[appRegistrationId.ToString()]
                .Request()
                .GetAsync()
                .ConfigureAwait(false);
            var result = new List<AppPermission>();
            if (app is null) return Enumerable.Empty<AppPermission>();
            foreach (var appRole in app.AppRoles)
            {
                if (Enum.TryParse<AppPermission>(appRole.Value, out var permission))
                {
                    result.Add(permission);
                }
            }

            return result;
        }

        public async Task UpdatePermissionsToAppAsync(Guid appRegistrationId, IEnumerable<string> appRoles)
        {
            var app = await _graphClient.Applications[appRegistrationId.ToString()]
                .Request()
                .GetAsync()
                .ConfigureAwait(false);
            var updated = new Application
            {
                AppRoles = appRoles.Select(x =>
                    new AppRole() { Id = Guid.NewGuid(), Value = x, DisplayName = x, Description = x })
            };
            await _graphClient.Applications[appRegistrationId.ToString()]
                .Request()
                .UpdateAsync(updated)
                .ConfigureAwait(false);
        }

        public async Task<string> CreateAppRegistrationAsync(BusinessRegisterIdentifier identifier)
        {
            ArgumentNullException.ThrowIfNull(identifier);

            var app = await _graphClient.Applications
                .Request()
                .AddAsync(new Application
                {
                    DisplayName = identifier.Identifier,
                    Api = new ApiApplication
                    {
                        RequestedAccessTokenVersion = 2,
                    },
                    CreatedDateTime = DateTimeOffset.Now,
                    SignInAudience = "AzureADMultipleOrgs",
                }).ConfigureAwait(false);
            var updated = new Application();
            if (app.IdentifierUris.ToList().Count == 0)
            {
                updated.IdentifierUris = new string[] { $"https://WE_NEED_TENANT_DOMAIN_HERE/{app.AppId}" };
            }

            var appscope = app.Api.Oauth2PermissionScopes.ToList();
            var newScope = new PermissionScope
            {
                Id = Guid.NewGuid(),
                AdminConsentDescription = "default scope",
                AdminConsentDisplayName = "default scope",
                IsEnabled = true,
                Type = "Admin",
                Value = "actor.default"
            };
            appscope.Add(newScope);
            updated.Api = new ApiApplication { Oauth2PermissionScopes = appscope };
            await _graphClient.Applications[app.Id]
                .Request()
                .UpdateAsync(updated)
                .ConfigureAwait(false);

            return app.AppId;
        }

        public async Task DeleteAppRegistrationAsync(Guid appRegistrationId)
        {
            await _graphClient.Applications[appRegistrationId.ToString()]
                .Request()
                .DeleteAsync()
                .ConfigureAwait(false);
        }

        //     public async Task<CreateAppRegistrationResponse> CreateAppRegistrationAsync(
        //         ActorNumber actorNumber,
        //         IReadOnlyCollection<EicFunction> permissions)
        //     {
        //         ArgumentNullException.ThrowIfNull(actorNumber, nameof(actorNumber));
        //         ArgumentNullException.ThrowIfNull(permissions, nameof(permissions));
        //
        //         var roles = _businessRoleCodeDomainService.GetBusinessRoleCodes(permissions);
        //         var b2CPermissions = await MapBusinessRoleCodesToB2CRoleIdsAsync(roles).ConfigureAwait(false);
        //         var permissionsToPass = b2CPermissions.Select(x => x.ToString()).ToList();
        //         try
        //         {
        //             var app = await CreateAppInB2CAsync(actorNumber.Value, permissionsToPass).ConfigureAwait(false);
        //
        //             var servicePrincipal = await AddServicePrincipalToAppInB2CAsync(app.AppId).ConfigureAwait(false);
        //
        //             foreach (var permission in b2CPermissions)
        //             {
        //                 await GrantAddedRoleToServicePrincipalAsync(
        //                     servicePrincipal.Id,
        //                     permission)
        //                     .ConfigureAwait(false);
        //             }
        //
        //             return new CreateAppRegistrationResponse(
        //                 new ExternalActorId(app.AppId),
        //                 app.Id,
        //                 servicePrincipal.Id);
        //         }
        //         catch (Exception e)
        //         {
        //             _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2cService)}");
        //             throw;
        //         }
        //     }
        //
        //     public async Task<AppRegistrationSecret> CreateSecretForAppRegistrationAsync(AppRegistrationObjectId appRegistrationObjectId)
        //     {
        //         ArgumentNullException.ThrowIfNull(appRegistrationObjectId, nameof(appRegistrationObjectId));
        //         var passwordCredential = new PasswordCredential
        //         {
        //             DisplayName = "App secret",
        //             StartDateTime = DateTimeOffset.Now,
        //             EndDateTime = DateTimeOffset.Now.AddMonths(6),
        //             KeyId = Guid.NewGuid(),
        //             CustomKeyIdentifier = null,
        //         };
        //
        //         var secret = await _graphClient.Applications[appRegistrationObjectId.Value.ToString()]
        //             .AddPassword(passwordCredential)
        //             .Request()
        //             .PostAsync().ConfigureAwait(false);
        //
        //         return new AppRegistrationSecret(secret.SecretText);
        //     }
        //
        //     public async Task DeleteAppRegistrationAsync(ExternalActorId externalActorId)
        //     {
        //         ArgumentNullException.ThrowIfNull(externalActorId);
        //
        //         try
        //         {
        //             var appId = externalActorId.Value.ToString();
        //             var applicationUsingAppId = await _graphClient
        //                 .Applications
        //                 .Request()
        //                 .Filter($"appId eq '{appId}'")
        //                 .GetAsync()
        //                 .ConfigureAwait(false);
        //
        //             var foundApp = applicationUsingAppId.SingleOrDefault();
        //             if (foundApp == null)
        //             {
        //                 throw new InvalidOperationException("Cannot delete registration from B2C; Application was not found.");
        //             }
        //
        //             await _graphClient.Applications[foundApp.Id]
        //                 .Request()
        //                 .DeleteAsync()
        //                 .ConfigureAwait(false);
        //         }
        //         catch (Exception e)
        //         {
        //             _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2cService)}");
        //             throw;
        //         }
        //     }
        //
        //     public async Task<ActiveDirectoryAppInformation> GetExistingAppRegistrationAsync(
        //         AppRegistrationObjectId appRegistrationObjectId,
        //         AppRegistrationServicePrincipalObjectId appRegistrationServicePrincipalObjectId)
        //     {
        //         ArgumentNullException.ThrowIfNull(appRegistrationObjectId, nameof(appRegistrationObjectId));
        //         ArgumentNullException.ThrowIfNull(appRegistrationServicePrincipalObjectId, nameof(appRegistrationServicePrincipalObjectId));
        //
        //         try
        //         {
        //             var retrievedApp = await _graphClient.Applications[appRegistrationObjectId.Value.ToString()]
        //                 .Request()
        //                 .Select(a => new { a.AppId, a.Id, a.DisplayName, a.AppRoles })
        //                 .GetAsync().ConfigureAwait(false);
        //
        //             var appRoles = await GetRolesAsync(appRegistrationServicePrincipalObjectId.Value).ConfigureAwait(false);
        //
        //             return new ActiveDirectoryAppInformation(
        //                 retrievedApp.AppId,
        //                 retrievedApp.Id,
        //                 retrievedApp.DisplayName,
        //                 appRoles);
        //         }
        //         catch (Exception e)
        //         {
        //             _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2cService)}");
        //             throw;
        //         }
        //     }
        //
        //     private async Task<IEnumerable<Guid>> MapBusinessRoleCodesToB2CRoleIdsAsync(IEnumerable<BusinessRoleCode> businessRoleCodes)
        //     {
        //         var roles = await _activeDirectoryB2CRolesProvider.GetB2CRolesAsync().ConfigureAwait(false);
        //         var b2CIds = new List<Guid>();
        //         foreach (var roleCode in businessRoleCodes)
        //         {
        //             switch (roleCode)
        //             {
        //                 case BusinessRoleCode.Ddk:
        //                     b2CIds.Add(roles.DdkId);
        //                     break;
        //                 case BusinessRoleCode.Ddm:
        //                     b2CIds.Add(roles.DdmId);
        //                     break;
        //                 case BusinessRoleCode.Ddq:
        //                     b2CIds.Add(roles.DdqId);
        //                     break;
        //                 case BusinessRoleCode.Ddx:
        //                     b2CIds.Add(roles.DdxId);
        //                     break;
        //                 case BusinessRoleCode.Ddz:
        //                     b2CIds.Add(roles.DdzId);
        //                     break;
        //                 case BusinessRoleCode.Dgl:
        //                     b2CIds.Add(roles.DglId);
        //                     break;
        //                 case BusinessRoleCode.Ez:
        //                     b2CIds.Add(roles.EzId);
        //                     break;
        //                 case BusinessRoleCode.Mdr:
        //                     b2CIds.Add(roles.MdrId);
        //                     break;
        //                 case BusinessRoleCode.Sts:
        //                     b2CIds.Add(roles.StsId);
        //                     break;
        //                 case BusinessRoleCode.Tso:
        //                     b2CIds.Add(roles.TsoId);
        //                     break;
        //                 default:
        //                     throw new ArgumentNullException(nameof(businessRoleCodes));
        //             }
        //         }
        //
        //         return b2CIds;
        //     }
        //
        //     private async Task<IEnumerable<ActiveDirectoryRole>> GetRolesAsync(string servicePrincipalObjectId)
        //     {
        //         try
        //         {
        //             var roles = await _graphClient.ServicePrincipals[servicePrincipalObjectId]
        //                 .AppRoleAssignments
        //                 .Request()
        //                 .GetAsync()
        //                 .ConfigureAwait(false);
        //
        //             if (roles is null)
        //             {
        //                 throw new InvalidOperationException($"'{nameof(roles)}' is null");
        //             }
        //
        //             var roleIds = new List<ActiveDirectoryRole>();
        //             foreach (var role in roles)
        //             {
        //                 roleIds.Add(new ActiveDirectoryRole(role.AppRoleId.ToString()!));
        //             }
        //
        //             return roleIds;
        //         }
        //         catch (Exception e)
        //         {
        //             _logger.LogCritical(e, $"Exception in {nameof(ActiveDirectoryB2cService)}");
        //             throw;
        //         }
        //     }
        //
        //     private async ValueTask GrantAddedRoleToServicePrincipalAsync(
        //         string consumerServicePrincipalObjectId,
        //         Guid roleId)
        //     {
        //         var appRole = new AppRoleAssignment
        //         {
        //             PrincipalId = Guid.Parse(consumerServicePrincipalObjectId),
        //             ResourceId = Guid.Parse(_azureAdConfig.BackendAppServicePrincipalObjectId),
        //             AppRoleId = roleId
        //         };
        //
        //         var role = await _graphClient.ServicePrincipals[consumerServicePrincipalObjectId]
        //             .AppRoleAssignedTo
        //             .Request()
        //             .AddAsync(appRole).ConfigureAwait(false);
        //
        //         if (role is null)
        //         {
        //             throw new InvalidOperationException($"The object: '{nameof(role)}' is null.");
        //         }
        //     }
        //
        //     private async Task<Application> CreateAppInB2CAsync(
        //         string consumerAppName,
        //         IReadOnlyList<string> permissions)
        //     {
        //         var resourceAccesses = permissions.Select(permission =>
        //             new ResourceAccess
        //             {
        //                 Id = Guid.Parse(permission),
        //                 Type = "Role"
        //             }).ToList();
        //
        //         return await _graphClient.Applications
        //             .Request()
        //             .AddAsync(new Application
        //             {
        //                 DisplayName = consumerAppName,
        //                 Api = new ApiApplication
        //                 {
        //                     RequestedAccessTokenVersion = 2,
        //                 },
        //                 CreatedDateTime = DateTimeOffset.Now,
        //                 RequiredResourceAccess = new[]
        //                 {
        //                     new RequiredResourceAccess
        //                     {
        //                         ResourceAppId = _azureAdConfig.BackendAppId,
        //                         ResourceAccess = resourceAccesses
        //                     }
        //                 },
        //                 SignInAudience = "AzureADMultipleOrgs"
        //             }).ConfigureAwait(false);
        //     }
        //
        //     private async Task<ServicePrincipal> AddServicePrincipalToAppInB2CAsync(string consumerAppId)
        //     {
        //         var consumerServicePrincipal = new ServicePrincipal
        //         {
        //             AppId = consumerAppId
        //         };
        //
        //         return await _graphClient.ServicePrincipals
        //             .Request()
        //             .AddAsync(consumerServicePrincipal).ConfigureAwait(false);
        //     }
        // }
    }
}
