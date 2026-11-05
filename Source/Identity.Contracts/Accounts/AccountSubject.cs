// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Identity.Contracts.Accounts;

/// <summary>
/// The immutable, canonical lowercase UUID v4 identifying an account.
/// </summary>
public record AccountSubject : ConceptAs<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountSubject"/> record.
    /// </summary>
    /// <param name="value">The canonical subject.</param>
    public AccountSubject(string value) : base(value)
    {
        if (!IsCanonical(value))
        {
            throw new InvalidAccountValue(nameof(AccountSubject));
        }
    }

    /// <summary>Converts a canonical subject.</summary>
    /// <param name="value">The subject.</param>
    public static implicit operator AccountSubject(string value) => new(value);

    /// <summary>Allocates a never-reused subject.</summary>
    /// <returns>The subject.</returns>
    public static AccountSubject New() => new(Guid.NewGuid().ToString("D"));

    /// <summary>Validates without normalizing an external subject.</summary>
    /// <param name="value">The supplied subject.</param>
    /// <param name="subject">The validated subject.</param>
    /// <returns>Whether the subject is canonical.</returns>
    public static bool TryParse(string? value, out AccountSubject? subject)
    {
        subject = IsCanonical(value) ? new(value!) : null;
        return subject is not null;
    }

    static bool IsCanonical(string? value) => value is { Length: 36 } &&
        Guid.TryParseExact(value, "D", out var id) && id.ToString("D") == value &&
        value[14] == '4' && value[19] is '8' or '9' or 'a' or 'b';
}
