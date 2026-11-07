// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Identity.ConsentFlow;

/// <summary>Validated operational consent deadlines and bounded reconciliation settings.</summary>
internal sealed class ConsentActivationOptions : IValidatableObject
{
    /// <summary>Gets or sets the time allowed to activate a request.</summary>
    public TimeSpan ActivationDeadline { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Gets or sets how long safe terminal rows remain before TTL removal.</summary>
    public TimeSpan TerminalRetention { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Gets or sets the bounded reconciliation batch size.</summary>
    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;

    /// <summary>Gets or sets the interval between reconciliation passes.</summary>
    public TimeSpan ReconcileInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ActivationDeadline <= TimeSpan.Zero || ActivationDeadline > TimeSpan.FromDays(1))
        {
            yield return new("ActivationDeadline must be positive and no greater than one day.", [nameof(ActivationDeadline)]);
        }

        if (TerminalRetention < TimeSpan.FromDays(1) || TerminalRetention > TimeSpan.FromDays(365))
        {
            yield return new("TerminalRetention must be between one and 365 days.", [nameof(TerminalRetention)]);
        }

        if (ReconcileInterval < TimeSpan.FromSeconds(1) || ReconcileInterval > TimeSpan.FromHours(1))
        {
            yield return new("ReconcileInterval must be between one second and one hour.", [nameof(ReconcileInterval)]);
        }
    }
}
