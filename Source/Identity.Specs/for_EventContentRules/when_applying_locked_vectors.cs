// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Contracts.Check;

namespace Cratis.Identity.for_EventContentRules;

public class when_applying_locked_vectors : Specification
{
    public static TheoryData<string, string, string> Cases()
    {
        var cases = new TheoryData<string, string, string>();
        foreach (var name in new[] { "forbidden-content", "pii-markings" })
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Vectors", "identity-events-v1", $"{name}.json")));
            foreach (var test in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                cases.Add(test.GetProperty("name").GetString(), document.RootElement.GetProperty("context").GetRawText(), test.GetRawText());
            }
        }
        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void should_match_the_locked_outcome(string id, string contextJson, string json)
    {
        using var document = JsonDocument.Parse(json);
        using var context = JsonDocument.Parse(contextJson);
        var vector = document.RootElement;
        id.ShouldNotBeEmpty();
        var given = vector.GetProperty("given");
        var violations = ContentRules.Check(VectorShapeBuilder.Build(given, context.RootElement), VectorShapeBuilder.Registry(given));
        var expected = vector.GetProperty("then");
        if (expected.GetProperty("outcome").GetString() == "pass") violations.ShouldBeEmpty();
        else violations.ShouldNotBeEmpty();
        var expectedViolations = expected.GetProperty("violations").EnumerateArray().Select(violation =>
            new Violation(violation.GetProperty("path").GetString(), violation.GetProperty("reason").GetString())).ToArray();
        violations.ToArray().ShouldEqual(expectedViolations);
    }
}
