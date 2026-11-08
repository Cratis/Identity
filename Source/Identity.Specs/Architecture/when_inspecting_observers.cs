// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Mail;
using System.Reflection;
using Cratis.Chronicle;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Authorization;
using Cratis.Identity.ConsentFlow;
using Cratis.Identity.Revocation;
using Microsoft.Extensions.Logging;

namespace Cratis.Identity.Architecture;

public class when_inspecting_observers : Specification
{
    /// <summary>
    /// Pure: AccountProjection, AccountGuardProjection, UpstreamKeyLookupProjection, AccountLinkStateReducer.
    /// Outbox publisher: AccountPublications, keyed by FactId:eventTypeId (never sequence number).
    /// </summary>
    internal static readonly Type[] Classified =
    [
        typeof(AccountProjection), typeof(AccountGuardProjection), typeof(UpstreamKeyLookupProjection),
        typeof(AccountLinkStateReducer), typeof(AccountPublications)
    ];

    internal static Type[] Observers => typeof(Account).Assembly.GetTypes().Where(IsObserver).ToArray();

    internal static bool IsClassified(Type type) => Classified.Contains(type);

    static readonly Type[] _forbiddenCapabilities =
    [
        typeof(HttpClient), typeof(TimeProvider), typeof(IFenceStore), typeof(IFenceLog), typeof(GrantRevocation),
        typeof(IAuthorizationActivator), typeof(IConsentRequestStore)
    ];

    internal static bool IsForbidden(Type type) =>
        type.Namespace?.StartsWith("OpenIddict", StringComparison.Ordinal) == true ||
        type.Namespace?.StartsWith("System.Net.Mail", StringComparison.Ordinal) == true ||
        _forbiddenCapabilities.Contains(type) || type.GenericTypeArguments.Any(IsForbidden);

    internal static IEnumerable<Type> Dependencies(Type type) => type.GetConstructors()
        .SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))
        .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(field => field.FieldType));

    static bool IsObserver(Type type) => type.GetInterfaces().Any(@interface =>
        @interface == typeof(IReactor) || (@interface.IsGenericType &&
        (@interface.GetGenericTypeDefinition() == typeof(IProjectionFor<>) || @interface.GetGenericTypeDefinition() == typeof(IReducerFor<>))));

    [Fact] void should_classify_every_observer() => Observers.OrderBy(type => type.FullName).ShouldEqual(Classified.OrderBy(type => type.FullName));
    [Fact] void should_keep_pure_observers_dependency_free() => Classified.Where(type => type != typeof(AccountPublications)).SelectMany(Dependencies).ShouldBeEmpty();
    [Fact] void should_limit_the_publisher_to_the_event_store_and_logger() => typeof(AccountPublications).GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType).ShouldEqual([typeof(IEventStore), typeof(ILogger<AccountPublications>)]);
    [Fact] void should_have_no_token_grant_clock_or_mail_capability() => Observers.SelectMany(Dependencies).Where(IsForbidden).ShouldBeEmpty();
    [Fact] void should_reject_an_unclassified_side_effecting_observer() => IsClassified(typeof(UnclassifiedObserver)).ShouldBeFalse();
    [Fact] void should_detect_a_mail_dependency() => IsForbidden(typeof(SmtpClient)).ShouldBeTrue();
    [Fact] void should_detect_the_unclassified_observers_side_effect_capability() => Dependencies(typeof(UnclassifiedObserver)).Any(IsForbidden).ShouldBeTrue();

    sealed class UnclassifiedObserver(HttpClient client) : IReactor
    {
        internal HttpClient Client { get; } = client;
    }
}
