# Declaring a form

A form is declared of a type whose written shape is settled by the code of a converter of its own, there being
nothing to ask about one of those. Everything else is answered by whoever writes the wire, which is asked about
it. What a type turns out to be is declared too, and that one is not a form: it is said kind by kind, further
down.

Everything here is of the booking API of [getting started](02%20Getting%20started.md).

## The forms there are

```csharp
var declaring = new WireBuilder();
var bookings = declaring.GetDeclarer("bookings");
```

Nothing is declared on the builder itself. A wire is composed of parts, and each declares through a `Declarer`
of its own so that what it said is tellable from what the others said — [who declared what](#who-declared-what)
is at the end of this chapter.

**`As`** — it travels whole as a value: a length that is a number, an identifier that is a GUID. What it travels
as is a [carrier](#what-a-carrier-is), and the library knows the twenty every writer writes whole.

**`AsText`** — it travels as a text, and these are the texts. A pattern is read of a text and of nothing else,
so it is this form that carries one and no other can.

```csharp
bookings.Add<BookingReference>(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
bookings.Add<FlightDuration>(new Shape.As(Carrier.Int64));
bookings.Add<DepartsOn>(new Shape.As(Carrier.DateOnly));
```

**`AsAnother`** — it travels as whatever another type of yours travels as, which is the case of a value written
as the object of the type that carries it. It has no carrier of its own: what travels is whatever that type turns
out to be, asked about it as about anything else.

```csharp
bookings.Add<Money>(new Shape.AsAnother(typeof(MoneyParts)));
```

**`OneOf`** — it travels as one of several forms, and which one is told by the form of what arrived and not by
anything inside it: a text or an object, a number or a text.

```csharp
bookings.Add<PassengerRef>(new Shape.OneOf(new[]
{
    new Alternative("id", new Shape.As(Carrier.Guid)),
    new Alternative("expanded", new Shape.AsAnother(typeof(Passenger)))
}));
```

**Each alternative goes under a name of yours, and the name is not in what travels.** Nothing in a union says
which alternative arrived — whoever receives one works that out from what came — so the name is there for
whoever **offers** the alternatives to somebody: the `title` of the branch in a document, which is where a
document says what one branch of several is called, and the one way of reading it in a generated client. What
those two have in common is a caller who has to be told which is which, and only you know.

**It is not a hierarchy.** A hierarchy is several types under a word that travels inside each of them, and
whoever receives one reads that word; this is several forms with nothing to read, and whoever receives one looks
at what came. Where there is a word to read, [declare the kinds](#what-a-type-turns-out-to-be) instead: it is
the readable one of the two, and it is the one a client can be written for.

Four refusals, all of them at the moment of writing the form, so that a convention or a catalog cannot smuggle
one in either: **fewer than two alternatives**, which is not one of several; **two of them under one name**, which is
two nobody could tell apart; **a name that says nothing**; and **a form a document cannot write as one of
several** — a wrapper that disappears, the content of a file, members declared by hand, or another union, which
is the same union written twice. What is left is the three forms that travel as a value, as a text, or as another
type of the wire.

What the name has to look like is not checked here: turning `expanded` into whatever a client calls it is the
work of whoever writes the client, as naming a schema is the work of whoever writes the document.

What is **not** refused is two alternatives that cannot be told apart from each other, and the reason is that it
cannot be decided: two texts whose patterns never overlap are told apart, two objects with different members are
told apart, and judging that in general would mean intersecting patterns and weighing which members are
required. A check that got it wrong would refuse what a reader reads perfectly well, so it is left to you. What
**does** refuse such a pair is whoever writes a client for it, where the reading is by what arrived and nothing
else — [a client of an API](08%20A%20client%20of%20an%20API.md) says it there.

**`Unwraps`** — it disappears once written, and the type it wraps takes its place. The argument index says which
of its generic arguments that is.

```csharp
public sealed class AmendedWireShapes : IShapeSource
{
    public Shape? GetShape(Type type, ISerializer serializer) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Amended<>)
            ? new Shape.Unwraps(0)
            : null;
}

bookings.AddShapeSource(new AmendedWireShapes());
```

A family and not a type, so it is declared as a convention and not as a line:
[where a form can be read from](#where-a-form-can-be-read-from) says what that is.

**`Members`** — it travels as an object with these members, named as they travel. For a type the serializer
cannot be asked about, or one written by a converter that builds the object itself. No type of the example is
declared this way, every value of it travelling either whole or as another type:

```csharp
bookings.Add<LegacyFare>(new Shape.Members(new[]
{
    new Member("basis", typeof(string), IsOptional: false),
    new Member("bookingClass", typeof(string), IsOptional: false)
}));
```

**`AsFile`** — it travels as the content of a file: the bytes as they stand, under the name and the media type
whatever carries them gives them, and not written inside anything else.

```csharp
bookings.Add<BoardingPassFile>(new Shape.AsFile());
```

What that declaration says is what a value comes out as **on this wire**, not what the type is. The same bytes
travel as the content of a file on one wire and as a text in base64 on another, and each wire declares its own:

```csharp
served.Add<byte[]>(new Shape.AsText(Format: "byte"));   // written inside what the wire answers with
uploads.Add<byte[]>(new Shape.AsFile());                // sent as content of its own
```

It is also the form to declare of a type whoever writes answers **wrongly** about rather than not at all, which
is what a type standing for a file usually gets: such a type carries its length, its name and its media type as
properties of its own, so whoever writes answers that it is an object with those in it, and a client written from
that answer would send them as members instead of sending the file.

Seven forms, each carrying its own and knowing nothing of the others. Every one of them answers one question, and
it is the only thing some of what reads a declaration ever asks:

```csharp
new Shape.AsText(Pattern: "^[A-Z]{6}$").Carrier;   // a text
new Shape.As(Carrier.Int64).Carrier;               // a number, held as a long
new Shape.AsFile().Carrier;                        // null
new Shape.AsAnother(typeof(MoneyParts)).Carrier;   // null
```

`Carrier` is what it travels whole as, and null for a form that does not travel whole as a value. What reads a
value out of text needs nothing else, and neither does the walk that works out the types of an API; only whoever
describes a document goes further and asks the form itself, because the pattern is of the form and not of every
form.

## What a carrier is

A carrier is **what travels, and what a value of it looks like once written**. Both, because both are needed and
neither answers for the other: whoever reads a value out of a route converts it to the type, and whoever
describes the wire writes what it looks like.

```csharp
public sealed record Carrier(Type Type, ValueKind Kind, string? Format = null, bool IsSelfExplanatory = false);
```

- **`Type`** — what it travels as, and what a value is read back into.
- **`Kind`** — what a value of it looks like written: `Text`, `Integer`, `Decimal`, `Boolean`. **Four and not
  twenty**, because a `float`, a `double` and a `decimal` are written the same number and which of them it is
  read into is what `Type` says.
- **`Format`** — which kind of value it is, where there is a settled name for the kind: `uuid`, `date-time`,
  `int32`. It is a word of whoever describes the wire and not of whoever writes it.
- **`IsSelfExplanatory`** — whether the language of whoever reads has a kind of its own for this already. A name
  of ours over such a value hides what the reader would otherwise recognise, and a client written from it would
  wrap a date of its own language in a type of ours.
- **`KeepsItsName`** — the other side of that: whether a type travelling as this one is worth keeping the name it
  was declared under. A number is any number and a text is any text, so with the name gone nothing is left of
  what the value meant.

The twenty every writer writes whole are here already — `Carrier.String`, `Carrier.Guid`, `Carrier.Int64`,
`Carrier.DateOnly`, named after the type you have in hand — and the five that stand for a moment in time are
self-explanatory. **A wire whose moments in time are not those five declares a carrier of its own**, and is
described as one and not as a text with a name of ours over it:

```csharp
bookings.Add<BoardingAt>(new Shape.AsText(Format: "date-time", IsSelfExplanatory: true));
```

Without that, `BoardingAt` would get a schema of its own named after it, and a generated client would hand the
caller a wrapper where its own language has an instant. It is the same of a date, a duration or a time of day
held in a library of its own rather than in the framework.

`Carrier.Of` answers which of them a type is, or nothing for a type that carries something of its own inside.
That is what reads a declaration made of a type rather than of a carrier — a source reading the converters of a
wire has the type one of them writes and nothing else — and it is why there is one list and not two: when each
projection kept its own, one of them knew of four integer types the other did not, and a type declared to travel
as one of those four would have been written as a value by one and pointed at as something with members of its
own by the other.

## What declaring one settles

Which carrier it is decides whether anything has to be asked of the serializer at all, whether the name the
value was declared under survives, and which name a document points at:

| Declared to travel as | Asked of the serializer | In a document |
| --- | --- | --- |
| `Carrier.String`, `Carrier.Int64`, `Carrier.Guid` | Nothing. What the carrier looks like, with the pattern of the declaration | A schema of its own, under the name of the declared type |
| A self-explanatory carrier — a date, an instant | Nothing | Written where it is used, with no name of ours |
| `AsAnother(MoneyParts)` | Its members, as they travel | A reference to the schema of **that type** |
| An enumeration | The word each value comes out as | A schema of its own, with those words |

The third row is the one worth reading twice. Declaring that a value travels as another type of yours **puts the
name of that type in the document and takes the name of the declared type out of it**: a member of type `Money`
declared as `AsAnother(typeof(MoneyParts))` is described as a reference to `MoneyParts`, and nothing in the
document says `Money` any more. That is right — the document describes what travels, and what travels is that
type's object — but it is the opposite of what a value written whole does, where the name is the only thing left
to tell one number from another.

What is asked, in the two rows that ask, is asked of whoever writes this wire, and the answer is what is written
in the model and in a document — never a declaration.

## Where the declaring ends

Nothing is read from a `WireBuilder`. The declarations are all made, and then the wire is made of them and of
whoever writes it:

```csharp
Wire wire = declaring.Build(new JsonSerializerOptionsSerializer(options));
```

What a wire says of a type cannot change afterwards, because there is no way to declare anything on it. What was
declared is copied, so building twice gives two wires that have nothing to do with each other, and anything
declared later leaves the ones already built as they were. The same declarations built with two writers are a
wire of each, which is what an application that writes the same types in two places has.

What `Build` settles is the declarations, not the world. A source belongs to whoever wrote it and whoever
writes the wire reads whatever it is reading now — in JSON, a set of options whose converters are a list
somebody else can still add to. A wire is not a photograph.

## Asked type by type, and asked to name them all

```csharp
wire.GetShape(typeof(BookingReference));   // a text, with that pattern
wire.GetDeclaredShapes();                  // every type whose form this wire knows
```

`GetShape` answers in one order, and the order is the point:

1. what was declared of that type one by one,
2. what a source answers, the first one added before the rest.

A declaration of its own says more than a source does — that a string is a booking reference is not something
the converter of one ever said — so it is answered first. `GetDeclaredShapes` is built the same way, the sources
first and what was declared one by one written over them, and it is read once when the wire is made: it is the
same answer however often it is asked.

**Between two declarations of one type there is no order: the second is refused.** Taking either of them would
make what a value looks like depend on the order the parts of a wire happened to be composed in, which is not
something anyone reasons about — and if two parts disagree about a type, that is worth hearing rather than
resolving by coincidence. Saying the same form twice is not a disagreement and is let through, and the one who
said it first keeps it: whoever agrees did not declare it.

A source works the other way for a reason: whether two of them can answer about one type is only known of a type
that is asked about and never of the sources themselves. What cannot be seen when it is declared is settled where
it can be — the first one added answers, whatever kind of source it is.

**What only a convention recognises is not among the declarations.** A convention recognises a type when it is
asked about one and cannot be asked to name them all, so whoever needs those has to ask type by type. Everything
that walks the declarations — the type descriptors, the schemas of a document — therefore never sees
`Amended<DepartsOn>` until something mentions it.

## Where a form can be read from

Some forms are already said by something else, and reading them from there saves a line that would say it twice.
That is what an `IShapeSource` is: a wire is where a form is kept, not where it is deduced.

There are two kinds of them, and which one a source is is a statement about the source and not a spare method.

**A convention** recognises a type when it is put in front of it and has no list behind it, because there is no
set: which `Amended<T>` exist depends on what the API lets a caller leave out.

```csharp
/// <summary>Every identifier of this wire travels as the GUID its own converter writes.</summary>
public sealed class IdentifierShapes : IShapeSource
{
    private static readonly Shape identifier = new Shape.As(Carrier.Guid);

    public Shape? GetShape(Type type, ISerializer serializer) =>
        typeof(IIdentifier).IsAssignableFrom(type) ? identifier : null;
}
```

**A catalog** keeps forms somebody already wrote down, so asking it to name them all has a complete answer.
Reading the converters of the wire is the usual one: a converter written as `ValueJsonConverter<TValue, TJson>`
says by its own declaration that `TValue` travels as `TJson`, and every type written by one of those is named
without a line of its own anywhere. `Carrier.Of` turns the `TJson` it finds into the carrier to declare.

```csharp
public sealed class ConverterShapes(JsonSerializerOptions options) : IShapeCatalog
{
    public Shape? GetShape(Type type, ISerializer serializer) => /* … */;

    public IReadOnlyDictionary<Type, Shape> GetDeclarations(ISerializer serializer) => /* … */;
}
```

Either is taken in the same way:

```csharp
bookings.AddShapeSource(new IdentifierShapes());
bookings.AddShapeSource(new ConverterShapes(options));
```

Where two of them could answer about one type, the one added first answers and the rest are not asked, so the
order they are taken in is the order they are tried. Being a catalog buys no precedence: what it buys is that
what it names is among the declarations, and so is seen by everything that walks them without anybody mentioning
it first.

## What a type turns out to be

A hierarchy is declared kind by kind, and a kind is a pair of types with the word that tells one apart inside
the other:

```csharp
bookings.AddKind<Segment, FlightSegment>("flight");
bookings.AddKind(new HierarchyKind(typeof(Segment), typeof(TrainSegment), "train"));
```

One fact at a time, because that is what a hierarchy is made of. The word belongs to the pair and not to either
type on its own — the same type can be a kind of two bases, and travel under a different word in each — and
nobody holding a base necessarily knows every type that derives from it, inheritance being open.

Two things are refused, and they are the only two that cannot be written or read: the same pair given a second
word, which cannot be written, and one word given to two kinds of one base, which cannot be read. A word used
under two different bases is neither, and is let through.

```csharp
wire.GetKinds(typeof(Segment));      // (FlightSegment, "flight"), (TrainSegment, "train")
wire.GetDeclaredKinds();             // every kind this wire can name
```

A base is never one of its own kinds, whatever answers. Registering it among them is how a hierarchy is usually
written down, and it is not one of the things the base turns out to be.

An `IHierarchySource` reads the kinds of a base from wherever they are already written down, as an
`IShapeSource` does with the forms:

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

This one can name them all, so a hierarchy nothing mentions is still described. A source that answers about a
base only when it is asked returns nothing from `GetDeclarations`, and then whoever needs a hierarchy nobody
mentions has to declare it.

## What an enumeration comes out as

An enumeration is two facts, and they are declared in two places because they are of two different things.

**How it comes out** is a form, declared as any other form is: a text means it travels as words, anything else
means numbers.

```csharp
bookings.Add<CabinClass>(new Shape.AsText());
```

**What comes out for each value** is a word of that value, one at a time, as a kind is:

```csharp
bookings.AddWord(CabinClass.Economy, "economy");
bookings.AddWord(CabinClass.Business, "business");
```

With the same two refusals, which are again the only two things that can be wrong: one value given a second
word, which cannot be written, and one word given to two values of the same enumeration, which cannot be read.
A word used in two different enumerations is neither, and is let through.

An enumeration declared to travel as a number carries the numbers underlying its own members, so there is
nothing to declare of each value — and declaring words for one is refused when the wire is built, because either
the words travel or the numbers do and nothing could write both. The `DisruptionReason` of the example is one of
those: its codes were settled long ago and they are what travels.

Where the words are a convention and not a list — every enumeration of this wire travels in the shape of its
own member names, cut one way or another — an `IEnumerationSource` says it once:

```csharp
/// <summary>Every enumeration of this wire travels under the name of its member, in snake case.</summary>
public sealed class SnakeCasedWords : IEnumerationSource
{
    public string? GetWord(Type enumType, object value, ISerializer serializer) =>
        Enum.GetName(enumType, value) is {} name ? SnakeCase.Of(name) : null;
}
```

One question and not two, unlike a source of forms or of kinds: which values an enumeration has is answered by
the language, so nobody ever has to ask a source to name them all.

## Who declared what

A wire is composed of parts, and the form alone does not always tell them apart. Two parts may each declare a
type of their own that happens to be called the same, and whoever has to give one of them a name of its own — a
schema of a document, a type of a client — has nothing else to tell them by. So every declaration carries the
name of whoever made it:

```csharp
var bookings = declaring.GetDeclarer("bookings");
var loyalty = declaring.GetDeclarer("loyalty");

bookings.Add<BookingReference>(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
loyalty.Add<MemberNumber>(new Shape.AsText(Pattern: "^[0-9]{9}$"));

wire.GetDeclarerOf(typeof(BookingReference));              // "bookings"
wire.GetDeclaredShapes()[typeof(MemberNumber)].Declarer;   // "loyalty"
```

The name is a word of whoever composed the wire and is read by nothing here: a plugin, a context, a half of an
application, whatever the parts are. Two of them are told apart by it and by nothing else, and asking for one
twice under the same name is the same declarer twice.

**A part declares under its own name and under no other.** That is what handing it a `Declarer` rather than the
builder is for: whoever composes the wire decides what each part is called, and a part cannot attribute a form
to anybody else.

**What is refused is refused across all of them**, and the refusal says which two disagreed:

> `'Money' is already declared by 'bookings' to travel as As { By = Decimal }, and 'loyalty' declares it as
> AsText { Pattern = }.`

**A type only a convention recognises has no declarer**, and `GetDeclarerOf` answers null for it. A convention
declares the form of a family it did not name — `Amended<T>` is any `T` the API lets a caller leave out — so
naming its types after whoever wrote it would say that the author of a convention declared somebody else's type,
which is the one thing a name of this kind must not say. Whoever needs an owner for such a type works it out
where it is known, which is not here.

## One wire, one set of declarations

All of this is of one wire. An application that serves an API has one for that API; what it keeps in a column it
keeps as the column holds it, and what it sends to an API of someone else it writes as that one asks. Those are
three wires, and nothing here makes them agree, because they describe three different things.

What cannot be split that way is the binder: a type descriptor is of the whole process, so only one wire can
teach it. [Reading a value from text](05%20Reading%20a%20value%20from%20text.md) says what follows from that.
