using System;
using System.Collections.Generic;
using System.Linq;

namespace IOKode.WireShape;

/// <summary>
/// What is declared of a wire before there is a wire: the written form of the types a converter of their own
/// takes over, and what each base turns out to be.
/// </summary>
/// <remarks>
/// Nothing is read from here. A wire answers, and this one only declares, so what a wire says cannot change
/// under whoever is reading it: the declarations are all made, and then <see cref="Build"/> makes the wire that
/// holds them.
/// <para>
/// A type is declared only when a converter of its own decides how it is written. Everything else is answered
/// by whoever writes, so declaring it would be saying twice what is said once.
/// </para>
/// </remarks>
public sealed class WireBuilder
{
    private readonly List<IShapeSource> shapeSources = new();
    private readonly Dictionary<Type, Shape> byType = new();
    private readonly List<(Func<Type, bool> Matches, Func<Type, Shape> Shape)> byRule = new();

    private readonly List<IHierarchySource> kindSources = new();
    private readonly Dictionary<Type, Dictionary<Type, string>> byBase = new();

    private readonly List<IEnumerationSource> wordSources = new();
    private readonly Dictionary<Type, Dictionary<object, string>> byEnumeration = new();

    /// <summary>
    /// Declares the written form of a type. One form per type: declaring a second one is refused, because
    /// taking either of the two would make what a value looks like depend on the order the parts of a wire
    /// happened to be composed in. Saying the same form twice is not a second one and is let through.
    /// </summary>
    /// <exception cref="InvalidOperationException">Another form was already declared of this type.</exception>
    public void Add(Type type, Shape shape)
    {
        if (byType.TryGetValue(type, out var declared) && declared != shape)
        {
            throw new InvalidOperationException(
                $"'{type.FullName}' is already declared to travel as {declared}, and this declares it as {shape}.");
        }

        byType[type] = shape;
    }

    /// <inheritdoc cref="Add(Type,Shape)"/>
    public void Add<T>(Shape shape) => Add(typeof(T), shape);

    /// <summary>
    /// Declares the written form of every type a rule recognises, for the families a single type cannot name,
    /// such as a generic wrapper. Where two rules recognise one type the first one added answers, because
    /// whether two of them overlap is only known of a type that is asked about, never of the rules themselves.
    /// </summary>
    public void Add(Func<Type, bool> matches, Func<Type, Shape> shape) => byRule.Add((matches, shape));

    /// <summary>
    /// Takes in somewhere a form can be read from when nobody declared it one by one. Where two sources can
    /// answer about one type the first one added answers and the rest are not asked, for the same reason a
    /// rule works that way: what a source will answer is not known until it is asked.
    /// </summary>
    public void AddShapeSource(IShapeSource source) => shapeSources.Add(source);

    /// <summary>
    /// Declares that a base turns out to be this type, under this word. One fact at a time, because that is
    /// what a hierarchy is made of: the word is of the pair, and nobody holding the base necessarily knows
    /// every type that derives from it.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// That pair was already given another word, or that word already tells another type of the same base
    /// apart. The first cannot be written and the second cannot be read.
    /// </exception>
    public void AddKind(HierarchyKind kind)
    {
        var kinds = byBase.TryGetValue(kind.Base, out var declared)
            ? declared
            : byBase[kind.Base] = new Dictionary<Type, string>();

        if (kinds.TryGetValue(kind.Type, out var word) && word != kind.Discriminator)
        {
            throw new InvalidOperationException(
                $"'{kind.Type.FullName}' already travels as a '{kind.Base.FullName}' under '{word}', and this " +
                $"would have it travel under '{kind.Discriminator}'.");
        }

        var taken = kinds.FirstOrDefault(other =>
            other.Value == kind.Discriminator && other.Key != kind.Type);

        if (taken.Key != null)
        {
            throw new InvalidOperationException(
                $"'{kind.Discriminator}' already tells '{taken.Key.FullName}' apart from the other kinds of " +
                $"'{kind.Base.FullName}', so it cannot also tell '{kind.Type.FullName}' apart.");
        }

        kinds[kind.Type] = kind.Discriminator;
    }

    /// <inheritdoc cref="AddKind(HierarchyKind)"/>
    public void AddKind<TBase, TDerived>(string discriminator) where TDerived : TBase =>
        AddKind(new HierarchyKind(typeof(TBase), typeof(TDerived), discriminator));

    /// <summary>
    /// Takes in somewhere the kinds of a base can be read from. Where two sources can answer about one base the
    /// first one added answers and the rest are not asked, as with a source of forms.
    /// </summary>
    public void AddKindSource(IHierarchySource source) => kindSources.Add(source);

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
    public void AddWord(Type enumType, object value, string word)
    {
        var words = byEnumeration.TryGetValue(enumType, out var declared)
            ? declared
            : byEnumeration[enumType] = new Dictionary<object, string>();

        if (words.TryGetValue(value, out var travels) && travels != word)
        {
            throw new InvalidOperationException(
                $"'{enumType.FullName}.{value}' already travels under '{travels}', and this would have it " +
                $"travel under '{word}'.");
        }

        var taken = words.FirstOrDefault(other => other.Value == word && !other.Key.Equals(value));

        if (taken.Key != null)
        {
            throw new InvalidOperationException(
                $"'{word}' already tells '{enumType.FullName}.{taken.Key}' apart from the other values, so it " +
                $"cannot also tell '{value}' apart.");
        }

        words[value] = word;
    }

    /// <inheritdoc cref="AddWord(Type,object,string)"/>
    public void AddWord<TEnum>(TEnum value, string word) where TEnum : struct, Enum =>
        AddWord(typeof(TEnum), value, word);

    /// <summary>
    /// Takes in somewhere the word of a value can be read from, for the wire whose words are a convention and
    /// not a list. Where two sources can answer about one value the first one added answers.
    /// </summary>
    public void AddWordSource(IEnumerationSource source) => wordSources.Add(source);

    /// <summary>
    /// The wire these declarations and this writer make: a wire that answers and that nobody can declare on any
    /// more.
    /// </summary>
    /// <remarks>
    /// What is declared is copied, so building twice makes two wires that have nothing to do with each other
    /// and declaring anything afterwards leaves the wires already built as they were. Building the same
    /// declarations with two writers is a wire of each, which is what an application that writes the same types
    /// in two places has.
    /// </remarks>
    /// <param name="serializer">What writes that wire, which is what decides every answer not declared.</param>
    /// <exception cref="InvalidOperationException">
    /// An enumeration was declared to travel as something other than a text and words were declared for its
    /// values. Nothing could write that: either the words travel or the numbers do.
    /// </exception>
    public Wire Build(ISerializer serializer)
    {
        foreach (var (enumeration, words) in byEnumeration)
        {
            if (byType.TryGetValue(enumeration, out var shape) && shape.Carrier?.Kind != ValueKind.Text)
            {
                throw new InvalidOperationException(
                    $"'{enumeration.FullName}' is declared to travel as '{shape.Carrier?.Type.FullName}', and " +
                    $"'{words.First().Value}' is declared as the word one of its values travels under.");
            }
        }

        return new Wire(
            serializer,
            new Dictionary<Type, Shape>(byType),
            byRule.ToArray(),
            shapeSources.ToArray(),
            byBase.ToDictionary(
                hierarchy => hierarchy.Key,
                hierarchy => (IReadOnlyDictionary<Type, string>) new Dictionary<Type, string>(hierarchy.Value)),
            kindSources.ToArray(),
            byEnumeration.ToDictionary(
                enumeration => enumeration.Key,
                enumeration => (IReadOnlyDictionary<object, string>) new Dictionary<object, string>(
                    enumeration.Value)),
            wordSources.ToArray());
    }
}
