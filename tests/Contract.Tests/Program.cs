using System.Text.Json;
using Square.Contracts;
using Square.Contracts.Rpc;
using Square.Contracts.Serialization;
using Square.Domain.Primitives;
using Square.TestKit;

return TestRunner.Run(
    ("strong IDs use one canonical JSON string", StrongIdsUseCanonicalJson),
    ("canonical UTC and schema versions round trip", PrimitiveContractsRoundTrip),
    ("unknown fields are rejected", UnknownFieldsAreRejected),
    ("RPC envelope shape is stable", RpcEnvelopeShapeIsStable));

static void StrongIdsUseCanonicalJson()
{
    JsonSerializerOptions options = SquareJson.CreateOptions();
    ProjectId projectId = ProjectId.Parse("prj_01JZ9H6Y8N4T2C3V5B7M9Q1WXE");
    string json = JsonSerializer.Serialize(projectId, options);
    AssertEx.Equal("\"prj_01JZ9H6Y8N4T2C3V5B7M9Q1WXE\"", json);
    AssertEx.Equal(projectId, JsonSerializer.Deserialize<ProjectId>(json, options));
}
static void PrimitiveContractsRoundTrip()
{
    JsonSerializerOptions options = SquareJson.CreateOptions();
    Sample sample = new(ContractVersions.DraftV1, new UtcInstant(new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero)), ContentHash.Compute(new byte[] { 1, 2, 3 }));
    string json = JsonSerializer.Serialize(sample, options);
    Sample? restored = JsonSerializer.Deserialize<Sample>(json, options);
    AssertEx.Equal(sample, restored ?? throw new InvalidOperationException("Round-trip returned null."));
}
static void UnknownFieldsAreRejected()
{
    JsonSerializerOptions options = SquareJson.CreateOptions();
    string json = "{\"version\":\"1.0\",\"at\":\"2026-08-07T00:00:00.0000000Z\",\"hash\":\"sha256:039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81\",\"surprise\":true}";
    AssertEx.Throws<JsonException>(() => JsonSerializer.Deserialize<Sample>(json, options));
}
static void RpcEnvelopeShapeIsStable()
{
    RpcResponseEnvelope<string> success = new(ContractVersions.RpcProtocol, ContractVersions.DraftV1, CorrelationId.Parse("cor_01JZ9H6Y8N4T2C3V5B7M9Q1WXE"), "ok", null);
    string json = JsonSerializer.Serialize(success, SquareJson.CreateOptions());
    AssertEx.True(json.Contains("\"protocol\":\"square.rpc\"", StringComparison.Ordinal), "Protocol must be explicit.");
    AssertEx.True(json.Contains("\"version\":\"1.0\"", StringComparison.Ordinal), "Version must be explicit.");
}
file sealed record Sample(SchemaVersion Version, UtcInstant At, ContentHash Hash);
