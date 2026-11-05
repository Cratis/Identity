// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Gives key-rotation specs their own host, so changing keys cannot disturb the shared one.
/// </summary>
[CollectionDefinition(Name)]
public class RotationCollection : ICollectionFixture<RotatingIdentityHost>
{
    public const string Name = "Rotation";
}
