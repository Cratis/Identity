// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Keys;

/// <summary>
/// The exception that is thrown when the secret store holds something that is not a valid key set. The whole
/// snapshot is refused; a partly loaded ring could silently drop a key that issued tokens still depend on.
/// </summary>
/// <remarks>
/// The message carries the file name and a fixed reason, never key bytes.
/// </remarks>
/// <param name="file">The file the problem was found in.</param>
/// <param name="reason">Why the file was refused.</param>
internal class InvalidKeyMaterial(string file, string reason) : Exception($"Secret store file \"{file}\" is refused: {reason}")
{
    /// <summary>
    /// Gets the file the problem was found in.
    /// </summary>
    public string File { get; } = file;

    /// <summary>
    /// Gets why the file was refused.
    /// </summary>
    public string Reason { get; } = reason;
}
