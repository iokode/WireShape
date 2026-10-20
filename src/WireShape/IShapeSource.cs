using System;
using System.Collections.Generic;

namespace IOKode.WireShape;

/// <summary>
/// Somewhere a written form can be read from without anyone having declared it one by one.
/// </summary>
/// <remarks>
/// Some forms are already said by something else: a converter written as one type to another says it in its own
/// declaration, and a type told apart from others by a discriminator says it by being told apart. A source reads
/// them from there, and they need no line of their own anywhere.
/// <para>
/// What knows how to read them is not the wire. A wire is where a form is kept, not where it is worked out, and
/// this is the seam between the two: implement it to turn a convention of your own — a base class, an
/// attribute, a converter written to a shape — into declarations nobody had to write down.
/// </para>
/// </remarks>
public interface IShapeSource
{
    /// <summary>The form of this type, or null where this source has nothing to say about it.</summary>
    Shape? GetShape(Type type, ISerializer serializer);

    /// <summary>
    /// Every type this source can name, with its form. Empty where a source answers about a type when it is
    /// asked and cannot be asked to name them all.
    /// </summary>
    IReadOnlyDictionary<Type, Shape> GetDeclarations(ISerializer serializer);
}
