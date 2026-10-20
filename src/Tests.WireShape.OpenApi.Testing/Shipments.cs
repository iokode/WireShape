using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IOKode.WireShape.OpenApi.Testing.Tests;

/// <summary>
/// What a shipment carries, which is what the documents of these tests describe rightly or wrongly.
/// </summary>
public readonly record struct TrackingCode(string Value);

public readonly record struct ParcelWeight(decimal Grams)
{
    public decimal Kilograms => Grams / 1000;
}

public enum CarrierService
{
    NextDay = 1,
    Economy = 2
}

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
