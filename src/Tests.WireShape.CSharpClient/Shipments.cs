using System;
using System.Collections.Generic;
using IOKode.WireShape.AspNetCore;

namespace IOKode.WireShape.CSharpClient.Tests;

/// <summary>
/// What a parcel service carries in its calls, and the model of it that a client is written from.
/// </summary>
public readonly record struct TrackingCode(string Value);

public readonly record struct DispatchedOn(DateOnly Day);

public sealed record Consignment(TrackingCode Code, string? Instructions);

public abstract record Parcel;

public sealed record Envelope : Parcel;

public sealed record Pallet : Parcel;

public sealed record PhoneParts(string Country, string Number);

/// <summary>What a consignee is reached on, which travels as a text or as its parts.</summary>
public readonly record struct Phone(string Value);

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

/// <summary>A file a call carries, which stands here for what a framework calls one.</summary>
public sealed class Attachment;

/// <summary>Another corner of the application, where a type happens to go by a name already taken.</summary>
public static class Other
{
    public sealed record Consignment;
}

/// <summary>The model these tests are written from, built by hand as whoever read a wire would hand it over.</summary>
public static class Shipments
{
    public static IReadOnlyDictionary<Type, Structure> Types { get; } = new Dictionary<Type, Structure>
    {
        [typeof(TrackingCode)] = new Structure.Carried(typeof(string), NeedsItsOwnName: true),
        [typeof(DispatchedOn)] = new Structure.Carried(typeof(DateOnly), NeedsItsOwnName: false),
        [typeof(Attachment)] = new Structure.File(),

        [typeof(Consignment)] = new Structure.Object(
            new[]
            {
                new Member("code", typeof(TrackingCode), IsOptional: false),
                new Member("instructions", typeof(string), IsOptional: true),
                new Member("dispatched", typeof(DispatchedOn), IsOptional: false, IsAnswerOnly: true)
            },
            null,
            Array.Empty<Kind>()),

        [typeof(Parcel)] = new Structure.Object(
            new[] {new Member("weight", typeof(decimal), IsOptional: false)},
            null,
            new[] {new Kind(typeof(Envelope), "envelope"), new Kind(typeof(Pallet), "pallet")}),

        [typeof(Envelope)] = new Structure.Object(
            new[] {new Member("weight", typeof(decimal), IsOptional: false)},
            typeof(Parcel),
            Array.Empty<Kind>()),

        [typeof(Pallet)] = new Structure.Object(
            new[]
            {
                new Member("weight", typeof(decimal), IsOptional: false),
                new Member("boxes", typeof(int), IsOptional: false)
            },
            typeof(Parcel),
            Array.Empty<Kind>()),

        [typeof(PhoneParts)] = new Structure.Object(
            new[]
            {
                new Member("country", typeof(string), IsOptional: false),
                new Member("number", typeof(string), IsOptional: false)
            },
            null,
            Array.Empty<Kind>()),

        // One of every way a value can arrive, which is all a reading has to go on, and at most one branch of
        // each: a text, a number, a truth, an object and a list.
        [typeof(Phone)] = new Structure.OneOf(new[]
        {
            new AlternativeType("national", typeof(string)),
            new AlternativeType("extension", typeof(int)),
            new AlternativeType("withheld", typeof(bool)),
            new AlternativeType("international", typeof(PhoneParts)),
            new AlternativeType("several", typeof(string[]))
        }),

        [typeof(CarrierService)] = new Structure.Enumeration(ByName: true, new[]
        {
            new EnumerationValue("NextDay", "1", "next-day"),
            new EnumerationValue("Economy", "2", "Economy")
        }),

        [typeof(ParcelSize)] = new Structure.Enumeration(ByName: false, new[]
        {
            new EnumerationValue("Small", "1", "1"),
            new EnumerationValue("Large", "2", "2")
        })
    };

    public static ApiModel Of(params ApiAction[] actions) =>
        new(actions, Types, Array.Empty<HierarchyKind>(), "kind");

    /// <summary>What a client is called, as a project that names its controllers with a suffix says it.</summary>
    public static string NameOf(string controller) =>
        controller.Replace("ResourceController", string.Empty).Replace("Controller", string.Empty) + "Client";
}
