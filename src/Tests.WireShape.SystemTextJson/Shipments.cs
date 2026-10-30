using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IOKode.WireShape.SystemTextJson.Tests;

/// <summary>
/// What a parcel service carries in its calls, which is the wire these tests ask about.
/// </summary>
public readonly record struct TrackingCode(string Value);

public sealed record Consignment(TrackingCode Code, string? Instructions, int? DeclaredValue)
{
    /// <summary>What a courier reads on the manifest, which is answered and never sent in.</summary>
    public string Summary => $"{Code.Value} ({DeclaredValue?.ToString() ?? "no value"})";

    /// <summary>The code the courier enters at the gate, which is sent in and never answered back.</summary>
    public string GateCode
    {
        set => gateCode = value;
    }

    /// <summary>Whether the courier was given a code for the gate.</summary>
    public bool NeedsGateCode => gateCode.Length > 0;

    private string gateCode = string.Empty;
}

/// <summary>What a shipment turns out to be, written down where the options read it.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Express), "express")]
[JsonDerivedType(typeof(Overnight), "overnight")]
public abstract record Shipment;

public sealed record Express : Shipment;

public sealed record Overnight : Shipment;

public enum CarrierService
{
    NextDay = 1,
    Economy = 2
}

public enum ParcelSize
{
    Small = 1,
    Large = 2
}

public sealed class TrackingCodeJsonConverter : JsonConverter<TrackingCode>
{
    public override TrackingCode Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        new(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, TrackingCode value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}

/// <summary>What happened to a consignment, under words a serializer writes escaped.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ConsignmentChange>))]
public enum ConsignmentChange
{
    [JsonStringEnumMemberName("añadido")] Added,
    [JsonStringEnumMemberName("a+b")] Joined
}
