# Asking the wire

A wire is the declarations made for it together with its writer. The two make up a `Wire`, and everything it
answers is answered from them, so what it says and what is written cannot come apart.

```csharp
var wire = declaring.Build(serializer);
```

Declaring is done on a `WireBuilder`, and reading on the wire it builds, as
[declaring a form](03%20Declaring%20a%20form.md) says. Nothing below can be asked before there is a wire, and
nothing can be declared once there is one. Everything here is of the booking API of
[getting started](02%20Getting%20started.md).

## What the writer is asked

A declaration does not say everything there is to know about a wire. Four things are known only by whatever writes
it, and the writer is asked them through `ISerializer`:

```csharp
public interface ISerializer
{
    IReadOnlyList<Member>? GetMembers(Type type);
    IReadOnlyCollection<Kind>? GetKinds(Type type);
    EnumerationValueForm? GetValueForm(Type enumType, object value);
    object? WriteAsCarrier(Type valueType, Type carrier, object? value);
    object? ReadFromCarrier(Type valueType, Type carrier, object? carried);
}
```

- **The members of a type as they travel.** The writer already knows the name each member goes under: a naming
  policy, a name written over one of them, a resolver of your own have all had their say by then. Null is an
  answer of its own, *I cannot say*, for a type a converter took over.
- **What a type turns out to be**, where the hierarchies of the wire are written down somewhere the writer can read
  them. Null means *I cannot say*, and empty means *this one is never told apart from anything*. Reading the first
  as the second would describe a base as a plain object and say nothing of what really travels.
- **What a value of an enumeration comes out as**, which no attribute settles and only writing a value shows: a
  word, or a number that need not be the one underlying the member.
- **A value turned into what carries it, and back.** What turns one into the other is the converter of the wire,
  and it cannot be asked to do half of its work.

`IOKode.WireShape.SystemTextJson` answers all of them from a `JsonSerializerOptions`, which is the usual writer of
an application that serves JSON:

```csharp
var serializer = new JsonSerializerOptionsSerializer(options);
```

Any other writer — a codec, a formatter, a serializer of your own, a wire nobody would call a serializer at all —
answers the same questions somewhere else. Nothing in `IOKode.WireShape` names a serializer. That is why the forms
of a wire written in XML, in a column or in Morse code are declared in the same words as these.

## What a type looks like written

```csharp
wire.GetShape(typeof(BookingReference));   // a text, with that pattern
wire.GetShape(typeof(Booking));            // null: it is written as it stands
```

Null is an answer: the type is written as it stands, and the writer describes it already.

## What a type carries

```csharp
wire.GetMembers(typeof(Booking));
// reference  BookingReference        IsOptional: false   IsAnswerOnly: false
// passenger  PassengerRef            IsOptional: false   IsAnswerOnly: false
// segments   IReadOnlyList<Segment>
// total      Money
// cabin      CabinClass
// boarding   BoardingAt?             IsOptional: true
```

The members are asked of the writer, named as they travel. A `Members` declaration wins over that answer, for a
type nothing can be asked about or one written by a converter that builds the object itself.

- **`IsOptional`** is a nullable value type, or a reference whose nullable read state says it may be null.
- **`IsAnswerOnly`** is a member with no setter and no constructor parameter: one the answer carries and a caller
  cannot give. That is what a member worked out from the others looks like from outside.

A declared member and an answered one are the same record, because they are the same facts: the name it travels
under, what it carries, whether it may be absent, whether only an answer ever has it, and what declared it, where
something did. Whoever reads them reads both the same way.

Null is the answer where nobody declared the members and the writer could not say. It does not mean "it carries
nothing": whoever asked has to say that it could not be told. That is why asking for everything that travels with
a type refuses such a type by name, instead of handing it over as an object with no members.

## Enumerations

```csharp
wire.TravelsByName(typeof(CabinClass));                                   // true
wire.GetValueForm(typeof(CabinClass), CabinClass.Economy);                // Word("economy")
wire.GetValueForm(typeof(DisruptionReason), DisruptionReason.Weather);    // Integer("71")
```

An enumeration is two facts and not one: **how** it comes out, as words or as numbers, and **what** comes out for
each of its values.

What comes out for a value is the word declared for it, or what a source reads, or what the writer answers. A
declared word wins, and it wins over nothing else: the other values stay as they were answered. A converter is free
to write a value under a word of its own, `economy` for `Economy`, and just as free to write it as a number of its
own. Only writing the value shows which of the two it is and what it says.

How it comes out is settled by the form, where one was declared: a text means words, anything else means numbers.
Where no form was declared, every one of the values is asked.

**Why every one and not the first:** an enumeration half of whose values come out as words is one no document can
describe, and taking the first for the rest would publish a form the wire does not keep.

## The kinds of a type

```csharp
wire.GetKinds(typeof(Segment));    // (FlightSegment, "flight"), (TrainSegment, "train")
```

This is the one answer where the declarations and the writer are taken **together**, instead of one in place of
the other.

**Why:** a kind is a fact of its own, and the two can each know a different one. A wire written with
`[JsonDerivedType]` carries its kinds where the writer can read them. The wire of a hierarchy nobody wrote down
carries them only where they were declared.

Where both know the same type, the declaration wins. That is what declaring is for: it corrects the word the writer
would give one kind without touching the other kinds of that base. As with declaring,
[a base is never one of its own kinds](03%20Declaring%20a%20form.md#what-a-type-turns-out-to-be).

```csharp
wire.GetDeclaredKinds();    // every kind this wire can name
```

What only the writer knows is not among those, and neither is what a source answers only when it is asked. Both
answer about a type that is named to them and cannot be asked to name them all. That is why a hierarchy nothing
else mentions has to be declared for anyone to find it. [Declaring a form](03%20Declaring%20a%20form.md) says how a
kind is declared and which two declarations are refused.

## Everything that travels with a type

Every answer above is about one type, named to the wire. The last one is about a set, and it is what code that
writes something without a wire in hand reads:

```csharp
IReadOnlyDictionary<Type, Structure> traveling = wire.GetStructures(new[] {typeof(Booking)});
```

Each row is a type and what that type comes out as. The rows are the types it is given, and **every type those
mention in turn**:

- the type of a member;
- the carrier it comes out as;
- what a wrapper leaves behind;
- the type it travels as;
- each of its kinds;
- the alternatives of a union;
- what a list or a map holds;
- the `T` of a `Nullable<T>`.

The rows also include every hierarchy this wire can name, whether anything mentions it or not. A type already
answered is not asked about twice, which is what stops the walk where a type holds one of itself.

So a booking brings `MoneyParts`, which only `Money` mentions, and `FlightSegment` and `TrainSegment`, which nothing
mentions at all.

What the writer already knows how to write gets no row of its own: what travels whole, a list, a map, an array,
and anything at all. So the answer closes: **every type named inside a row is either a row itself or one of
those**, and whoever reads it never meets a type it knows nothing about. That is what lets a client be written by
something with no wire, no serializer and no application in front of it.

```csharp
// Were a booking to carry a Seat, written by a converter of its own and declared by nobody:
wire.GetStructures(new[] {typeof(Booking)});
// throws: Neither a declaration nor whoever writes this wire could say what these types travel as, so there is
// no answer to give for what travels: 'Flights.Seat'. Declare the form of each of them.
```

**A type that neither a declaration nor the writer can say anything about refuses the whole answer**, by name.

**Why:** the one thing that could be handed over instead is an object with no members. A client writes that as an
empty object, and a document describes it as carrying nothing, which is the mistake whose shape is hardest to
notice. There is one way out, and it is always open: declare what that type travels as.

What each row can say is in [the model of an API](07%20Reading%20what%20an%20application%20serves.md), which is
read from this.
