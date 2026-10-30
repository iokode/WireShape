using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IOKode.WireShape;

/// <summary>
/// One wire, asked: what each of its types looks like once written, and everything a declaration does not say.
/// </summary>
/// <remarks>
/// One of these per wire, because the same type is written differently in each: an API answers with what writes
/// that API, a column keeps what the column holds, and a call to somebody else's API is written as that one
/// asks. Nothing here makes two wires agree.
/// <para>
/// Two things answer together. Whoever writes answers for everything it writes itself — it already knows the
/// name every member travels under — and the declarations answer for the types a converter of their own took
/// over, which whoever writes can say nothing about. Whoever asks does not need to know which of the two
/// answered.
/// </para>
/// <para>
/// A wire is made by a <see cref="WireBuilder"/> and cannot be declared on, so what it says of a type cannot
/// change under whoever is reading it. What it holds is its own, and what it delegates is not: a source belongs
/// to whoever wrote it and whoever writes the wire reads whatever it is reading now, so building a wire is not
/// taking a photograph of the world. It is the declarations that are settled.
/// </para>
/// </remarks>
public sealed class Wire
{
    private readonly ISerializer serializer;
    private readonly IReadOnlyDictionary<Type, Declaration> byType;
    private readonly IReadOnlyList<(string Declarer, IShapeSource Source)> shapeSources;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, string>> byBase;
    private readonly IReadOnlyList<IHierarchySource> kindSources;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<object, string>> byEnumeration;
    private readonly IReadOnlyList<IEnumerationSource> wordSources;

    private readonly IReadOnlyDictionary<Type, Declaration> declaredShapes;
    private readonly IReadOnlyCollection<HierarchyKind> declaredKinds;

    internal Wire(
        ISerializer serializer,
        IReadOnlyDictionary<Type, Declaration> byType,
        IReadOnlyList<(string Declarer, IShapeSource Source)> shapeSources,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, string>> byBase,
        IReadOnlyList<IHierarchySource> kindSources,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<object, string>> byEnumeration,
        IReadOnlyList<IEnumerationSource> wordSources)
    {
        this.serializer = serializer;
        this.byType = byType;
        this.shapeSources = shapeSources;
        this.byBase = byBase;
        this.kindSources = kindSources;
        this.byEnumeration = byEnumeration;
        this.wordSources = wordSources;

        declaredShapes = NameEveryShape();
        declaredKinds = NameEveryKind();
    }

    /// <summary>
    /// The written form of a type, or null when the type is written as it stands and whoever writes it
    /// describes it already.
    /// </summary>
    public Shape? GetShape(Type type)
    {
        if (byType.TryGetValue(type, out var declared))
        {
            return declared.Shape;
        }

        return shapeSources
            .Select(source => source.Source.GetShape(type, serializer))
            .FirstOrDefault(shape => shape != null);
    }

    /// <summary>
    /// Every type whose written form this wire knows, with that form and with who declared it: the ones
    /// declared one by one and the ones a catalog can name. Read when the wire was made, so it is the same
    /// answer however often it is asked.
    /// </summary>
    /// <remarks>
    /// What only a convention recognizes is not here. A convention recognizes a type when it is asked about one
    /// and cannot be asked to name them all, so whoever needs those has to ask type by type.
    /// </remarks>
    public IReadOnlyDictionary<Type, Declaration> GetDeclaredShapes() => declaredShapes;

    /// <summary>
    /// The name of whoever declared the form of this type, or null where nobody declared it of that type: one
    /// written as it stands, or one that only a convention recognizes.
    /// </summary>
    /// <remarks>
    /// Asked of the type and not of the form. A convention declares the form of a family it did not name, so
    /// attributing its types to whoever wrote it would say that the author of a convention declared somebody
    /// else's type — which is the one thing a name of this kind must not say.
    /// </remarks>
    public string? GetDeclarerOf(Type type) =>
        declaredShapes.TryGetValue(type, out var declared) ? declared.Declarer : null;

    /// <summary>
    /// The members of a type, named as they travel: what a declaration says of it, or what whoever writes
    /// answers where nothing was declared. Null where neither could say, which is not "it carries nothing":
    /// whoever reads it has to say that it could not be told.
    /// </summary>
    public IReadOnlyList<Member>? GetMembers(Type type)
    {
        if (GetShape(type) is Shape.Members declared)
        {
            return declared.Value;
        }

        return serializer.GetMembers(type);
    }

    /// <summary>
    /// Whether an enumeration travels as words. What was declared of its form settles it, and where nothing
    /// was, every one of its values is asked, because what decides it is a converter and not the type.
    /// </summary>
    /// <remarks>
    /// Every one and not the first, because an enumeration half of whose values come out as words is one no
    /// document can describe, and taking the first for the rest would publish a form the wire does not keep.
    /// </remarks>
    public bool TravelsByName(Type enumType)
    {
        if (GetShape(enumType) is {Carrier: { } carrier})
        {
            return carrier.Kind == ValueKind.Text;
        }

        var values = Enum.GetValues(enumType).Cast<object>().ToArray();

        return values.Length > 0 &&
               values.All(value => GetValueForm(enumType, value) is EnumerationValueForm.Word);
    }

    /// <summary>
    /// What one value of an enumeration comes out as: the word declared of it, what a source reads, or what
    /// whoever writes answers. Null where none of the three could say.
    /// </summary>
    /// <remarks>
    /// A declared word wins, which is what declaring is for, and it wins over nothing else: the other values of
    /// that enumeration are left as they were answered.
    /// </remarks>
    public EnumerationValueForm? GetValueForm(Type enumType, object value)
    {
        if (byEnumeration.TryGetValue(enumType, out var declared) && declared.TryGetValue(value, out var word))
        {
            return new EnumerationValueForm.Word(word);
        }

        var read = wordSources
            .Select(source => source.GetWord(enumType, value, serializer))
            .FirstOrDefault(answered => answered != null);

        return read != null
            ? new EnumerationValueForm.Word(read)
            : serializer.GetValueForm(enumType, value);
    }

    /// <inheritdoc cref="ISerializer.WriteAsText"/>
    public string WriteAsText(Type type, object value) => serializer.WriteAsText(type, value);

    /// <inheritdoc cref="ISerializer.ReadFromText"/>
    public object? ReadFromText(Type type, ValueKind kind, string text) =>
        serializer.ReadFromText(type, kind, text);

    /// <summary>
    /// What a base turns out to be, each with the word that tells it apart: what was declared of it, what a
    /// source reads, and what whoever writes answers, taken together.
    /// </summary>
    /// <remarks>
    /// Together and not one instead of the other, because a kind is a fact of its own and two parties can know
    /// a different one of them. A declaration wins over the word whoever writes would give the same type, which
    /// is what declaring is for.
    /// <para>
    /// A base is never one of its own kinds, whatever answers: registering it among them is how a hierarchy is
    /// usually written down, and it is not one of the things the base turns out to be.
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<Kind> GetKinds(Type type)
    {
        var kinds = new Dictionary<Type, string>();

        foreach (var kind in serializer.GetKinds(type) ?? Array.Empty<Kind>())
        {
            kinds[kind.Type] = kind.Discriminator;
        }

        var read = kindSources
            .Select(source => source.GetKinds(type, serializer))
            .FirstOrDefault(answered => answered.Count > 0);

        foreach (var kind in read ?? Array.Empty<Kind>())
        {
            kinds[kind.Type] = kind.Discriminator;
        }

        if (byBase.TryGetValue(type, out var declared))
        {
            foreach (var (derived, word) in declared)
            {
                kinds[derived] = word;
            }
        }

        return kinds
            .Where(kind => kind.Key != type)
            .Select(kind => new Kind(kind.Key, kind.Value))
            .ToArray();
    }

    /// <summary>
    /// Every kind this wire can name: the ones declared one by one and the ones a source can name. Read when
    /// the wire was made, as the forms are.
    /// </summary>
    /// <remarks>
    /// What whoever writes knows is not here, and neither is a source that answers about a base only when it is
    /// asked. Both answer about a type that is named to them and cannot be asked to name them all, so a
    /// hierarchy nothing else mentions has to be declared for anyone to find it.
    /// </remarks>
    public IReadOnlyCollection<HierarchyKind> GetDeclaredKinds() => declaredKinds;

    /// <summary>
    /// What every type that travels with these comes out as: the ones named here, and every type those mention
    /// in turn.
    /// </summary>
    /// <remarks>
    /// The answer closes, so that whoever reads it reads it and nothing else: every type named inside one of the
    /// answers is answered too, unless it is one whoever writes already knows — what travels whole, a list, a
    /// map, a nullable of one of those, or anything at all. A type already answered is left alone, which is what
    /// ends the walk where a type holds one of itself.
    /// <para>
    /// Every hierarchy this wire can name is answered as well, whether or not anything mentions it, because a
    /// kind travels in an answer of its base.
    /// </para>
    /// <para>
    /// It is given types and not the calls of an API, because what travels with a type is a question for the
    /// wire: the same answer serves what a column keeps and what a message carries.
    /// </para>
    /// </remarks>
    /// <param name="mentioned">
    /// The types to start from, in the order whoever reads them is to declare them. Null is one of them and is
    /// skipped, so something that travels with nothing needs no filtering out.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// A type came up that neither a declaration nor whoever writes could say anything about. There is no answer
    /// to give for it, and the one thing that could be handed over instead — an object with no members — is what
    /// a generated client writes as an empty object and a document describes as carrying nothing. So nothing is
    /// handed over: declare what those types travel as.
    /// </exception>
    public IReadOnlyDictionary<Type, Structure> GetStructures(IEnumerable<Type?> mentioned)
    {
        var types = new Dictionary<Type, Structure>();
        var unanswered = new List<Type>();

        foreach (var type in mentioned)
        {
            Close(type, types, unanswered);
        }

        foreach (var kind in declaredKinds)
        {
            Close(kind.Base, types, unanswered);
            Close(kind.Type, types, unanswered);
        }

        if (unanswered.Count > 0)
        {
            throw new InvalidOperationException(
                "Neither a declaration nor whoever writes this wire could say what these types travel as, so " +
                "there is no answer to give for what travels: " +
                $"{string.Join(", ", unanswered.Select(type => $"'{type.FullName}'"))}. Declare the form of " +
                "each of them.");
        }

        return types;
    }

    /// <summary>
    /// What a type comes out as, and what the types it mentions come out as. A type already answered is left
    /// alone, which is what ends the walk where a type holds one of itself.
    /// </summary>
    private void Close(Type? type, Dictionary<Type, Structure> types, List<Type> unanswered)
    {
        if (type == null || types.ContainsKey(type))
        {
            return;
        }

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            Close(underlying, types, unanswered);
            return;
        }

        if (type.IsEnum)
        {
            types[type] = EnumerationOf(type);
            return;
        }

        switch (GetShape(type))
        {
            case {Carrier: { } carrier}:
                types[type] = new Structure.Carried(carrier.Type, carrier.KeepsItsName);
                Close(carrier.Type, types, unanswered);
                return;

            // What travels is whatever that other type turns out to be, and the name this one was declared
            // under is not left anywhere: the name in a document is that type's.
            case Shape.AsAnother another:
                types[type] = new Structure.Carried(another.Type, NeedsItsOwnName: false);
                Close(another.Type, types, unanswered);
                return;

            case Shape.Unwraps unwraps:
                var inside = type.GetGenericArguments()[unwraps.ArgumentIndex];

                // No name of its own, whatever it leaves behind: a wrapper means nothing by itself, so there is
                // nothing of it to write. What it leaves may well be a type that does need one, and then the
                // name belongs to that type and not to the wrapper it came out of.
                types[type] = new Structure.Carried(inside, NeedsItsOwnName: false);
                Close(inside, types, unanswered);
                return;

            case Shape.AsFile:
                types[type] = new Structure.File();
                return;

            case Shape.OneOf union:
                var alternatives = union.Alternatives
                    .Select(alternative => new AlternativeType(alternative.Name, TravelsAs(alternative.Form)))
                    .ToArray();

                types[type] = new Structure.OneOf(alternatives);

                // Each of them, because nothing else will: an alternative is a form and not a member, so this
                // is the only place the type it travels as is met.
                foreach (var alternative in alternatives)
                {
                    Close(alternative.Type, types, unanswered);
                }

                return;
        }

        if (IsStructural(type, out var arguments))
        {
            foreach (var argument in arguments)
            {
                Close(argument, types, unanswered);
            }

            return;
        }

        if (IsWrittenAsItIs(type))
        {
            return;
        }

        types[type] = ObjectOf(type, types, unanswered);
    }

    private Structure ObjectOf(Type type, Dictionary<Type, Structure> types, List<Type> unanswered)
    {
        var kinds = GetKinds(type).ToArray();

        var answered = GetMembers(type);
        if (answered == null)
        {
            unanswered.Add(type);
        }

        var members = answered ?? Array.Empty<Member>();
        var @base = DeclaredBaseOf(type);
        var described = new Structure.Object(members, @base, kinds);

        // Written before the walk goes on, so that a type that holds one of itself ends the walk.
        types[type] = described;

        Close(@base, types, unanswered);

        foreach (var kind in kinds)
        {
            Close(kind.Type, types, unanswered);
        }

        foreach (var member in members)
        {
            Close(member.Type, types, unanswered);
        }

        return described;
    }

    /// <summary>
    /// The type this one travels under, which is the nearest one above it that travels as one of several. A type
    /// between them that declares nothing of its own leaves no trace.
    /// </summary>
    private Type? DeclaredBaseOf(Type type)
    {
        for (var current = type.BaseType; current != null && current != typeof(object); current = current.BaseType)
        {
            if (GetKinds(current).Count > 0)
            {
                return current;
            }
        }

        return null;
    }

    private Structure EnumerationOf(Type type)
    {
        var byName = TravelsByName(type);

        var values = Enum.GetValues(type)
            .Cast<object>()
            .Select(value => new EnumerationValue(
                Enum.GetName(type, value)!,
                ((Enum) value).ToString("D"),

                // The number underlying it where nobody could say what it travels as, there being nothing else
                // to go on and a type that is written at all being written as something.
                GetValueForm(type, value)?.Text ?? ((Enum) value).ToString("D")))
            .ToArray();

        return new Structure.Enumeration(byName, values);
    }

    /// <summary>
    /// The type one alternative of a union travels as: what carries it, or the type of the wire it travels as.
    /// </summary>
    private static Type TravelsAs(Shape alternative) => alternative switch
    {
        {Carrier: { } carrier} => carrier.Type,
        Shape.AsAnother another => another.Type,
        _ => throw new ArgumentOutOfRangeException(
            nameof(alternative), alternative, "This form travels as nothing of its own.")
    };

    /// <summary>A type that is made of others and is written as the shape of them: a list, a map.</summary>
    private static bool IsStructural(Type type, out IReadOnlyList<Type> arguments)
    {
        if (type.IsArray)
        {
            arguments = new[] {type.GetElementType()!};
            return true;
        }

        var generic = type.IsGenericType ? type.GetGenericArguments() : Array.Empty<Type>();
        if (generic.Length > 0 && typeof(IEnumerable).IsAssignableFrom(type))
        {
            arguments = generic;
            return true;
        }

        arguments = Array.Empty<Type>();
        return false;
    }

    /// <summary>A type whoever writes writes as it stands, which whoever reads an answer already knows.</summary>
    private static bool IsWrittenAsItIs(Type type) =>
        Carrier.Of(type) != null || type == typeof(object);

    private IReadOnlyDictionary<Type, Declaration> NameEveryShape()
    {
        var declared = new Dictionary<Type, Declaration>();

        // Only what has a list behind it. A convention answers about a type when it is asked and has none, so
        // there is nothing of it to walk here.
        foreach (var (declarer, source) in shapeSources)
        {
            if (source is not IShapeCatalog catalog)
            {
                continue;
            }

            foreach (var (type, shape) in catalog.GetDeclarations(serializer))
            {
                declared[type] = new Declaration(shape, declarer);
            }
        }

        // Last, because a declaration of its own says more than a catalog does: that a string is an email
        // address is not something the converter of one ever said.
        foreach (var (type, declaration) in byType)
        {
            declared[type] = declaration;
        }

        return declared;
    }

    private IReadOnlyCollection<HierarchyKind> NameEveryKind()
    {
        var declared = new Dictionary<(Type Base, Type Type), string>();

        foreach (var source in kindSources)
        {
            foreach (var kind in source.GetDeclarations(serializer))
            {
                declared[(kind.Base, kind.Type)] = kind.Discriminator;
            }
        }

        foreach (var (@base, kinds) in byBase)
        {
            foreach (var (type, word) in kinds)
            {
                declared[(@base, type)] = word;
            }
        }

        return declared
            .Where(kind => kind.Key.Base != kind.Key.Type)
            .Select(kind => new HierarchyKind(kind.Key.Base, kind.Key.Type, kind.Value))
            .ToArray();
    }
}
