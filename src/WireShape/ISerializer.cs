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

    /// <summary>The value a type is declared to travel as, from a value of that type.</summary>
    /// <param name="valueType">The type as it was declared.</param>
    /// <param name="carrier">The type it travels as.</param>
    object? WriteAsCarrier(Type valueType, Type carrier, object? value);

    /// <summary>The declared value back, from the value it traveled as.</summary>
    /// <param name="valueType">The type as it was declared.</param>
    /// <param name="carrier">The type it travels as.</param>
    object? ReadFromCarrier(Type valueType, Type carrier, object? carried);
}
