# Asking the wire

A wire is the declarations made for it and whoever writes it. The two together are a `Wire`, and what it answers
is answered from them, so what it says and what is written cannot come apart.

```csharp
var wire = declaring.Build(serializer);
```

Declaring is done on a `WireBuilder` and reading on the wire it builds, as
[declaring a form](03%20Declaring%20a%20form.md) says. Nothing below can be asked before there is a wire, and
nothing can be declared once there is one. Everything here is of the booking API of
[getting started](02%20Getting%20started.md).

## What whoever writes is asked

A declaration does not say everything there is to know about a wire. Four things are known only by whatever
writes it, and it is asked them through `ISerializer`:

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

- **The members of a type as they travel.** Whoever writes already knows the name each member goes under: a
  naming policy, a name written over one of them, a resolver of your own have all had their say by then. Null is
  an answer of its own — *I cannot say* — for a type a converter took over.
- **What a type turns out to be**, where the hierarchies of the wire are written down somewhere it can read
  them. Null is *I cannot say*, and empty is *this one is never told apart from anything*; reading the first as
  the second would describe a base as a plain object and say nothing of what really travels.
- **What a value of an enumeration comes out as**, which no attribute settles and only writing a value shows:
  a word, or a number that need not be the one underlying the member.
- **A value turned into what carries it, and back.** What turns one into the other is the converter of the wire,
  and there is no asking it to do half of its work.

`IOKode.WireShape.SystemTextJson` answers all of them from a `JsonSerializerOptions`, which is the usual answer
for an application that serves JSON:

```csharp
var serializer = new JsonSerializerOptionsSerializer(options);
```

Anything else — a codec, a formatter, a serializer of your own, a wire nobody would call a serializer at all —
is the same questions answered somewhere else. Nothing in `IOKode.WireShape` names a serializer, and that is
why the forms of a wire written in XML, in a column or in morse are declared in the same words as these.

## What a type looks like written

```csharp
wire.GetShape(typeof(BookingReference));   // a text, with that pattern
wire.GetShape(typeof(Booking));            // null: it is written as it stands
```

Null is an answer: the type is written as it stands, and whoever writes it describes it already.

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

The members are asked of whoever writes the wire, named as they travel. A `Members` declaration wins over that
answer, for a type nothing can be asked about or one written by a converter that builds the object itself.

- **`IsOptional`** is a nullable value type, or a reference whose nullable read state says it may be null.
- **`IsAnswerOnly`** is a member with no setter and no constructor parameter: one the answer carries and a caller
  cannot give, which is what a member worked out from the others looks like from outside.

One record carries both a declared member and an answered one, because they are the same facts: the name it
travels under, what it carries, whether it may be absent, whether only an answer ever has it, and what declared
it, where there is something that did. A declaration and an answer are read the same way by whoever reads them.

Null is the answer where nobody declared the members and whoever writes could not say. It is not "it carries
nothing": whoever asked has to say that it could not be told, and it is why asking for everything that travels
with a type refuses such a type by name rather than hand it over as an object with no members.

## Enumerations

```csharp
wire.TravelsByName(typeof(CabinClass));                                   // true
wire.GetValueForm(typeof(CabinClass), CabinClass.Economy);                // Word("economy")
wire.GetValueForm(typeof(DisruptionReason), DisruptionReason.Weather);    // Integer("71")
```

An enumeration is two facts and not one: **how** it comes out, as words or as numbers, and **what** comes out
for each of its values. The second is a word declared of that value, or what a source reads, or what whoever
writes answers — a declared word wins, and it wins over nothing else, the other values staying as they were
answered. A converter is free to write a value under a word of its own, `economy` for `Economy`, and just as free
to write it as a number of its own, and nothing but writing it says which of the two it is or what it says.

The first is settled by the form where one was declared — a text means words, anything else means numbers — and
where none was, every one of the values is asked. Every one and not the first: an enumeration half of whose
values come out as words is one no document can describe, and taking the first for the rest would publish a
form the wire does not keep.

## What a type turns out to be

```csharp
wire.GetKinds(typeof(Segment));    // (FlightSegment, "flight"), (TrainSegment, "train")
```

This is the one answer where the declarations and whoever writes are taken **together** rather than one instead
of the other, because a kind is a fact of its own and two parties can know a different one of them: a wire
written with `[JsonDerivedType]` carries its kinds where whoever writes can read them, and the wire of a
hierarchy nobody wrote down carries them only where they were declared.

Where both know the same type, the declaration wins. That is what declaring is for: the word whoever writes
would give a kind is corrected without the other kinds of that base being touched.

A base is never one of its own kinds, whatever answers. Registering it among them is how a hierarchy is usually
written down, and it is not one of the things the base turns out to be.

```csharp
wire.GetDeclaredKinds();    // every kind this wire can name
```

What only whoever writes knows is not among those, and neither is what a source answers only when it is asked.
Both answer about a type that is named to them and cannot be asked to name them all, which is why a hierarchy
nothing else mentions has to be declared for anyone to find it. [Declaring a form](03%20Declaring%20a%20form.md)
says how a kind is declared and which two declarations are refused.

## Everything that travels with a type

Every answer above is about one type that is named to the wire. The last one is about a set, and it is what
whoever writes without a wire in hand reads:

```csharp
IReadOnlyDictionary<Type, Structure> travelling = wire.GetStructures(new[] {typeof(Booking)});
```

A type in each row, and what that type comes out as. The rows are the types it is given and **every type those
mention in turn** — the type of a member, the carrier it comes out as, what a wrapper leaves behind, the type it
travels under, each of its kinds, the alternatives of a union, what a list or a map holds, the `T` of a
`Nullable<T>` — and every hierarchy this wire can name, whether anything mentions it or not. A type already
answered is not asked about twice, which is what stops it where a type holds one of itself.

So a booking brings `MoneyParts`, which only `Money` mentions, and `FlightSegment` and `TrainSegment`, which
nothing mentions at all.

What gets no row of its own is what whoever writes already knows how to write: what travels whole, a list, a map,
an array, and anything at all. So the answer closes: **every type named inside a row is either a row itself or
one of those**, and whoever reads it never meets a type it knows nothing about. That is what lets a client be
written by something with no wire, no serializer and no application in front of it.

```csharp
wire.GetStructures(new[] {typeof(Booking)});    // throws: nothing could say what 'Flights.Seat' travels as
```

**A type neither a declaration nor whoever writes could say anything about refuses the whole answer**, by name.
The one thing that could be handed over instead is an object with no members, and that is what a client writes
as an empty object and a document describes as carrying nothing — the mistake whose shape is hardest to notice.
There is one way out and it is always open: declare what that type travels as.

What each row can say is in [the model of an API](07%20Reading%20what%20an%20application%20serves.md), which is
read from this.
