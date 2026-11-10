// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Clients;
using Cratis.Identity.ConsentFlow;
using Cratis.Identity.Device;
using Cratis.Identity.DPoP;
using Cratis.Identity.Protocol;
using Cratis.Identity.Resources;
using Cratis.Identity.Revocation;
using Cratis.Identity.Tenants;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_resolving_feature_registrations : given.registered_features
{
    [Fact] void should_resolve_resources() => enabled_provider.GetRequiredService<IResourceCatalog>().ShouldNotBeNull();
    [Fact] void should_resolve_tenants() => enabled_provider.GetRequiredService<ITenantDirectory>().ShouldNotBeNull();
    [Fact] void should_resolve_clients() => enabled_provider.GetRequiredService<ClientDescriptors>().ShouldNotBeNull();
    [Fact] void should_resolve_consent_proof() => enabled_provider.GetRequiredService<IConsentProof>().ShouldNotBeNull();
    [Fact] void should_resolve_the_account_reader() => scope.ServiceProvider.GetRequiredService<IAccountReader>().ShouldBeOfExactType<AccountReader>();
    [Fact] void should_resolve_the_account_sign_in() => scope.ServiceProvider.GetRequiredService<IAccountSignIn>().ShouldBeOfExactType<EventSourcedAccountAdapter>();
    [Fact] void should_resolve_the_protocol_boundary() => scope.ServiceProvider.GetRequiredService<IProtocolCommands>().ShouldBeOfExactType<ProtocolCommands>();
    [Fact] void should_resolve_projection_progress() => scope.ServiceProvider.GetRequiredService<IObserverProgress>().ShouldBeOfExactType<ObserverProgress>();
    [Fact] void should_resolve_read_model_catch_up() => scope.ServiceProvider.GetRequiredService<ReadModelCatchUp>().ShouldNotBeNull();
    [Fact] void should_resolve_verified_upstream_observations() => scope.ServiceProvider.GetRequiredService<IVerifiedUpstreamSignIn>().ShouldBeOfExactType<VerifiedUpstreamSignIn>();
    [Fact] void should_resolve_consent_activation() => scope.ServiceProvider.GetRequiredService<ConsentActivation>().ShouldNotBeNull();
    [Fact] void should_resolve_the_recovery_gate() => enabled_provider.GetRequiredService<RecoveryGate>().ShouldNotBeNull();
    [Fact] void should_resolve_the_subject_token_validator() => enabled_provider.GetRequiredService<ISubjectTokenValidator>().ShouldBeOfExactType<SubjectTokenValidator>();
    [Fact] void should_resolve_the_shared_device_admission() => scope.ServiceProvider.GetRequiredService<DeviceGrantAdmission>().ShouldNotBeNull();
    [Fact] void should_enable_device_state_only_when_requested() => enabled_provider.GetRequiredService<IDeviceGrantStateStore>().ShouldBeOfExactType<MongoDeviceGrantStateStore>();
    [Fact] void should_enable_dpop_only_when_requested() => enabled_provider.GetRequiredService<DPoPProofValidator>().ShouldNotBeNull();
    [Fact] void should_not_register_disabled_dpop() => disabled_services.Any(service => service.ServiceType == typeof(DPoPProofValidator)).ShouldBeFalse();
    [Fact] void should_not_register_disabled_device_state() => disabled_services.Any(service => service.ServiceType == typeof(IDeviceGrantStateStore)).ShouldBeFalse();
    [Fact] void should_not_register_disabled_device_endpoints() => disabled_services.Any(service => service.ServiceType == typeof(DeviceAuthorizationEndpoint) || service.ServiceType == typeof(DeviceVerificationEndpoint)).ShouldBeFalse();
    [Fact] void should_keep_admission_when_device_grants_are_disabled() => disabled_services.Any(service => service.ServiceType == typeof(DeviceGrantAdmission)).ShouldBeTrue();
}
