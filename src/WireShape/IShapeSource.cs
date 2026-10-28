using System;

namespace IOKode.WireShape;

/// <summary>
/// Whatever can be asked the written form of a type: a convention that recognizes one, or a place that keeps
/// the forms somebody already wrote down.
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
/// <para>
/// One question and not two. A convention recognizes a type when it is put in front of it and has no list
/// behind it: which <c>Amended&lt;T&gt;</c> exist is not a question anyone can answer, because the argument is
/// open. A source that does have a list is an <see cref="IShapeCatalog"/> and says so by being one.
/// </para>
/// </remarks>
public interface IShapeSource
{
    /// <summary>The form of this type, or null where this source has nothing to say about it.</summary>
    Shape? GetShape(Type type, ISerializer serializer);
}
