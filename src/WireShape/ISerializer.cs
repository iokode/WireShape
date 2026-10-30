using System;
using System.Collections.Generic;

namespace IOKode.WireShape;

/// <summary>
/// The serializer of one wire, asked instead of used: what it writes a type as, and what it reads back.
/// </summary>
/// <remarks>
/// A form is declared, and everything a form does not cover is decided by whoever writes. Which name a member
/// travels under, which word a value of an enumeration comes out as, what the text of a value turns back into —
/// none of that is in a declaration, and the only answer that cannot disagree with what travels is the one given
/// by the thing that writes it. So it is asked, and this is the asking.
/// <para>
/// Implement it over whatever writes your wire. Nothing here supposes a format: an answer of "I cannot say" is
/// an answer, and a wire whose writer knows less is a wire whose types are declared instead.
/// </para>
/// </remarks>
public interface ISerializer
{
    /// <summary>
    /// The members a type travels with, named as they travel, or null where nothing is known of what it
    /// carries. Null is not "it carries nothing": whoever reads it has to say that it could not be told.
    /// </summary>
    IReadOnlyList<Member>? GetMembers(Type type);

    /// <summary>
    /// What a type turns out to be on this wire, each with the word that tells it apart, or null where nothing
    /// is known of it. Empty is an answer too: this one is never told apart from anything.
    /// </summary>
    /// <remarks>
    /// Null and empty are not the same, for the same reason they are not the same of the members: a hierarchy
    /// written by a converter of its own cannot be answered about, and reading that silence as "it is never
    /// told apart" would describe the base as a plain object and say nothing of what really travels.
    /// </remarks>
    IReadOnlyCollection<Kind>? GetKinds(Type type);

    /// <summary>
    /// What one value of an enumeration comes out as, or null where nothing is known of it. A writer is free to
    /// put a value under a word of its own and free to write it as a number of its own, and nothing but writing
    /// it says which of the two it is or what it says.
    /// </summary>
    EnumerationValueForm? GetValueForm(Type enumType, object value);

    /// <summary>
    /// The text a value is written as: what a route, a query or anything else that carries text carries of it,
    /// which is the text it has wherever else it is written.
    /// </summary>
    /// <remarks>
    /// Written, and not worked out, because the text is what the converter of the wire makes of the value and
    /// nothing else can say it. It is the text and not what the format wraps it in: a word without the quotes
    /// or the escapes it is written inside.
    /// <para>
    /// There is no answer of "I cannot say" here, unlike of the members of a type. Writing a value is what the
    /// writer does with every one it is handed, and a writer that could not would not be the writer of the
    /// wire. A value it cannot write is refused as it would be anywhere else.
    /// </para>
    /// </remarks>
    /// <param name="type">The type the value is written as.</param>
    /// <param name="value">The value.</param>
    string WriteAsText(Type type, object value);

    /// <summary>A value back, from the text it is written as.</summary>
    /// <remarks>
    /// The text alone does not say what it was written as: <c>125</c> is a text for one type and a number for
    /// another, and only the first is written inside quotes. So whoever reads says which, from what the type is
    /// declared to travel as. A text that is not a value of that kind, or not one of the type, is refused, and
    /// never read as something else the format could make of it.
    /// </remarks>
    /// <param name="type">The type to read the value as.</param>
    /// <param name="kind">What the text was written as.</param>
    /// <param name="text">The text, as <see cref="WriteAsText"/> gives it.</param>
    object? ReadFromText(Type type, ValueKind kind, string text);
}
