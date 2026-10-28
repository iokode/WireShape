using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using IOKode.WireShape.SystemTextJson;

namespace IOKode.WireShape.Documentation.Tests;

/// <summary>
/// The booking API of an airline, which is the example every chapter of the documentation is written on. What is
/// here is what those chapters show, so that nothing they show is written by hand and left unread.
/// </summary>
/// <remarks>
/// One of every hard case: a value whose name is all that is left of it, one whose kind the reader has already,
/// a value that travels as another type, a hierarchy, an enumeration by word and one by number, a wrapper that
/// disappears, the content of a file, and a value that comes out as one of several forms.
/// </remarks>
public static class Flights
{
    /// <summary>The whole wire of the example, declared as a chapter shows it.</summary>
    public static WireBuilder Declare()
    {
        var declaring = new WireBuilder();
        var bookings = declaring.GetDeclarer("bookings");

        // Values a converter of their own writes, which is why they are declared at all.
        bookings.Add<BookingReference>(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
        bookings.Add<PassengerId>(new Shape.As(Carrier.Guid));
        bookings.Add<FlightDuration>(new Shape.As(Carrier.Int64));
        bookings.Add<DepartsOn>(new Shape.As(Carrier.DateOnly));

        // A moment in time of its own: the reader's language has a kind for it, so no name of ours goes over it.
        bookings.Add<BoardingAt>(
            new Shape.AsText(Format: "date-time", IsSelfExplanatory: true));

        // It travels as another type of the wire, so the document points at that type and the name Money is not
        // in it at all.
        bookings.Add<Money>(new Shape.AsAnother(typeof(MoneyParts)));

        // A family a convention recognizes, which cannot be listed one by one.
        bookings.AddShapeSource(new AmendedWireShapes());

        // The content of a file, which in an application is whatever its framework calls one.
        bookings.Add<BoardingPassFile>(new Shape.AsFile());

        // One of several forms, told apart by what arrived: the identifier, or the passenger expanded.
        bookings.Add<PassengerRef>(new Shape.OneOf(new[]
        {
            new Alternative("id", new Shape.As(Carrier.Guid)),
            new Alternative("expanded", new Shape.AsAnother(typeof(Passenger)))
        }));

        // What a segment turns out to be, one fact at a time.
        bookings.AddKind<Segment, FlightSegment>("flight");
        bookings.AddKind<Segment, TrainSegment>("train");

        // The word a value of an enumeration travels under, one fact at a time for the same reason.
        bookings.AddWord(CabinClass.Economy, "economy");
        bookings.AddWord(CabinClass.Business, "business");

        return declaring;
    }

    /// <summary>What writes this wire, which is what answers everything no declaration says.</summary>
    public static ISerializer Writer()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        options.Converters.Add(new BookingReferenceJsonConverter());
        options.Converters.Add(new PassengerIdJsonConverter());
        options.Converters.Add(new FlightDurationJsonConverter());
        options.Converters.Add(new DepartsOnJsonConverter());
        options.Converters.Add(new BoardingAtJsonConverter());
        options.Converters.Add(new MoneyJsonConverter());

        return new JsonSerializerOptionsSerializer(options);
    }

    public static Wire Build() => Declare().Build(Writer());
}

public readonly record struct BookingReference(string Value);

public readonly record struct PassengerId(Guid Value);

/// <summary>How long a flight is, which travels as its minutes.</summary>
public readonly record struct FlightDuration(long Minutes);

public readonly record struct DepartsOn(DateOnly Day);

/// <summary>When boarding opens, which travels as the moment it is.</summary>
public readonly record struct BoardingAt(DateTimeOffset Moment);

/// <summary>An amount and the currency it is in, which are one thing and travel as one object.</summary>
public sealed record Money(decimal Amount, string Currency);

/// <summary>What a <see cref="Money"/> travels as.</summary>
public sealed record MoneyParts(decimal Amount, string Currency);

/// <summary>A value that may be left out of a change, and travels as the value when it is not.</summary>
public sealed record Amended<T>(T Value);

/// <summary>
/// What a value left out of a change travels as, whatever type it wraps: the wrapper disappears and what it
/// wrapped travels in its place.
/// </summary>
/// <remarks>
/// A convention and not a catalog. Which types are amended is open — any type the API lets a caller leave out —
/// so there is no list of them to be read through.
/// </remarks>
public sealed class AmendedWireShapes : IShapeSource
{
    public Shape? GetShape(Type type, ISerializer serializer) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Amended<>)
            ? new Shape.Unwraps(0)
            : null;
}

/// <summary>A boarding pass as it is sent and as it comes back, which an application reads as a file.</summary>
public sealed class BoardingPassFile;

public sealed record Passenger(PassengerId Id, string Name, DateOnly BornOn);

/// <summary>Who a seat is for: the identifier, or the passenger expanded.</summary>
public readonly record struct PassengerRef(Guid Value);

public abstract record Segment(DepartsOn Departs, FlightDuration Takes);

public sealed record FlightSegment(DepartsOn Departs, FlightDuration Takes, string Number) : Segment(Departs, Takes);

public sealed record TrainSegment(DepartsOn Departs, FlightDuration Takes, string Operator)
    : Segment(Departs, Takes);

public sealed record Booking(
    BookingReference Reference,
    PassengerRef Passenger,
    IReadOnlyList<Segment> Segments,
    Money Total,
    CabinClass Cabin,
    BoardingAt? Boarding);

public enum CabinClass
{
    Economy = 1,
    Business = 2
}

/// <summary>Why a flight was disrupted, under the codes the carriers settled on long ago.</summary>
public enum DisruptionReason
{
    Weather = 71,
    Strike = 72
}

public sealed class BookingReferenceJsonConverter : JsonConverter<BookingReference>
{
    public override BookingReference Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        new(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, BookingReference value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}

public sealed class PassengerIdJsonConverter : JsonConverter<PassengerId>
{
    public override PassengerId Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        new(reader.GetGuid());

    public override void Write(Utf8JsonWriter writer, PassengerId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}

public sealed class FlightDurationJsonConverter : JsonConverter<FlightDuration>
{
    public override FlightDuration Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        new(reader.GetInt64());

    public override void Write(Utf8JsonWriter writer, FlightDuration value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Minutes);
}

public sealed class DepartsOnJsonConverter : JsonConverter<DepartsOn>
{
    public override DepartsOn Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        new(DateOnly.Parse(reader.GetString()!));

    public override void Write(Utf8JsonWriter writer, DepartsOn value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Day.ToString("yyyy-MM-dd"));
}

public sealed class BoardingAtJsonConverter : JsonConverter<BoardingAt>
{
    public override BoardingAt Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        new(DateTimeOffset.Parse(reader.GetString()!));

    public override void Write(Utf8JsonWriter writer, BoardingAt value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Moment.ToString("O"));
}

public sealed class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var parts = JsonSerializer.Deserialize<MoneyParts>(ref reader, options)!;

        return new Money(parts.Amount, parts.Currency);
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, new MoneyParts(value.Amount, value.Currency), options);
}
