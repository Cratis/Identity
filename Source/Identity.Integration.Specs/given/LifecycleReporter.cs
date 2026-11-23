// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit.Runner.Common;
using Xunit.Sdk;

[assembly: RegisterRunnerReporter(typeof(LifecycleReporter))]

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Temporary opt-in runner reporter (selected with the identity-lifecycle reporter switch) that prints static lifecycle markers for
/// Cratis/Identity#228: assembly, collection, class and fact starts and finishes, with class names only.
/// </summary>
/// <remarks>
/// It never prints test display names, arguments, output or exception content. Everything else is the default reporter.
/// </remarks>
public class LifecycleReporter : DefaultRunnerReporter
{
    /// <inheritdoc/>
    public override string Description => "show standard progress messages plus static lifecycle markers (temporary diagnostic)";

    /// <inheritdoc/>
    public override string? RunnerSwitch => "identity-lifecycle";

    /// <inheritdoc/>
    public override ValueTask<IRunnerReporterMessageHandler> CreateMessageHandler(IRunnerLogger logger, IMessageSink? diagnosticMessageSink) =>
        new(new Handler(logger));

    sealed class Handler(IRunnerLogger logger) : DefaultRunnerReporterMessageHandler(logger)
    {
        const string UnknownClass = "<unknown class>";
        const string NoCollectionClass = "<no collection class>";

        protected override void HandleTestAssemblyStarting(MessageHandlerArgs<ITestAssemblyStarting> args)
        {
            base.HandleTestAssemblyStarting(args);
            Mark("assembly started", string.Empty);
        }

        protected override void HandleTestAssemblyFinished(MessageHandlerArgs<ITestAssemblyFinished> args)
        {
            base.HandleTestAssemblyFinished(args);
            Mark("assembly finished", string.Empty);
        }

        protected override void HandleTestCollectionStarting(MessageHandlerArgs<ITestCollectionStarting> args)
        {
            base.HandleTestCollectionStarting(args);
            Mark("collection started", args.Message.TestCollectionClassName ?? NoCollectionClass);
        }

        protected override void HandleTestCollectionFinished(MessageHandlerArgs<ITestCollectionFinished> args)
        {
            var name = MetadataCache.TryGetCollectionMetadata(args.Message)?.TestCollectionClassName ?? NoCollectionClass;
            base.HandleTestCollectionFinished(args);
            Mark("collection finished", name);
        }

        protected override void HandleTestClassStarting(MessageHandlerArgs<ITestClassStarting> args)
        {
            base.HandleTestClassStarting(args);
            Mark("class started", args.Message.TestClassName);
        }

        protected override void HandleTestClassFinished(MessageHandlerArgs<ITestClassFinished> args)
        {
            var name = ClassName(args.Message);
            base.HandleTestClassFinished(args);
            Mark("class finished", name);
        }

        protected override void HandleTestStarting(MessageHandlerArgs<ITestStarting> args)
        {
            base.HandleTestStarting(args);
            Mark("fact started in", ClassName(args.Message));
        }

        protected override void HandleTestFinished(MessageHandlerArgs<ITestFinished> args)
        {
            var name = ClassName(args.Message);
            base.HandleTestFinished(args);
            Mark("fact finished in", name);
        }

        string ClassName(ITestClassMessage message) =>
            message.TestClassUniqueID is null ? UnknownClass : MetadataCache.TryGetClassMetadata(message)?.TestClassName ?? UnknownClass;

        void Mark(string phase, string name) =>
            Logger.LogImportantMessage(StackFrameInfo.None, $"[identity-lifecycle] {DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)} {phase} {name}".TrimEnd());
    }
}
