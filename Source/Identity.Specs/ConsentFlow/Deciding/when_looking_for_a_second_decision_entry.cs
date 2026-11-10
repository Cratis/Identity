// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Identity.ConsentFlow.Deciding;

/// <summary>Documents the weaker second decision entry (<c language="text">ConsentActivation.Decide(binding)</c>) that no command reaches today.</summary>
public class when_looking_for_a_second_decision_entry : Specification
{
    MethodInfo[] _commandHandlers;

    void Because() => _commandHandlers = typeof(ConsentActivation).Assembly.GetTypes()
        .Where(_ => Attribute.IsDefined(_, typeof(CommandAttribute)))
        .SelectMany(_ => _.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        .Where(_ => _.Name == "Handle")
        .ToArray();

    [Fact] void should_find_the_consent_commands() => _commandHandlers.Count(_ => _.DeclaringType.Namespace == typeof(ConsentActivation).Namespace).ShouldEqual(2);
    [Fact] void should_not_reach_the_activation_from_any_command() => _commandHandlers.SelectMany(_ => _.GetParameters()).Count(_ => _.ParameterType == typeof(ConsentActivation)).ShouldEqual(0);
    [Fact] void should_not_reach_the_activation_from_the_interaction() => typeof(ConsentInteraction).GetConstructors().SelectMany(_ => _.GetParameters()).Count(_ => _.ParameterType == typeof(ConsentActivation)).ShouldEqual(0);
    [Fact] void should_still_expose_the_entry_on_the_barrier() => typeof(ConsentActivation).GetMethod(nameof(ConsentActivation.Decide)).GetParameters()[1].ParameterType.ShouldEqual(typeof(ConsentBinding));
}
