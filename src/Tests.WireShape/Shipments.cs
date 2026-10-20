using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IOKode.WireShape.Tests;

/// <summary>
/// What a parcel service carries in its calls, which is what these tests work the types of an API out of.
/// </summary>
public readonly record struct TrackingCode(string Value);

public readonly record struct DispatchedOn(DateOnly Day);

public sealed record Consignment(TrackingCode Code, string? Instructions, IReadOnlyList<Parcel> Parcels);

public sealed record Route(string Name, Route? Next);

public abstract record Parcel;

/// <summary>What a shipment turns out to be, written down where whoever writes this wire reads it.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Express), "express")]
[JsonDerivedType(typeof(Overnight), "overnight")]
public abstract record Shipment;

public sealed record Express : Shipment;

public sealed record Overnight : Shipment;

public sealed record Envelope : Parcel;

public sealed record Pallet : Parcel;

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

public enum DeliveryWindow
{
    Morning = 1,
    Afternoon = 2
}

/// <summary>The window travels as the code the depots have used for it since before this wire existed.</summary>
public sealed class DeliveryWindowJsonConverter : JsonConverter<DeliveryWindow>
{
    public override DeliveryWindow Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        reader.GetInt32() == 7 ? DeliveryWindow.Morning : DeliveryWindow.Afternoon;

    public override void Write(Utf8JsonWriter writer, DeliveryWindow value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value == DeliveryWindow.Morning ? 7 : 9);
}

public sealed class TrackingCodeJsonConverter : JsonConverter<TrackingCode>
{
    public override TrackingCode Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        new(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, TrackingCode value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}

/// <summary>What the parcels of this wire turn out to be, as the wire of these tests tells them apart.</summary>
public sealed class Parcels : IHierarchySource
{
    /// <remarks>
    /// With the base among them, which is how a hierarchy is usually written down and is not one of the things
    /// the base turns out to be.
    /// </remarks>
    public IReadOnlyCollection<Kind> GetKinds(Type declaredBase, ISerializer serializer) => declaredBase == typeof(Parcel)
        ? new[]
        {
            new Kind(typeof(Parcel), "parcel"),
            new Kind(typeof(Envelope), "envelope"),
            new Kind(typeof(Pallet), "pallet")
        }
        : Array.Empty<Kind>();

    public IReadOnlyCollection<HierarchyKind> GetDeclarations(ISerializer serializer) => new[]
    {
        new HierarchyKind(typeof(Parcel), typeof(Envelope), "envelope"),
        new HierarchyKind(typeof(Parcel), typeof(Pallet), "pallet")
    };
}
