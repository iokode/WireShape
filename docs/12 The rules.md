# The rules

**Packages:** `IOKode.WireShape.Testing` for what a wire is declared from, and `IOKode.WireShape.OpenApi.Testing`
for a document of one

These are rules to run in your own tests. Each rule answers with everything that breaks it, so a run reports all of
it instead of stopping at the first thing, and each answer names what is wrong. None of them asserts a name: they
look at relations instead, so that a change of naming does not mean rewriting them.

Both packages call their class `Invariants`, in `IOKode.WireShape.Testing` and `IOKode.WireShape.OpenApi.Testing`.
A test file that runs rules of both names one of them through an alias.

## What a wire is declared from

These hold a source to what being one says about it, which nothing else can. A wire answers, and an answer is right
or wrong on its own terms; what is wrong here is a source that contradicts itself, and a wire built on one answers
the two questions about a type differently without anybody being able to tell.

```csharp
var broken = Invariants.FindWhatACatalogDidNotName(
    new ConverterShapes(options),
    new JsonSerializerOptionsSerializer(options),
    new[] {typeof(Booking).Assembly});

Assert.True(broken.Count == 0, string.Join(Environment.NewLine, broken));
```

**`FindWhatACatalogDidNotName`** — a catalog names every type it answers about. Being a catalog rather than a plain
source says there is a list behind it, and this holds that statement to what the source does. What a catalog names
is among the declarations, which is what everything that walks them reads: the type descriptors, the schemas of a
document, the types a client carries. A type it answers about without naming it is seen by none of those, so it is
described by the writer instead of by the form it was declared to travel in, and every rule that walks the
declarations passes, the type not being among them to check.

It asks the catalog about the types of the assemblies it is given, and not about what the catalog names, because a
catalog that names too few cannot be asked which those are: that is the fault being looked for.
[Reading declarations from a source](04%20Reading%20declarations%20from%20a%20source.md#where-a-form-can-be-read-from)
says what a catalog is.

## What a document is held to

These hold of any document that describes a wire, whatever that document describes.

Run them over every document you serve:

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

### Each rule

**`FindUnreferencedSchemasAsync`** — nothing is described that nothing points at. A schema nobody reaches is
either residue of something that stopped being referenced, or a type that walked into the document on its own.

**`FindDanglingReferencesAsync`** — nothing points at what is not described. A reference to a schema the document
does not have leaves whoever reads it with a member whose form they cannot know.

**`FindSchemasCarrying`** — no schema offers a member that never travels. Which member that is depends on what the
wire is made of, so you name it. A convention can be asked whether it recognizes a type, but never to name the
types it recognizes, so the family `Amended<>` belongs to cannot be enumerated, and the member it would have
brought with it is named here instead.

**`FindEnumerationsDescribedOtherwise`** — an enumeration is described as the wire writes it. Whether `CabinClass`
travels as a word or as a number is asked of the wire, which writes a value to find out, because what decides it
is a converter and not the type.

**`FindNamedValuesDescribedOtherwise`** — a type declared to travel as a value is described as that value, under
its own name, with the format and the pattern it was declared with. A `BookingReference` described as something
with members is a declaration the document did not keep.

**`FindValueObjectsOfferingWhatIsWorkedOut`** — a value object travels as the data it carries, not with the
conveniences it offers on top. A member worked out from the others is one the reader can work out too, and
publishing it is one more thing to keep consistent for nothing. Those members are told apart by their getter: the
getter of a member a type carries is written by the compiler, and the getter of one it works out is written by
whoever wrote the type.

### What is yours

- **`nameOf`** — what a type is called in the document. No rule here names a schema: they look at relations
  instead, and ask you for the name of a type when they need to find its schema.
  **Why:** a rule written against a name has to be rewritten whenever the naming changes, and rewriting it is
  what hides that the change broke something.
- **`isValueObject`** — which of your types is a value object. Nothing in a type says so.
- **`answers`** — whether a member is answered although it is worked out. A query result does work members out for
  whoever reads it, and those are part of the answer. Each one is a decision written down where the documents are
  examined, not an accident.
- **the member that never travels** — named outright, for the reason its rule gives.

### What is not here

**A union and a hierarchy are not held to anything yet.** The rule about a declared value asks for a carrier, and
neither of those has one, so nothing checks that the `oneOf` of a document says what the declaration says. What
does cover them is the two rules that look at relations: a branch pointing at a schema the document does not have
is a dangling reference, and a schema nobody points at is unreferenced.

A document is also held to things that belong to whoever serves it and cannot be stated here: that every document
it promises is generated, that every schema describes a type somebody declared, that the member a kind's word
travels under is named as the application declares it. Each of those names something only that application has,
such as who owns a name or which of its parts a schema came from. So a rule written on them is a rule about one
application, and it belongs beside the documents it is about.
