// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Cratis.Identity.for_VectorArtifacts.given;

public class an_artifact : Specification
{
    protected const string VectorFile = "profiles-and-discovery.json";
    static readonly JsonSerializerOptions _serializationOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    protected string _directory;
    protected JsonElement _manifest;
    protected JsonElement[] _vectors;
    protected JsonElement[] _cases;
    protected string _version;

    protected static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    protected static string CaseHash(JsonElement vector) => Hash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(vector, _serializationOptions)));

    void Establish()
    {
        _directory = Path.Combine(AppContext.BaseDirectory, "Vectors", "arc-mcp-authorization-v1");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(_directory, "manifest.json")));
        _manifest = manifest.RootElement.Clone();
        using var vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(_directory, VectorFile)));
        _vectors = vectors.RootElement.GetProperty("given").GetProperty("vectors").EnumerateArray().Select(vector => vector.Clone()).ToArray();
        _cases = _manifest.GetProperty("cases").EnumerateArray().ToArray();
        _version = _manifest.GetProperty("version").GetString();
    }
}
