// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Runner.Common;
using Xunit.Sdk;

namespace Cratis.Identity.Integration.for_LifecycleReporter;

public class when_handling_lifecycle_messages : Specification
{
    const string ClassName = "Synthetic.Namespace.SyntheticSpecification";
    const string DisplayName = "synthetic-display-name-that-must-not-appear";
    const string ClassId = "class-1";

    readonly RecordingLogger _logger = new();
    readonly Dictionary<string, IReadOnlyCollection<string>> _traits = [];
    IMessageSink _sink;

    void Establish() =>
        _sink = new LifecycleReporter().CreateMessageHandler(_logger, null).AsTask().GetAwaiter().GetResult();

    void Because()
    {
        _sink.OnMessage(new TestClassStarting
        {
            AssemblyUniqueID = "assembly-1",
            TestCollectionUniqueID = "collection-1",
            TestClassUniqueID = ClassId,
            StartTime = DateTimeOffset.UtcNow,
            TestClassName = ClassName,
            TestClassNamespace = "Synthetic.Namespace",
            TestClassSimpleName = "SyntheticSpecification",
            Traits = _traits
        });
        _sink.OnMessage(Starting("test-1", ClassId));
        _sink.OnMessage(new TestFinished
        {
            AssemblyUniqueID = "assembly-1",
            TestCollectionUniqueID = "collection-1",
            TestClassUniqueID = ClassId,
            TestMethodUniqueID = "method-1",
            TestCaseUniqueID = "case-1",
            TestUniqueID = "test-1",
            Attachments = new Dictionary<string, TestAttachment>(),
            ExecutionTime = 0m,
            FinishTime = DateTimeOffset.UtcNow,
            Output = string.Empty,
            Warnings = null
        });
        _sink.OnMessage(new TestClassFinished
        {
            AssemblyUniqueID = "assembly-1",
            TestCollectionUniqueID = "collection-1",
            TestClassUniqueID = ClassId,
            ExecutionTime = 0m,
            FinishTime = DateTimeOffset.UtcNow,
            TestsFailed = 0,
            TestsNotRun = 0,
            TestsSkipped = 0,
            TestsTotal = 1
        });
        _sink.OnMessage(Starting("test-2", "class-never-started"));
    }

    [Fact] void should_use_the_identity_lifecycle_switch() => new LifecycleReporter().RunnerSwitch.ShouldEqual("identity-lifecycle");
    [Fact] void should_not_be_enabled_by_the_environment() => new LifecycleReporter().IsEnvironmentallyEnabled.ShouldBeFalse();
    [Fact] void should_mark_five_events() => _logger.Lines.Count.ShouldEqual(5);
    [Fact] void should_mark_the_class_start_first() => _logger.Lines[0].ShouldContain($"class started {ClassName}", StringComparison.Ordinal);
    [Fact] void should_mark_the_fact_start_with_its_class() => _logger.Lines[1].ShouldContain($"fact started in {ClassName}", StringComparison.Ordinal);
    [Fact] void should_mark_the_fact_finish_with_its_class() => _logger.Lines[2].ShouldContain($"fact finished in {ClassName}", StringComparison.Ordinal);
    [Fact] void should_resolve_the_class_before_its_metadata_is_removed() => _logger.Lines[3].ShouldContain($"class finished {ClassName}", StringComparison.Ordinal);
    [Fact] void should_use_a_fixed_label_for_an_unknown_class() => _logger.Lines[4].ShouldContain("fact started in <unknown class>", StringComparison.Ordinal);
    [Fact] void should_not_print_test_display_names() => _logger.Lines.ShouldEachConformTo(line => !line.Contains(DisplayName, StringComparison.Ordinal));
    [Fact] void should_tag_every_line() => _logger.Lines.ShouldEachConformTo(line => line.StartsWith("[identity-lifecycle] ", StringComparison.Ordinal));

    TestStarting Starting(string testId, string classId) => new()
    {
        AssemblyUniqueID = "assembly-1",
        TestCollectionUniqueID = "collection-1",
        TestClassUniqueID = classId,
        TestMethodUniqueID = "method-1",
        TestCaseUniqueID = "case-1",
        TestUniqueID = testId,
        Explicit = false,
        StartTime = DateTimeOffset.UtcNow,
        TestDisplayName = DisplayName,
        TestLabel = null,
        Timeout = 0,
        Traits = _traits
    };

    sealed class RecordingLogger : IRunnerLogger
    {
        public List<string> Lines { get; } = [];

        public object LockObject { get; } = new();

        public void LogImportantMessage(StackFrameInfo stackFrame, string message) => Lines.Add(message);

        public void LogMessage(StackFrameInfo stackFrame, string message) => Lines.Add(message);

        public void LogWarning(StackFrameInfo stackFrame, string message) => Lines.Add(message);

        public void LogError(StackFrameInfo stackFrame, string message) => Lines.Add(message);

        public void LogRaw(string message) => Lines.Add(message);

        public void WaitForAcknowledgment()
        {
        }
    }
}
