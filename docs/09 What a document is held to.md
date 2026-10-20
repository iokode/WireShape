# What a document is held to

Rules that hold of any document that describes a wire, whatever that document happens to describe. Each one
answers with everything that breaks it, so a run says all of it instead of the first thing, and each answer names
the document and what is wrong in it.

```csharp
var broken = new List<string>();

foreach (var (name, document) in served)
{
    broken.AddRange(await Invariants.FindUnreferencedSchemasAsync(name, document));
    broken.AddRange(await Invariants.FindDanglingReferencesAsync(name, document));
    broken.AddRange(Invariants.FindSchemasCarrying(name, document, "shouldAmend"));
    broken.AddRange(Invariants.FindEnumerationsDescribedOtherwise(name, document, wire, nameOf, assemblies));
    broken.AddRange(Invariants.FindNamedValuesDescribedOtherwise(name, document, wire, nameOf));
    broken.AddRange(Invariants.FindValueObjectsOfferingWhatIsWorkedOut(
        name, document, wire, assemblies, nameOf, IsValueObject, Answers));
}

Assert.True(broken.Count == 0, string.Join(Environment.NewLine, broken));
```

## The rules

**`FindUnreferencedSchemasAsync`** — nothing is described that nothing points at. A schema nobody reaches is either
residue of something that stopped being referenced or a type that walked into the document on its own.

**`FindDanglingReferencesAsync`** — nothing points at what is not described. A reference to a schema the document
does not have leaves whoever reads it with a member they cannot know the form of.

**`FindSchemasCarrying`** — no schema offers a member that never travels. Which member that is depends on what the
wire is made of, so whoever runs it says which one: a rule can be asked whether it recognises a type, never to
name the types it recognises, so the family `Amended<>` belongs to cannot be enumerated and the member it would
have brought with it is named here instead.

**`FindEnumerationsDescribedOtherwise`** — an enumeration is described as the wire writes it. Whether `CabinClass`
travels as a word or as a number is asked of the wire, which writes a value to find out, because what decides it
is a converter and not the type.

**`FindNamedValuesDescribedOtherwise`** — a type declared to travel as a value is described as that value and under
its own name, with the format and the pattern it was declared with. A `BookingReference` described as something
with members is a declaration the document did not keep.

**`FindValueObjectsOfferingWhatIsWorkedOut`** — a value object travels as the data it carries and not with the
conveniences it offers on top. A member worked out from the others is one the reader can work out too, and
publishing it is one more thing to keep consistent for nothing. Which members those are is told apart by their
getter: the getter of a member a type carries is written by the compiler, and the getter of one it works out is
written by whoever wrote the type.

## What is yours

- **`nameOf`** — what a type is called in the document. A rule written against a name has to be rewritten the day
  the naming changes, and rewriting it is what hides that the change broke something, so no rule here names a
  schema: they look at relations instead, and ask you for the name of a type when they need to find its schema.
- **`isValueObject`** — which of your types is a value object. There is nothing in a type that says so.
- **`answers`** — whether a member is answered although it is worked out. A query result does work members out
  for whoever reads it, and those are part of the answer; each one is a decision written down where the documents
  are examined, not an accident.
- **the member that never travels** — named outright, for the reason the rule gives.

## What is not here

**A union and a hierarchy are not held to anything yet.** The rule about a declared value asks for a carrier, and
neither of those has one, so nothing checks that the `oneOf` of a document says what the declaration says. What
does cover them is the two rules that look at relations: a branch pointing at a schema the document does not have
is a dangling reference, and a schema nobody points at is unreferenced.

A document is also held to things that are of whoever serves it and cannot be stated here: that every document
it promises is generated, that every schema describes a type somebody declared, that a discriminator is named as
the application declares it. Each of those names something only that application has — who owns a name, which
of its parts a schema came from — so a rule written on them is a rule about one application, and it belongs
beside the documents it is about.
