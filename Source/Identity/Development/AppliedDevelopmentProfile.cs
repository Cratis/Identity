// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.Development;

/// <summary>
/// The outcome of an enabled local-development profile.
/// </summary>
/// <param name="Options">The <see cref="DevelopmentOptions"/> that were applied.</param>
/// <param name="Root">The process-lifetime custody of the development keys.</param>
internal sealed record AppliedDevelopmentProfile(DevelopmentOptions Options, DevelopmentKeyStoreRoot Root)
{
    /// <summary>Gets the diagnostic directory path; key operations use <see cref="Root"/>, never this path.</summary>
    public string KeyDirectory => Root.DirectoryPath;

    /// <summary>
    /// Gets the Data Protection key directory, beside the development keys.
    /// </summary>
    public string DataProtectionPath => Path.Combine(KeyDirectory, DevelopmentKeyStore.DataProtectionDirectory);

    /// <summary>
    /// Makes the options the host resolves agree with the profile, and registers the profile for the client descriptors.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    public void Register(IServiceCollection services)
    {
        var directory = KeyDirectory;
        var development = Options;
        services.AddSingleton(development);
        // A factory registration lets the container own and dispose custody when the host shuts down.
        services.AddSingleton(_ => Root);
        services.PostConfigure<IdentityOptions>(options =>
        {
            options.SecretStore.Path = directory;
            options.Resources = [.. development.Resources];
        });
    }
}
