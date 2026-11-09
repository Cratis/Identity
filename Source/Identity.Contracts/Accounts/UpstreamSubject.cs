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
    /// <param name="value">The stored subject, or an empty string after compliance erasure.</param>
    /// <remarks>The constructor supports replay. Use <see cref="Create"/> for new upstream observations.</remarks>
    public UpstreamSubject(string value) : base(value)
    {
    }

    /// <summary>Converts and validates a verified upstream subject.</summary>
    /// <param name="value">The subject.</param>
    public static implicit operator UpstreamSubject(string value) => Create(value);

    /// <summary>Validates a new upstream observation, never an erased replay value.</summary>
    /// <param name="value">The exact subject.</param>
    /// <returns>The validated subject.</returns>
    /// <exception cref="InvalidAccountValue">The subject is blank, too long or contains control characters.</exception>
    public static UpstreamSubject Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!IsValid(value))
        {
            throw new InvalidAccountValue(nameof(UpstreamSubject));
        }

        return new(value);
    }

    static bool IsValid(string value) => !string.IsNullOrWhiteSpace(value) && Encoding.UTF8.GetByteCount(value) <= 256 && !value.Any(char.IsControl);
}
