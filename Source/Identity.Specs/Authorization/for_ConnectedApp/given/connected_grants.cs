// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization.ConnectedApps;
using Cratis.Identity.Revocation;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Authorization.for_ConnectedApp.given;

public class connected_grants : Specification
{
    protected const string Subject = "2e08ca46-2146-4ae2-a8d8-f288f6805158";
    protected IOpenIddictAuthorizationManager _authorizations;
    protected IOpenIddictApplicationManager _applications;
    private protected IFenceStore _fences;
    private protected ConnectedAppReader _reader;
    protected IHttpContextAccessor _context;
    private protected ConnectedApp[] _result;

    void Establish()
    {
        _authorizations = Substitute.For<IOpenIddictAuthorizationManager>();
        _applications = Substitute.For<IOpenIddictApplicationManager>();
        _fences = Substitute.For<IFenceStore>();
        _reader = new(_authorizations, _applications, _fences);
        _context = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, Subject)], "session")) } };
    }

    protected object Grant(string id, DateTimeOffset at, string status = OpenIddictConstants.Statuses.Valid)
    {
        var grant = new object();
        _authorizations.GetIdAsync(grant, Arg.Any<CancellationToken>()).Returns(id);
        _authorizations.GetCreationDateAsync(grant, Arg.Any<CancellationToken>()).Returns(at);
        _authorizations.GetApplicationIdAsync(grant, Arg.Any<CancellationToken>()).Returns("application");
        _authorizations.GetScopesAsync(grant, Arg.Any<CancellationToken>()).Returns(["openid", "profile"]);
        _authorizations.HasStatusAsync(grant, OpenIddictConstants.Statuses.Valid, Arg.Any<CancellationToken>()).Returns(status == OpenIddictConstants.Statuses.Valid);
        return grant;
    }

    protected void Grants(params object[] grants) => _authorizations.FindBySubjectAsync(Subject, Arg.Any<CancellationToken>()).Returns(grants.ToAsyncEnumerable());
}
