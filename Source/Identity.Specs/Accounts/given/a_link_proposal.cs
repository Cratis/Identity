// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.given;

public class a_link_proposal : an_account_store
{
    protected AccountSubject _account;
    protected LinkProposalId _proposal;
    protected VerifiedAuthentication _proof;

    async Task Establish()
    {
        _account = AccountSubject.New();
        await Seed(_account, GitHubIssuer, "existing-key");
        await Refresh();
        _proof = Verified(GitHubIssuer, "candidate-key", FactId.New(), Session(_account));
    }

    protected async Task Propose(bool quarantine = false)
    {
        _proposal = await _linking.Propose(Session(_account), _proof, new(true, SoftDisabledConnection: quarantine));
        await Refresh();
    }

    protected static async Task<string?> Refusal(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (AccountOperationRefused exception)
        {
            return exception.Message;
        }
    }
}
