# Declaring a form

Declare a form for a type whose written shape is settled by a converter of its own, because nothing can be asked
about one of those. Everything else is answered by the writer of the wire. What a type turns out to be is declared
too, but that is not a form: it is declared kind by kind, further down.

Everything here is of the booking API of [getting started](02%20Getting%20started.md).

## The forms there are

```csharp
var declaring = new WireBuilder();
var bookings = declaring.GetDeclarer("bookings");
```

Nothing is declared on the builder itself. A wire is composed of parts, and each part declares through a
`Declarer` of its own, so that what it said can be told apart from what the others said.
[Who declared what](#who-declared-what), at the end of this chapter, says the rest.

**`As`** — it travels whole as a value: a length that is a number, an identifier that is a GUID. What it travels as
is a [carrier](#what-a-carrier-is), and the library knows the twenty that every writer writes whole.

**`AsText`** — it travels as a text, and these are the texts. A pattern only means something for a text, so this
is the one form that carries a pattern.

```csharp
bookings.Add<BookingReference>(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
bookings.Add<FlightDuration>(new Shape.As(Carrier.Int64));
bookings.Add<DepartsOn>(new Shape.As(Carrier.DateOnly));
```

**`AsAnother`** — it travels as whatever another type of yours travels as. This is the case of a value written as
the object of the type that carries it. It has no carrier of its own: what travels is whatever that other type
turns out to be, which is asked about it as about anything else.

```csharp
bookings.Add<Money>(new Shape.AsAnother(typeof(MoneyParts)));
```

**`OneOf`** — it travels as one of several forms. Which one arrived is told by the form of what arrived, and not
by anything inside it: a text or an object, a number or a text.

```csharp
bookings.Add<PassengerRef>(new Shape.OneOf(new[]
{
    new Alternative("id", new Shape.As(Carrier.Guid)),
    new Alternative("expanded", new Shape.AsAnother(typeof(Passenger)))
}));
```

**Give each alternative a name of yours. The name does not travel.** Nothing in a union says which alternative
arrived; whoever receives one works that out from what came. The name is for whoever **offers** the alternatives
to somebody else. In a document it is the `title` of the branch, which is where a document says what one branch of
several is called. In a generated client it is the one way of reading that branch. Both have a caller who has to
be told which is which, and only you know.

**It is not a hierarchy.** A hierarchy is several types under a word that travels inside each of them, and whoever
receives one reads that word. A union is several forms with nothing to read, and whoever receives one looks at
what came. Where there is a word to read, [declare the kinds](#what-a-type-turns-out-to-be) instead. It is the
readable one of the two, and it is the one a client can be written for.

A union is refused in four cases, all of them when the form is made, so that a convention or a catalog cannot
slip one in either:

- **fewer than two alternatives**, which is not one of several;
- **two alternatives under one name**, which nobody could tell apart;
- **a name that says nothing**;
- **a form a document cannot write as one of several**: a wrapper that disappears, the content of a file, members
  declared by hand, or another union, which would be the same union written twice.

That leaves three forms an alternative can be: a value, a text, or another type of the wire.

What the name has to look like is not checked here. Turning `expanded` into whatever a client calls it is the work
of whoever writes the client, as naming a schema is the work of whoever writes the document.

Two alternatives that cannot be told apart from each other are **not** refused.

**Why:** it cannot be decided. Two texts whose patterns never overlap can be told apart, and so can two objects
with different members. Judging that in general would mean intersecting patterns and weighing which members are
required, and a check that got it wrong would refuse what a reader reads perfectly well. So it is left to you.
What **does** refuse such a pair is the C# client this library writes, which reads by what arrived and by nothing
else; [a client of an API](08%20A%20client%20of%20an%20API.md) says so there.

**`Unwraps`** — it disappears once written, and the type it wraps takes its place. The argument index says which of
its generic arguments that is.

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

`Amended<T>` is a family and not a type, so it is declared as a convention and not as a line.
[Where a form can be read from](#where-a-form-can-be-read-from) says what a convention is.

**`Members`** — it travels as an object with these members, named as they travel. Use it for a type the writer
cannot be asked about, or for one written by a converter that builds the object itself. No type of the example is
declared this way, because every value of it travels either whole or as another type:

```csharp
bookings.Add<LegacyFare>(new Shape.Members(new[]
{
    new Member("basis", typeof(string), IsOptional: false),
    new Member("bookingClass", typeof(string), IsOptional: false)
}));
```

**`AsFile`** — it travels as the content of a file: the bytes as they stand, under the name and the media type that
whatever carries them gives them, and not written inside anything else.

```csharp
bookings.Add<BoardingPassFile>(new Shape.AsFile());
```

That declaration says what a value comes out as **on this wire**, not what the type is. The same bytes travel as
the content of a file on one wire and as a text in base64 on another, and each wire declares its own:

```csharp
served.Add<byte[]>(new Shape.AsText(Format: "byte"));   // written inside what the wire answers with
uploads.Add<byte[]>(new Shape.AsFile());                // sent as content of its own
```

Declare it also for a type the writer answers about **wrongly**, rather than not at all. A type standing for a
file usually is one: it carries its length, its name and its media type as properties of its own, so the writer
answers that it is an object with those in it. A client written from that answer would send them as members
instead of sending the file.

That makes seven forms, each carrying what is its own and knowing nothing of the others. Each answers one question,
and for some of what reads a declaration it is the only question ever asked:

```csharp
new Shape.AsText(Pattern: "^[A-Z]{6}$").Carrier;   // a text
new Shape.As(Carrier.Int64).Carrier;               // a number, held as a long
new Shape.AsFile().Carrier;                        // null
new Shape.AsAnother(typeof(MoneyParts)).Carrier;   // null
```

`Carrier` is what the value travels whole as, and null for a form that does not travel whole as a value. Reading a
value out of text needs nothing else, and neither does the walk that works out the types of an API. Only whoever
describes a document goes further and asks the form itself, because the pattern belongs to one form and not to
every form.

## What a carrier is

A carrier is **what travels, and what a value of it looks like once written**. It is both, because both are needed
and neither answers for the other: whoever reads a value out of a route converts it to the type, and whoever
describes the wire writes what it looks like.

```csharp
public sealed record Carrier(Type Type, ValueKind Kind, string? Format = null, bool IsSelfExplanatory = false);
```

- **`Type`** — what it travels as, and what a value is read back into.
- **`Kind`** — what a value of it looks like written: `Text`, `Integer`, `Decimal`, `Boolean`. **Four and not
  twenty**, because a `float`, a `double` and a `decimal` are written as the same kind of number. Which of them it
  is read into is what `Type` says.
- **`Format`** — which kind of value it is, where there is a settled name for the kind: `uuid`, `date-time`,
  `int32`. It is a word of whoever describes the wire, not of the writer.
- **`IsSelfExplanatory`** — whether the reader's language already has a kind of its own for this. A name of ours
  over such a value hides what the reader would otherwise recognize, and a client written from it would wrap a
  date of its own language in a type of ours.
- **`KeepsItsName`** — the other side of that, computed from it: whether a type traveling as this one is worth
  keeping the name it was declared under. A number is any number and a text is any text, so with the name gone
  nothing is left of what the value meant.

The twenty that every writer writes whole are here already, named after the type you have in hand:
`Carrier.String`, `Carrier.Guid`, `Carrier.Int64`, `Carrier.DateOnly` and so on. The five that stand for a moment
or a span of time — a date, a time of day, two kinds of instant and a duration — are self-explanatory.

**If the moments in time of your wire are not those five, declare a carrier of your own**, so that it is described
as one and not as a text with a name of ours over it:

```csharp
bookings.Add<BoardingAt>(new Shape.AsText(Format: "date-time", IsSelfExplanatory: true));
```

Without that, `BoardingAt` would get a schema of its own named after it, and a generated client would hand the
caller a wrapper where the caller's own language has an instant. The same goes for a date, a duration or a time of
day held in a library of its own instead of the framework.

`Carrier.Of` answers which of the twenty a type is, or nothing for a type that carries something of its own
inside. It is what reads a declaration made of a type instead of a carrier: a source reading the converters of a
wire has the type one of them writes and nothing else.

**Why one list and not two:** when each projection kept its own, one of them knew of four integer types the other
did not. A type declared to travel as one of those four would have been written as a value by one projection and
pointed at as something with members of its own by the other.

## What declaring one settles

Which carrier a form has decides three things: whether anything has to be asked of the writer at all, whether the
name the value was declared under survives, and which name a document points at.

| Declared to travel as | Asked of the writer | In a document |
| --- | --- | --- |
| `Carrier.String`, `Carrier.Int64`, `Carrier.Guid` | Nothing. What the carrier looks like, with the pattern of the declaration | A schema of its own, under the name of the declared type |
| A self-explanatory carrier — a date, an instant | Nothing | Written where it is used, with no name of ours |
| `AsAnother(MoneyParts)` | Its members, as they travel | A reference to the schema of **that type** |
| An enumeration | The word each value comes out as | A schema of its own, with those words |

The third row is worth reading twice. Declaring that a value travels as another type of yours **puts the name of
that type in the document and takes the name of the declared type out of it**. A member of type `Money` declared
as `AsAnother(typeof(MoneyParts))` is described as a reference to `MoneyParts`, and nothing in the document says
`Money` anymore. That is right, because the document describes what travels, and what travels is that type's
object. But it is the opposite of what happens to a value written whole, where the name is the only thing left to
tell one number from another.

In the two rows that ask, what is asked is asked of the writer of this wire. Its answer is what is written in the
model and in a document; it never becomes a declaration.

## Where the declaring ends

Nothing is read from a `WireBuilder`. Make all the declarations, then build the wire from them and the writer:

```csharp
Wire wire = declaring.Build(new JsonSerializerOptionsSerializer(options));
```

What a wire says of a type cannot change afterwards, because there is no way to declare anything on it. Building
copies what was declared. So building twice gives two wires that have nothing to do with each other, and anything
declared later leaves the wires already built as they were. The same declarations built with two writers give a
wire for each, which is what an application that writes the same types in two places needs.

`Build` settles the declarations, not the world. A source belongs to whoever wrote it, and the writer reads
whatever it is reading at the moment it is asked. In JSON, that is a set of options whose converters are a list
somebody else can still add to. A wire is not a photograph.

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

## Where a form can be read from

Some forms are already said by something else. Reading them from there saves a line that would say the same thing
twice. That is what an `IShapeSource` is for: a wire is where a form is kept, not where it is deduced.

There are two kinds of source. Which kind a source is says something about the source; it is not a spare method.

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

## What a type turns out to be

Declare a hierarchy kind by kind. A kind is a pair of types, with the word that tells one apart inside the other:

```csharp
bookings.AddKind<Segment, FlightSegment>("flight");
bookings.AddKind(new HierarchyKind(typeof(Segment), typeof(TrainSegment), "train"));
```

**Why one fact at a time:** that is what a hierarchy is made of. The word belongs to the pair and not to either
type on its own, because the same type can be a kind of two bases and travel under a different word in each. And
inheritance is open, so nobody holding a base necessarily knows every type that derives from it.

Two declarations are refused, and they are the only two that cannot be written or read:

- the same pair given a second word, which cannot be written;
- one word given to two kinds of one base, which cannot be read.

A word used under two different bases is neither, and is let through.

```csharp
wire.GetKinds(typeof(Segment));      // (FlightSegment, "flight"), (TrainSegment, "train")
wire.GetDeclaredKinds();             // every kind this wire can name
```

A base is never one of its own kinds, whatever answers. Registering the base among its kinds is how a hierarchy is
usually written down, but the base is not one of the things it turns out to be.

An `IHierarchySource` reads the kinds of a base from wherever they are already written down, as an `IShapeSource`
does with forms:

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

## What an enumeration comes out as

An enumeration is two facts, and they are declared in two places, because they are facts about two different
things.

**How it comes out** is a form, declared as any other form is. A text means it travels as words; anything else
means numbers.

```csharp
bookings.Add<CabinClass>(new Shape.AsText());
```

The example does not declare it, because it does not have to. Where no form is declared, every value is asked,
and a word declared for each of them already says that the enumeration travels as words.

**What comes out for each value** is a word of that value. Declare it one value at a time, as a kind is declared:

```csharp
bookings.AddWord(CabinClass.Economy, "economy");
bookings.AddWord(CabinClass.Business, "business");
```

The same two declarations are refused, and again they are the only two things that can be wrong:

- one value given a second word, which cannot be written;
- one word given to two values of the same enumeration, which cannot be read.

A word used in two different enumerations is neither, and is let through.

An enumeration declared to travel as a number carries the numbers underlying its own members, so there is nothing
to declare of each value. Declaring words for one is refused when the wire is built, because either the words
travel or the numbers do, and nothing could write both.

The `DisruptionReason` of the example travels as its numbers without being declared at all. No word is declared
for any of its values, and the writer writes the codes settled long ago, which are what travels.

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

## Who declared what

A wire is composed of parts, and the form alone does not always tell them apart. Two parts may each declare a type
of their own that happens to have the same name. Whoever has to give one of them a name of its own — a schema of a
document, a type of a client — then has nothing else to tell them apart by. So every declaration carries the name
of the part that made it:

```csharp
var bookings = declaring.GetDeclarer("bookings");
var loyalty = declaring.GetDeclarer("loyalty");

bookings.Add<BookingReference>(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
loyalty.Add<MemberNumber>(new Shape.AsText(Pattern: "^[0-9]{9}$"));

wire.GetDeclarerOf(typeof(BookingReference));              // "bookings"
wire.GetDeclaredShapes()[typeof(MemberNumber)].Declarer;   // "loyalty"
```

The name is a word of whoever composed the wire, and nothing here reads it: a plugin, a context, half of an
application, whatever the parts are. Two parts are told apart by it and by nothing else. Asking twice for a
declarer under the same name gives the same declarer twice.

**A part declares under its own name and under no other.** That is why a part is handed a `Declarer` and not the
builder: whoever composes the wire decides what each part is called, and a part cannot attribute a form to
anybody else.

**What is refused is refused across all the parts**, and the refusal says which two disagreed:

> `'Flights.Money' is already declared by 'bookings' to travel as a value carried as 'System.Decimal' under the
> format 'decimal', and 'loyalty' declares it as a text.`

**A type that only a convention recognizes has no declarer**, and `GetDeclarerOf` answers null for it.

**Why:** a convention declares the form of a family it did not name. `Amended<T>` is any `T` the API lets a caller
leave out. Naming its types after whoever wrote the convention would say that the author of a convention declared
somebody else's type, which is the one thing a name of this kind must not say. Whoever needs an owner for such a
type has to work it out where the owner is known, which is not here.

## One wire, one set of declarations

All of this is of one wire, and [the problem](01%20The%20problem.md#what-is-not-here) says why each wire declares
its own forms.

What cannot be split by wire is the binder. A type descriptor belongs to the whole process, so only one wire can
teach it. [Reading a value from text](05%20Reading%20a%20value%20from%20text.md) says what follows from that.
