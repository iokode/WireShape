using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IOKode.WireShape.ComponentModel.Tests;

/// <summary>
/// What a parcel service carries in its calls, which is what these tests declare forms for.
/// </summary>
public readonly record struct TrackingCode(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct ParcelWeight(decimal Grams);

public sealed record Consignee(string Name, string Street);

public sealed class TrackingCodeJsonConverter : JsonConverter<TrackingCode>
{
    public override TrackingCode Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        new(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, TrackingCode value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}

public sealed class ParcelWeightJsonConverter : JsonConverter<ParcelWeight>
{
    public override ParcelWeight Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        new(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, ParcelWeight value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Grams);
}
