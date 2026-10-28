using System.Collections.Generic;
using System.Linq;

namespace IOKode.WireShape;

/// <summary>What a value written whole looks like, which is all a reader of one has to go on.</summary>
/// <remarks>
/// Four and not twenty, because this is about what travels and not about the type it was read from: a
/// <c>float</c>, a <c>double</c> and a <c>decimal</c> are written the same, and which of them it is read back
/// into is what the carrier's own type says.
/// </remarks>
public enum ValueKind
{
    /// <summary>A text, whatever it holds: a word, an identifier, a date.</summary>
    Text,

    /// <summary>A whole number.</summary>
    Integer,

    /// <summary>A number that may have a fractional part.</summary>
    Decimal,

    /// <summary>A truth: true or false.</summary>
    Boolean
}

/// <summary>
/// The type a value travels whole as, and what a value of it looks like once written.
/// </summary>
/// <remarks>
/// What it travels as and what that looks like are two things, and both are needed: whoever reads a value out of
/// a text converts it to the type, and whoever describes the wire writes what it looks like. The ones every
/// writer writes whole are here already, and a wire whose writer writes some other type whole declares one of
/// its own.
/// </remarks>
/// <param name="Type">The type a value travels as, which is what it is read back into.</param>
/// <param name="Kind">What a value of it looks like written.</param>
/// <param name="Format">
/// Which kind of value it is, where there is a settled name for the kind. Null when nothing names it. It is a
/// word of whoever describes the wire and not of whoever writes it: the projection of a document that ships with
/// this writes it as the <c>format</c> of a schema, which is an annotation that means something only for the
/// words its own registry defines — <c>uuid</c>, <c>date-time</c>, <c>int32</c> — and anything else is carried
/// and may be ignored by every reader.
/// </param>
/// <param name="IsSelfExplanatory">
/// Whether the language of whoever reads has a kind of its own for this already, as it has for a date or a moment
/// in time. A name of ours over such a value hides what the reader would otherwise recognize, and a client written
/// from it would wrap a date of its own language in a type of ours.
/// </param>
public sealed record Carrier(
    System.Type Type,
    ValueKind Kind,
    string? Format = null,
    bool IsSelfExplanatory = false)
{
    /// <summary>
    /// Whether a type that travels as this one is worth keeping the name it was declared under. A number is any
    /// number and a text is any text, so with the name gone nothing is left of what the value meant; and one
    /// whose kind the reader has already needs no name of ours.
    /// </summary>
    public bool KeepsItsName => !IsSelfExplanatory;

    /// <summary>A <see cref="string"/>, written as a text.</summary>
    public static Carrier String { get; } = new(typeof(string), ValueKind.Text);

    /// <summary>A <see cref="char"/>, written as a text of one character.</summary>
    public static Carrier Char { get; } = new(typeof(char), ValueKind.Text);

    /// <summary>A <see cref="System.Guid"/>, written as a text under the format <c>uuid</c>.</summary>
    public static Carrier Guid { get; } = new(typeof(System.Guid), ValueKind.Text, "uuid");

    /// <summary>A <see cref="bool"/>, written as a truth.</summary>
    public static Carrier Boolean { get; } = new(typeof(bool), ValueKind.Boolean);

    /// <summary>A <see cref="byte"/>, written as a whole number under the format <c>int32</c>.</summary>
    public static Carrier Byte { get; } = new(typeof(byte), ValueKind.Integer, "int32");

    /// <summary>An <see cref="sbyte"/>, written as a whole number under the format <c>int32</c>.</summary>
    public static Carrier SByte { get; } = new(typeof(sbyte), ValueKind.Integer, "int32");

    /// <summary>A <see cref="short"/>, written as a whole number under the format <c>int32</c>.</summary>
    public static Carrier Int16 { get; } = new(typeof(short), ValueKind.Integer, "int32");

    /// <summary>A <see cref="ushort"/>, written as a whole number under the format <c>int32</c>.</summary>
    public static Carrier UInt16 { get; } = new(typeof(ushort), ValueKind.Integer, "int32");

    /// <summary>An <see cref="int"/>, written as a whole number under the format <c>int32</c>.</summary>
    public static Carrier Int32 { get; } = new(typeof(int), ValueKind.Integer, "int32");

    /// <summary>
    /// A <see cref="uint"/>, written as a whole number under the format <c>int64</c>, which is the smallest that holds
    /// it.
    /// </summary>
    public static Carrier UInt32 { get; } = new(typeof(uint), ValueKind.Integer, "int64");

    /// <summary>A <see cref="long"/>, written as a whole number under the format <c>int64</c>.</summary>
    public static Carrier Int64 { get; } = new(typeof(long), ValueKind.Integer, "int64");

    /// <summary>
    /// Without a format, there being none for a whole number that does not fit in a signed eight bytes.
    /// </summary>
    public static Carrier UInt64 { get; } = new(typeof(ulong), ValueKind.Integer);

    /// <summary>A <see cref="float"/>, written as a number under the format <c>float</c>.</summary>
    public static Carrier Single { get; } = new(typeof(float), ValueKind.Decimal, "float");

    /// <summary>A <see cref="double"/>, written as a number under the format <c>double</c>.</summary>
    public static Carrier Double { get; } = new(typeof(double), ValueKind.Decimal, "double");

    /// <summary>A <see cref="decimal"/>, written as a number under the format <c>decimal</c>.</summary>
    public static Carrier Decimal { get; } = new(typeof(decimal), ValueKind.Decimal, "decimal");

    /// <summary>
    /// A <see cref="System.DateOnly"/>, written as a text under the format <c>date</c>, and self-explanatory.
    /// </summary>
    public static Carrier DateOnly { get; } =
        new(typeof(System.DateOnly), ValueKind.Text, "date", IsSelfExplanatory: true);

    /// <summary>
    /// A <see cref="System.TimeOnly"/>, written as a text under the format <c>time</c>, and self-explanatory.
    /// </summary>
    public static Carrier TimeOnly { get; } =
        new(typeof(System.TimeOnly), ValueKind.Text, "time", IsSelfExplanatory: true);

    /// <summary>
    /// A <see cref="System.DateTime"/>, written as a text under the format <c>date-time</c>, and self-explanatory.
    /// </summary>
    public static Carrier DateTime { get; } =
        new(typeof(System.DateTime), ValueKind.Text, "date-time", IsSelfExplanatory: true);

    /// <summary>
    /// A <see cref="System.DateTimeOffset"/>, written as a text under the format <c>date-time</c>, and self-
    /// explanatory.
    /// </summary>
    public static Carrier DateTimeOffset { get; } =
        new(typeof(System.DateTimeOffset), ValueKind.Text, "date-time", IsSelfExplanatory: true);

    /// <summary>
    /// A <see cref="System.TimeSpan"/>, written as a text under the format <c>duration</c>, and self-explanatory.
    /// </summary>
    public static Carrier TimeSpan { get; } =
        new(typeof(System.TimeSpan), ValueKind.Text, "duration", IsSelfExplanatory: true);

    /// <summary>
    /// The carriers every writer writes whole, which are the ones a wire needs nothing declared about.
    /// </summary>
    public static IReadOnlyCollection<Carrier> Known { get; } = new[]
    {
        String, Char, Guid, Boolean,
        Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64,
        Single, Double, Decimal,
        DateOnly, TimeOnly, DateTime, DateTimeOffset, TimeSpan
    };

    private static readonly Dictionary<System.Type, Carrier> known =
        Known.ToDictionary(carrier => carrier.Type);

    /// <summary>
    /// The carrier of a type every writer writes whole, or null for a type that carries something of its own
    /// inside and so is described rather than written as a value.
    /// </summary>
    /// <remarks>
    /// This is what reads a declaration made of a type rather than of a carrier: a source that reads the
    /// converters of a wire has the type one of them writes and nothing else, and asking here is what keeps it
    /// from having to know what a value looks like written.
    /// </remarks>
    public static Carrier? Of(System.Type type) => known.GetValueOrDefault(type);
}
