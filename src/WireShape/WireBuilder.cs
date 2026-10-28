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
/// <para>
/// Nothing is declared here directly. A wire is composed of parts, and each part declares through a
/// <see cref="Declarer"/> of its own so that what it said is tellable from what the others said.
/// </para>
/// </remarks>
public sealed class WireBuilder
{
    private readonly List<(string Declarer, IShapeSource Source)> shapeSources = new();
    private readonly Dictionary<Type, Declaration> byType = new();

    private readonly List<IHierarchySource> kindSources = new();
    private readonly Dictionary<Type, Dictionary<Type, (string Discriminator, string Declarer)>> byBase = new();

    private readonly List<IEnumerationSource> wordSources = new();
    private readonly Dictionary<Type, Dictionary<object, (string Word, string Declarer)>> byEnumeration = new();

    /// <summary>
    /// Whoever declares, under this name: everything declared through what this gives back is declared by it.
    /// </summary>
    /// <remarks>
    /// Asking twice under one name is the same declarer twice and is no different from keeping the first: the
    /// name is what a declaration is attributed to, and two of them under one name are indistinguishable by
    /// design.
    /// </remarks>
    /// <param name="name">
    /// What this part of the wire is called, which is read by nothing here and is what tells one part's
    /// declarations from another's.
    /// </param>
    public Declarer GetDeclarer(string name) => new(this, name);

    internal void Declare(string declarer, Type type, Shape shape)
    {
        if (byType.TryGetValue(type, out var declared))
        {
            if (declared.Shape != shape)
            {
                throw new InvalidOperationException(
                    $"'{type.FullName}' is already declared by '{declared.Declarer}' to travel as " +
                    $"{declared.Shape}, and '{declarer}' declares it as {shape}.");
            }

            // The one who said it keeps it. Saying the same form again is agreeing with it, and whoever agrees
            // did not declare it: taking the name of the last to speak would make it depend on the order the
            // parts of a wire happened to be composed in.
            return;
        }

        byType[type] = new Declaration(shape, declarer);
    }

    internal void Declare(string declarer, IShapeSource source) => shapeSources.Add((declarer, source));

    internal void Declare(string declarer, HierarchyKind kind)
    {
        var kinds = byBase.TryGetValue(kind.Base, out var declared)
            ? declared
            : byBase[kind.Base] = new Dictionary<Type, (string, string)>();

        if (kinds.TryGetValue(kind.Type, out var travels) && travels.Discriminator != kind.Discriminator)
        {
            throw new InvalidOperationException(
                $"'{kind.Type.FullName}' already travels as a '{kind.Base.FullName}' under " +
                $"'{travels.Discriminator}', declared by '{travels.Declarer}', and '{declarer}' would have it " +
                $"travel under '{kind.Discriminator}'.");
        }

        var taken = kinds.FirstOrDefault(other =>
            other.Value.Discriminator == kind.Discriminator && other.Key != kind.Type);

        if (taken.Key != null)
        {
            throw new InvalidOperationException(
                $"'{kind.Discriminator}' already tells '{taken.Key.FullName}' apart from the other kinds of " +
                $"'{kind.Base.FullName}', declared by '{taken.Value.Declarer}', so '{declarer}' cannot also " +
                $"tell '{kind.Type.FullName}' apart by it.");
        }

        kinds[kind.Type] = (kind.Discriminator, declarer);
    }

    internal void Declare(IHierarchySource source) => kindSources.Add(source);

    internal void Declare(string declarer, Type enumType, object value, string word)
    {
        var words = byEnumeration.TryGetValue(enumType, out var declared)
            ? declared
            : byEnumeration[enumType] = new Dictionary<object, (string, string)>();

        if (words.TryGetValue(value, out var travels) && travels.Word != word)
        {
            throw new InvalidOperationException(
                $"'{enumType.FullName}.{value}' already travels under '{travels.Word}', declared by " +
                $"'{travels.Declarer}', and '{declarer}' would have it travel under '{word}'.");
        }

        var taken = words.FirstOrDefault(other => other.Value.Word == word && !other.Key.Equals(value));

        if (taken.Key != null)
        {
            throw new InvalidOperationException(
                $"'{word}' already tells '{enumType.FullName}.{taken.Key}' apart from the other values, " +
                $"declared by '{taken.Value.Declarer}', so '{declarer}' cannot also tell '{value}' apart by it.");
        }

        words[value] = (word, declarer);
    }

    internal void Declare(IEnumerationSource source) => wordSources.Add(source);

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
            if (byType.TryGetValue(enumeration, out var declared) &&
                declared.Shape.Carrier?.Kind != ValueKind.Text)
            {
                throw new InvalidOperationException(
                    $"'{enumeration.FullName}' is declared to travel as " +
                    $"'{declared.Shape.Carrier?.Type.FullName}', and '{words.First().Value.Word}' is declared " +
                    "as the word one of its values travels under.");
            }
        }

        return new Wire(
            serializer,
            new Dictionary<Type, Declaration>(byType),
            shapeSources.ToArray(),
            byBase.ToDictionary(
                hierarchy => hierarchy.Key,
                hierarchy => (IReadOnlyDictionary<Type, string>) hierarchy.Value.ToDictionary(
                    kind => kind.Key,
                    kind => kind.Value.Discriminator)),
            kindSources.ToArray(),
            byEnumeration.ToDictionary(
                enumeration => enumeration.Key,
                enumeration => (IReadOnlyDictionary<object, string>) enumeration.Value.ToDictionary(
                    value => value.Key,
                    value => value.Value.Word)),
            wordSources.ToArray());
    }
}
