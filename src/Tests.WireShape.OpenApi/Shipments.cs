using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IOKode.WireShape.OpenApi.Tests;

/// <summary>
/// What a shipment carries in its calls, which is what these tests declare forms for.
/// </summary>
public readonly record struct TrackingCode(string Value);

public readonly record struct DeclaredValue(decimal Amount, string Currency);

public readonly record struct DispatchedOn(DateOnly Day);

public sealed class TrackingCodeJsonConverter : JsonConverter<TrackingCode>
{
    public override TrackingCode Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        new(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, TrackingCode value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
