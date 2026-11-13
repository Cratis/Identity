// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

/// <summary>The kernel container stopped before its token endpoint became ready.</summary>
/// <param name="exitCode">The container's exit code.</param>
/// <param name="output">The tail of the kernel's output.</param>
public sealed class ChronicleKernelExited(long exitCode, string output)
    : Exception($"The Chronicle kernel container exited with code {exitCode} before it became ready.\n{output}");
