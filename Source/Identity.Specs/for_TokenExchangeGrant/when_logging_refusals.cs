// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_logging_refusals : given.a_token_exchange_request
{
    const string SecretToken = "subject-token-placeholder";
    const string ExternalSubject = "external-subject-1";

    async Task Because()
    {
        await Handle();

        _validator.Validate(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Refused(SubjectTokenRefusal.Invalid)));
        await Handle();
        _validator.Validate(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Refused(SubjectTokenRefusal.KeysUnavailable)));
        await Handle();
        _validator.Validate(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Accepted(_subject)));

        _keys.Find(default!, default!).ReturnsForAnyArgs(Task.FromResult<UpstreamKeyMatch?>(null));
        await Handle();
        _keys.Find(default!, default!).ReturnsForAnyArgs(Task.FromResult<UpstreamKeyMatch?>(new UpstreamKeyMatch(_account.Id, default!, true)));

        _tenants.SelectableTenants(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult<TenantLookup>(new TenantsUnavailable(TenantsUnavailableReason.Unreachable)));
        await Handle();
        _tenants.SelectableTenants(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult<TenantLookup>(new TenantsListed([new SelectableTenant("other", "Other")])));
        await Handle();
        _tenants.SelectableTenants(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult<TenantLookup>(new TenantsListed([new SelectableTenant("cratis", "Cratis")])));

        _replay.Record(_subject.ReplayKey, _time.Now.AddMinutes(5));
        await Handle();
    }

    [Fact] void should_have_logged_the_refusals() => _logger.Messages.Count.ShouldBeGreaterThan(4);
    [Fact] void should_never_log_the_subject_token() => _logger.Messages.ShouldNotContain(_ => _.Contains(SecretToken));
    [Fact] void should_never_log_the_external_subject() => _logger.Messages.ShouldNotContain(_ => _.Contains(ExternalSubject));
    [Fact] void should_never_log_the_account_name() => _logger.Messages.ShouldNotContain(_ => _.Contains(_account.Name.Value));
    [Fact] void should_never_log_the_github_login() => _logger.Messages.ShouldNotContain(_ => _.Contains(_account.DisplayLogin().Value));
    [Fact] void should_never_log_the_github_id() => _logger.Messages.ShouldNotContain(_ => _.Contains(_account.LinkedUpstreamKeys.Single().IdpSub.Value));
}
