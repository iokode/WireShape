# Hierarchies and enumerations

**Package:** `IOKode.WireShape`

What a type turns out to be and what a value of an enumeration comes out as are declared one fact at a time, not as
a form of the whole type. Either can also be read from a source instead;
[reading declarations from a source](04%20Reading%20declarations%20from%20a%20source.md) says how.

Everything here is of the booking API of [getting started](02%20Getting%20started.md).

## What a type turns out to be

Declare a hierarchy kind by kind. A kind is a pair of types, with the word that tells one apart inside the other.
That word is the kind's **discriminator**:

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

Which member the word travels under is not declared here. It is of the whole API, not of one hierarchy, and it is
given to whatever writes the API down: a document, a model, a client.

**A hierarchy is not one of several forms.** A hierarchy is several types under a word that travels inside each of
them, and whoever receives one reads that word. [One of several forms](03%20Declaring%20a%20form.md#the-forms-there-are)
has nothing to read, and whoever receives one looks at what came. Where there is a word to read, declare the kinds:
it is the readable one of the two, and it is the one a client can be written for.

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
