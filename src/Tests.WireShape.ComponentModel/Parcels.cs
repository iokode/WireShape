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

/// <summary>When a courier calls, which is a time of day.</summary>
public readonly record struct PickupAt(TimeOnly Time);

/// <summary>When a parcel was collected, which is an instant.</summary>
public readonly record struct CollectedAt(DateTime Moment);

/// <summary>When a parcel was delivered, which is an instant where it was.</summary>
public readonly record struct DeliveredAt(DateTimeOffset Moment);

public readonly record struct Fragile(bool Value);

/// <summary>
/// A value written as whatever carries it, and read back from it, which is all the converters of these values
/// do.
/// </summary>
public sealed class CarriedJsonConverter<TValue, TCarried>(Func<TCarried, TValue> read, Func<TValue, TCarried> write)
    : JsonConverter<TValue>
{
    public override TValue Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        read(JsonSerializer.Deserialize<TCarried>(ref reader, options)!);

    public override void Write(Utf8JsonWriter writer, TValue value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, write(value), options);
}
