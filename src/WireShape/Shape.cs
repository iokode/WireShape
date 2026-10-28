using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IOKode.WireShape;

/// <summary>
/// What a type looks like once it has been written, which is not always what the type looks like. A value object
/// is a type here and a string on the wire; a wrapper disappears and leaves what it wrapped in its place.
/// </summary>
/// <remarks>
/// This says what travels, and nothing about how anyone renders it. Whoever writes a schema and whoever writes a
/// client each project it their own way, and neither projection belongs here.
/// </remarks>
public abstract record Shape
{
    private Shape()
    {
    }

    /// <summary>
    /// What it travels whole as, or null when it does not travel whole as a value. It is the one question every
    /// form answers, and the only thing some of what reads a declaration ever asks.
    /// </summary>
    public Carrier? Carrier => this switch
    {
        As travelsAs => travelsAs.By,
        AsText text => WireShape.Carrier.String with
        {
            Format = text.Format,
            IsSelfExplanatory = text.IsSelfExplanatory
        },
        _ => null
    };

    /// <summary>
    /// It travels whole as a value: an identifier that is a GUID, an amount that is a number, a month of a year
    /// that is a text.
    /// </summary>
    /// <param name="By">What it travels as, and what a value of that looks like once written.</param>
    public sealed record As(Carrier By) : Shape;

    /// <summary>
    /// It travels as whatever another type of yours travels as, which is the case of a value written as the
    /// object of the type that carries it.
    /// </summary>
    /// <remarks>
    /// It has no carrier of its own: what travels is whatever that other type turns out to be, which is asked
    /// about it as about anything else. A document describing this points at the schema of that type, and the
    /// name this one was declared under is not in the document at all.
    /// </remarks>
    public sealed record AsAnother(Type Type) : Shape;

    /// <summary>It travels as a text, and these are the texts it travels as.</summary>
    /// <param name="Pattern">
    /// The regular expression one of its values matches: a reference is three capitals, a month of a year is a
    /// year and a month and nothing else. Null when any text will do. Only a text has one, because a pattern is
    /// read of a text and of nothing else.
    /// </param>
    /// <param name="Format">
    /// Which kind of text it is, where there is a settled name for the kind: an email address, a URI. Null when
    /// nothing names it.
    /// </param>
    /// <param name="IsSelfExplanatory">
    /// Whether the language of whoever reads has a kind of its own for such a text already, which is what a
    /// moment in time written as one is: a name of ours over it would hide what the reader would recognize.
    /// </param>
    public sealed record AsText(
        string? Pattern = null,
        string? Format = null,
        bool IsSelfExplanatory = false) : Shape;

    /// <summary>
    /// It travels as the content of a file: the bytes as they stand, under the name and the media type whatever
    /// carries them gives them, and not written inside anything else.
    /// </summary>
    /// <remarks>
    /// What this says is what a value comes out as on this wire, not what the type is. The same bytes travel as
    /// the content of a file on one wire and as a text in base64 on another, and each wire declares its own. It
    /// travels whole as no other type, so it carries none.
    /// </remarks>
    public sealed record AsFile : Shape;

    /// <summary>
    /// It travels as one of several forms, and which one it is is told by the form of what arrived and not by
    /// anything inside it: a text or an object, a number or a text.
    /// </summary>
    /// <remarks>
    /// Not a hierarchy. A hierarchy is several types under a word that travels inside each of them, and whoever
    /// receives one reads that word; this is several forms with nothing to read, and whoever receives one looks
    /// at what came. Where there is a word to read, declare the kinds instead: it is the readable one of the
    /// two, and it is what a client can be written for.
    /// <para>
    /// Whether the alternatives can be told apart from one another is not checked here and cannot be: two texts
    /// whose patterns never overlap are told apart, two objects with different members are told apart, and
    /// deciding that in general would need to intersect patterns and weigh which members are required. A check
    /// that got it wrong would refuse what a reader can read perfectly well, so it is left to whoever declares
    /// it.
    /// </para>
    /// </remarks>
    public sealed record OneOf : Shape
    {
        /// <param name="alternatives">
        /// The forms it comes out as, two or more, each under a name of yours. The form has to be one a
        /// document can write as one of several: a value, a text, or another type of the wire.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Fewer than two alternatives, which is not one of several; two of them under one name, which is two
        /// nobody could tell apart; a name that says nothing; or a form that cannot be written as one of
        /// several — a wrapper that disappears, the content of a file, members declared by hand, or another
        /// union, which is the same union written twice.
        /// </exception>
        public OneOf(IReadOnlyList<Alternative> alternatives)
        {
            if (alternatives.Count < 2)
            {
                throw new ArgumentException(
                    $"One of several is two or more, and this is {alternatives.Count}. A single form is " +
                    "declared as itself.",
                    nameof(alternatives));
            }

            if (alternatives.FirstOrDefault(alternative => string.IsNullOrWhiteSpace(alternative.Name))
                is not null)
            {
                throw new ArgumentException(
                    "An alternative goes under a name of yours, which is what whoever reads it is offered it " +
                    "by, and one of these has none.",
                    nameof(alternatives));
            }

            if (alternatives.GroupBy(alternative => alternative.Name).FirstOrDefault(named => named.Count() > 1)
                is { } twice)
            {
                throw new ArgumentException(
                    $"Two alternatives go under '{twice.Key}', and whoever reads them would be offered the " +
                    "one name for both.",
                    nameof(alternatives));
            }

            if (alternatives.FirstOrDefault(alternative =>
                    alternative.Form is not (As or AsText or AsAnother)) is { } written)
            {
                throw new ArgumentException(
                    $"'{written.Name}' is {written.Form}, which cannot be written as one of several. An " +
                    "alternative travels as a value, as a text, or as another type of the wire.",
                    nameof(alternatives));
            }

            Alternatives = alternatives;
        }

        public IReadOnlyList<Alternative> Alternatives { get; }

        /// <summary>
        /// By the alternatives themselves and not by the list that holds them, so that two parts of a wire
        /// declaring the same union are saying the same thing once and not two different things.
        /// </summary>
        public bool Equals(OneOf? other) => other != null && Alternatives.SequenceEqual(other.Alternatives);

        /// <inheritdoc cref="Equals(OneOf)"/>
        public override int GetHashCode() =>
            Alternatives.Aggregate(0, (hash, alternative) => HashCode.Combine(hash, alternative));
    }

    /// <summary>It disappears, and the type it wraps takes its place.</summary>
    public sealed record Unwraps(int ArgumentIndex) : Shape;

    /// <summary>It travels as an object whose members are these, and not the ones the type declares.</summary>
    public sealed record Members(IReadOnlyList<Member> Value) : Shape
    {
        /// <summary>
        /// By the members themselves and not by the list that holds them, so that two parts of a wire declaring
        /// the same members are saying the same thing once and not two different things.
        /// </summary>
        public bool Equals(Members? other) => other != null && Value.SequenceEqual(other.Value);

        /// <inheritdoc cref="Equals(Members)"/>
        public override int GetHashCode() =>
            Value.Aggregate(0, (hash, member) => HashCode.Combine(hash, member));
    }
}

/// <summary>
/// One member of what travels: the name it travels under, what it carries, whether it may be absent, and whether
/// it is only ever written by whoever answers, which is the case of a member worked out from the others.
/// </summary>
/// <param name="Declared">
/// The member of the type this one came out of, where whoever answered knew it. Null for a member of a
/// declaration, which is written and comes out of nothing.
/// </param>
public sealed record Member(
    string Name,
    Type Type,
    bool IsOptional,
    bool IsAnswerOnly = false,
    MemberInfo? Declared = null);

/// <summary>
/// One of the forms a value comes out as where it comes out as one of several, under a name of yours.
/// </summary>
/// <remarks>
/// The name is not in what travels and is not read from it: nothing in a union says which alternative arrived,
/// and whoever receives one works it out from what came. It is there for whoever offers the alternatives to
/// somebody — the title of the branch in a document, the name of the one way of reading it in a client — because
/// what the two of them have in common is a caller who has to be told which is which, and only you know.
/// </remarks>
/// <param name="Name">What this alternative is called to whoever is offered it.</param>
/// <param name="Form">What it comes out as.</param>
public sealed record Alternative(string Name, Shape Form);
