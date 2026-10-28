using System;

namespace IOKode.WireShape;

/// <summary>
/// Whoever declares, named. Everything declared through one of these is declared by it, and nothing is declared
/// without one.
/// </summary>
/// <remarks>
/// A wire is composed of parts and what one part said has to be tellable from what another said: which plugin,
/// which context, which half of an application. The name is a word of whoever composed it — this library never
/// reads it — and a part handed one of these declares under its own name and under no other.
/// <para>
/// What is refused is refused across all of them. Two parts declaring one type differently is a disagreement
/// worth hearing wherever it comes from, so every check is of the wire being built and not of the one declaring.
/// </para>
/// <para>
/// Only a form keeps the name it was declared under. A kind and a word are read back by what tells them apart
/// and never by who said so, so the name is used here to say which two declarers disagreed and is kept nowhere
/// afterwards.
/// </para>
/// </remarks>
public sealed class Declarer
{
    private readonly WireBuilder declaring;

    internal Declarer(WireBuilder declaring, string name)
    {
        this.declaring = declaring;
        Name = name;
    }

    /// <summary>The name everything declared through this is declared under.</summary>
    public string Name { get; }

    /// <summary>
    /// Declares the written form of a type. One form per type: declaring a second one is refused, because
    /// taking either of the two would make what a value looks like depend on the order the parts of a wire
    /// happened to be composed in. Saying the same form twice is not a second one and is let through.
    /// </summary>
    /// <exception cref="InvalidOperationException">Another form was already declared of this type.</exception>
    public void Add(Type type, Shape shape) => declaring.Declare(Name, type, shape);

    /// <inheritdoc cref="Add(Type,Shape)"/>
    public void Add<T>(Shape shape) => Add(typeof(T), shape);

    /// <summary>
    /// Takes in whatever can be asked the form of a type when nobody declared it one by one. Where two of them
    /// answer about one type the first one added answers and the rest are not asked, because what a source will
    /// answer is not known until it is asked.
    /// </summary>
    public void AddShapeSource(IShapeSource source) => declaring.Declare(Name, source);

    /// <summary>
    /// Declares that a base turns out to be this type, under this word. One fact at a time, because that is
    /// what a hierarchy is made of: the word is of the pair, and nobody holding the base necessarily knows
    /// every type that derives from it.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// That pair was already given another word, or that word already tells another type of the same base
    /// apart. The first cannot be written and the second cannot be read.
    /// </exception>
    public void AddKind(HierarchyKind kind) => declaring.Declare(Name, kind);

    /// <inheritdoc cref="AddKind(HierarchyKind)"/>
    public void AddKind<TBase, TDerived>(string discriminator) where TDerived : TBase =>
        AddKind(new HierarchyKind(typeof(TBase), typeof(TDerived), discriminator));

    /// <summary>
    /// Takes in somewhere the kinds of a base can be read from. Where two sources can answer about one base the
    /// first one added answers and the rest are not asked, as with a source of forms.
    /// </summary>
    public void AddKindSource(IHierarchySource source) => declaring.Declare(source);

    /// <summary>
    /// Declares the word one value of an enumeration travels under. One fact at a time, as a kind is, and for
    /// the same reason: the word is of the value and of nothing else.
    /// </summary>
    /// <remarks>
    /// Whether the enumeration travels as words at all is a form and is declared as one: an enumeration
    /// declared to travel as a number carries the numbers underlying its own members, and there is nothing to
    /// say of each value.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// That value was already given another word, or that word already tells another value of the same
    /// enumeration apart. The first cannot be written and the second cannot be read.
    /// </exception>
    public void AddWord(Type enumType, object value, string word) =>
        declaring.Declare(Name, enumType, value, word);

    /// <inheritdoc cref="AddWord(Type,object,string)"/>
    public void AddWord<TEnum>(TEnum value, string word) where TEnum : struct, Enum =>
        AddWord(typeof(TEnum), value, word);

    /// <summary>
    /// Takes in somewhere the word of a value can be read from, for the wire whose words are a convention and
    /// not a list. Where two sources can answer about one value the first one added answers.
    /// </summary>
    public void AddWordSource(IEnumerationSource source) => declaring.Declare(source);
}
