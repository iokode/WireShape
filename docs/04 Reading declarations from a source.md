# Reading declarations from a source

**Package:** `IOKode.WireShape`

Some forms are already said by something else. Reading them from there saves a line that would say the same thing
twice. That is what a source is for: a wire is where a form is kept, not where it is deduced. There is a source for
each of the three things a wire is declared: a form, a kind and a word.

Everything here is of the booking API of [getting started](02%20Getting%20started.md).

## Where a form can be read from

An `IShapeSource` answers what a type looks like written, or null where it has nothing to say. There are two kinds
of source. Which kind a source is says something about the source; it is not a spare method.

**A convention** recognizes a type when it is put in front of it, and has no list behind it, because there is no
set to list. Which `Amended<T>` exist depends on what the API lets a caller leave out.

```csharp
/// <summary>Every identifier of this wire travels as the GUID its own converter writes.</summary>
public sealed class IdentifierShapes : IShapeSource
{
    private static readonly Shape identifier = new Shape.As(Carrier.Guid);

    public Shape? GetShape(Type type, ISerializer serializer) =>
        typeof(IIdentifier).IsAssignableFrom(type) ? identifier : null;
}
```

**A catalog** keeps forms somebody already wrote down, so asking it to name them all has a complete answer. The
usual one reads the converters of the wire. A converter written as `ValueJsonConverter<TValue, TJson>` says, by
its own declaration, that `TValue` travels as `TJson`, so every type written by one of those is named without a
line of its own anywhere. `Carrier.Of` turns the `TJson` it finds into the carrier to declare.

```csharp
public sealed class ConverterShapes(JsonSerializerOptions options) : IShapeCatalog
{
    public Shape? GetShape(Type type, ISerializer serializer) => /* … */;

    public IReadOnlyDictionary<Type, Shape> GetDeclarations(ISerializer serializer) => /* … */;
}
```

Take either kind with `AddShapeSource`:

```csharp
bookings.AddShapeSource(new IdentifierShapes());
bookings.AddShapeSource(new ConverterShapes(options));
```

Where two sources could answer about one type, the one added first answers and the rest are not asked, so the
order you add them in is the order they are tried. Being a catalog buys no precedence. What it buys is that what
it names is among the declarations, and so is seen by everything that walks them without anybody mentioning it
first.

**Hold a catalog to naming everything it answers about.** A catalog that answers about a type it does not name
leaves that type out of everything that walks the declarations, and nothing else notices.
[The rules](12%20The%20rules.md#what-a-wire-is-declared-from) has the one that finds it.

## Asked type by type, and asked to name them all

```csharp
wire.GetShape(typeof(BookingReference));   // a text, with that pattern
wire.GetDeclaredShapes();                  // every type whose form this wire knows
```

`GetShape` answers in one order, and the order is the point:

1. what was declared of that type one by one;
2. what a source answers, the first one added before the rest.

A declaration of its own says more than a source does. That a string is a booking reference is not something the
converter of one ever said, so the declaration is answered first. `GetDeclaredShapes` is built the same way: the
sources first, and what was declared one by one written over them. It is read once, when the wire is built, so it
is the same answer however often it is asked.

**Between two declarations of one type there is no order: the second is refused.** Saying the same form twice is
not a disagreement and is let through, and the part that said it first keeps it, because a part that agrees did
not declare anything.

**Why:** taking either of the two would make what a value looks like depend on the order in which the parts of a
wire happened to be composed, and nobody reasons about that. If two parts disagree about a type, that is worth
hearing, not resolving by coincidence.

Sources work the other way. Whether two of them can answer about one type is only known for a type that is asked
about, never of the sources themselves. What cannot be seen when it is declared is settled where it can be: the
first source added answers, whatever kind of source it is.

**What only a convention recognizes is not among the declarations.** A convention recognizes a type when it is
asked about one, and cannot be asked to name them all, so whoever needs those types has to ask type by type.
Everything that walks the declarations — the type descriptors, the schemas of a document — therefore never sees
`Amended<DepartsOn>` until something mentions it.

## Where the kinds of a type can be read from

An `IHierarchySource` reads the kinds of a base from wherever they are already written down, as an `IShapeSource`
does with forms. [Hierarchies and enumerations](05%20Hierarchies%20and%20enumerations.md#what-a-type-turns-out-to-be)
says what a kind is.

```csharp
/// <summary>What the types of these assemblies turn out to be, read from the attribute each kind carries.</summary>
public sealed class AttributedHierarchies(IEnumerable<Assembly> assemblies) : IHierarchySource
{
    private readonly IReadOnlyCollection<HierarchyKind> kinds = assemblies
        .SelectMany(assembly => assembly.GetTypes())
        .Select(type => (Type: type, Told: type.GetCustomAttribute<ToldApartAsAttribute>()))
        .Where(found => found.Told != null)
        .Select(found => new HierarchyKind(found.Type.BaseType!, found.Type, found.Told!.Word))
        .ToArray();

    public IReadOnlyCollection<Kind> GetKinds(Type declaredBase, ISerializer serializer) =>
        kinds.Where(kind => kind.Base == declaredBase)
            .Select(kind => new Kind(kind.Type, kind.Discriminator))
            .ToArray();

    public IReadOnlyCollection<HierarchyKind> GetDeclarations(ISerializer serializer) => kinds;
}
```

Take it with `AddKindSource`. Where two sources can answer about one base, the first one added answers, as with a
source of forms:

```csharp
bookings.AddKindSource(new AttributedHierarchies(new[] {typeof(Segment).Assembly}));
```

This source can name all its kinds, so a hierarchy nothing mentions is still described. A source that answers
about a base only when it is asked returns nothing from `GetDeclarations`. With one of those, a hierarchy nobody
mentions has to be declared for anyone to find it.

## Where the words of an enumeration can be read from

Where the words are a convention and not a list — every enumeration of this wire travels under its own member
names, cut one way or another — an `IEnumerationSource` says it once:

```csharp
/// <summary>Every enumeration of this wire travels under the name of its member, in snake case.</summary>
public sealed class SnakeCasedWords : IEnumerationSource
{
    public string? GetWord(Type enumType, object value, ISerializer serializer) =>
        Enum.GetName(enumType, value) is {} name ? SnakeCase.Of(name) : null;
}
```

Take it with `AddWordSource`. Where two sources can answer about one value, the first one added answers:

```csharp
bookings.AddWordSource(new SnakeCasedWords());
```

Unlike a source of forms or of kinds, it has one question and not two. Which values an enumeration has is answered
by the language, so nobody ever has to ask a source to name them all.
