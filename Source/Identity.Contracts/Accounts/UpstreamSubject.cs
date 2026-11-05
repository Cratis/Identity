// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>The exact immutable subject vouched for by an upstream adapter.</summary>
[PII, ComplianceDetails("Raw upstream subject, retained only inside the account erasure boundary.")]
public record UpstreamSubject : ConceptAs<string>
{
    /// <summary>Initializes a new instance of the <see cref="UpstreamSubject"/> record.</summary>
    /// <param name="value">The exact subject.</param>
    public UpstreamSubject(string value) : base(value)
    {
        if (!IsValid(value))
        {
            throw new InvalidAccountValue(nameof(UpstreamSubject));
        }
    }

    /// <summary>Converts a verified upstream subject.</summary>
    /// <param name="value">The subject.</param>
    public static implicit operator UpstreamSubject(string value) => new(value);

    static bool IsValid(string value) => !string.IsNullOrWhiteSpace(value) && Encoding.UTF8.GetByteCount(value) <= 256 && !value.Any(char.IsControl);
}
