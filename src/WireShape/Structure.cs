using System;
using System.Collections.Generic;

namespace IOKode.WireShape;

/// <summary>What a type comes out as, as data.</summary>
/// <remarks>
/// The answer of the wire about one type, worked out already: whoever reads this asks nothing of a wire or of
/// whoever writes one, because whoever built it asked for them. What is here is what travels, and nothing about
/// how it was declared: the same structure comes out of a form declared by hand and of one nobody declared at
/// all.
/// </remarks>
public abstract record Structure
{
    private Structure()
    {
    }

    /// <summary>
    /// Whether the meaning of such a value lives nowhere but in this type, so that whoever writes has to give it
    /// a place of its own — a schema of a document, a type of a client — instead of writing what it comes out as.
    /// </summary>
    /// <remarks>
    /// Which name that place goes under is not said here and is decided by nothing in this library: a document
    /// names its schemas and a client names its types, and one type may go by a different name in each. What is
    /// said here is that there has to be one. A date needs none, because whoever reads a document knows a date
    /// where it says so and a name of ours over it would hide that; a text needs one, because a text is any text
    /// and with the name gone nothing is left of what the value meant.
    /// </remarks>
    public abstract bool NeedsItsOwnName { get; init; }

    /// <summary>
    /// It comes out as another type, and that other type is what travels: an identifier that is a GUID, a code
    /// that is a text, a wrapper that disappears and leaves what it wrapped.
    /// </summary>
    /// <remarks>
    /// A wrapper is one of these and not a case of its own. How a type came to come out as another — a carrier
    /// named in the declaration, or a wrapper leaving one of its own arguments behind — is a fact of the
    /// declaring, and what travels is the same either way: that other type, in its place.
    /// </remarks>
    /// <param name="By">The type that travels where this one was.</param>
    /// <param name="NeedsItsOwnName">
    /// Whether the meaning of the value lives nowhere but in this type, which is the case of a text, a number or a
    /// GUID that keeps its name. A self-explanatory carrier, another type of the wire and a wrapper that
    /// disappeared need none.
    /// </param>
    public sealed record Carried(Type By, bool NeedsItsOwnName) : Structure;

    /// <summary>It travels as an object with these members, and as one of these kinds where it has any.</summary>
    /// <param name="Members">What it carries, named as it travels.</param>
    /// <param name="Base">
    /// The type it travels under, which is the nearest one above it that travels as one of several. Null when it
    /// travels under no other.
    /// </param>
    /// <param name="Kinds">What it can turn out to be, each with the word that tells it apart.</param>
    public sealed record Object(
        IReadOnlyList<Member> Members,
        Type? Base,
        IReadOnlyList<Kind> Kinds) : Structure
    {
        /// <summary>Its members live nowhere else, so there has to be somewhere to write them.</summary>
        public override bool NeedsItsOwnName { get; init; } = true;
    }

    /// <summary>It travels as one of a set of values, by the word of each or by the number of each.</summary>
    public sealed record Enumeration(bool ByName, IReadOnlyList<EnumerationValue> Values) : Structure
    {
        /// <summary>Which values are admitted lives nowhere else, as the members of an object do.</summary>
        public override bool NeedsItsOwnName { get; init; } = true;
    }

    /// <summary>
    /// It comes out as one of several forms, and which one is told by what arrived and not by anything inside
    /// it.
    /// </summary>
    /// <remarks>
    /// The types and not what each of them comes out as, because an alternative is a form and not a type that
    /// was declared: it has no name of its own to keep, and asking what one of these types comes out as is the
    /// same as asking it of the type of a member — it is answered here where there is an answer to give, and
    /// where there is none it is one of those whoever writes writes as it stands.
    /// <para>
    /// Two alternatives that travel as the same type cannot be told apart here, which is the one thing this
    /// loses against the forms it was read from. It loses nothing a client could have written: two branches of
    /// one type are two branches it cannot declare either.
    /// </para>
    /// </remarks>
    public sealed record OneOf(IReadOnlyList<AlternativeType> Alternatives) : Structure
    {
        /// <summary>
        /// That these alternatives are the same value lives nowhere but in this type: with the type gone, what
        /// is left is several forms with nothing saying they are one field.
        /// </summary>
        public override bool NeedsItsOwnName { get; init; } = true;
    }

    /// <summary>
    /// It comes out as the content of a file, which travels as it stands and not written inside anything else.
    /// </summary>
    public sealed record File : Structure
    {
        /// <summary>
        /// The meaning is in what travels — content, under a name and a media type — and a name of ours over it
        /// would add nothing to it.
        /// </summary>
        public override bool NeedsItsOwnName { get; init; } = false;
    }
}

/// <summary>One of the types a type can turn out to be, with the word that tells it apart.</summary>
public sealed record Kind(Type Type, string Discriminator);

/// <summary>One of the types a base can turn out to be, with the base and the word that tells it apart.</summary>
public sealed record HierarchyKind(Type Base, Type Type, string Discriminator);

/// <summary>
/// One value of an enumeration: the name it carries in the code, the number underlying it, and the text it
/// travels as, which is a word or a number as the enumeration says.
/// </summary>
/// <remarks>
/// What underlies it and what it travels as are two things. Whoever declares this enumeration again needs the
/// first, so that a value keeps the number it had; whoever writes a value needs the second, and a converter is
/// free to make them differ.
/// </remarks>
public sealed record EnumerationValue(string Name, string Underlying, string Written);

/// <summary>
/// One of the forms a value comes out as where it comes out as one of several: the name it is offered under and
/// the type it travels as.
/// </summary>
/// <remarks>
/// The type and not what that type comes out as, because that is a row of its own wherever there is an answer to
/// give for it, and where there is none it is one of the things whoever writes writes as they stand. It is the
/// same record as the declared alternative in all but that, and it is a second one because what travels is a
/// type and what was declared is a form.
/// </remarks>
/// <param name="Name">What this alternative is called to whoever is offered it.</param>
/// <param name="Type">The type it travels as.</param>
public sealed record AlternativeType(string Name, Type Type);
