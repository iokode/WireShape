# An OpenAPI document

Nothing in a document is written by hand. What a type looks like once written is read from where that is
declared, so the document, the client and the binder answer from one declaration. Everything here is of the
booking API of [getting started](02%20Getting%20started.md).

## What a declaration becomes

`Schemas` is the whole of it, and what it says of a value is read from the carrier of the declaration: **four
rows and not twenty**, one for each kind a value can be written as — a text, a whole number, a number, a truth —
with the carrier's own format beside it. So a `long` is an integer with the format `int64`, a `Guid` a string
with `uuid`, a `DateOnly` a string with `date`, and a carrier of your own whatever you declared it to be.

```csharp
Schemas.GetValueSchema(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
// {"type": "string", "pattern": "^[A-Z]{6}$"}
```

The pattern of the declaration is written beside it, which is what lets a text say which texts it is, and the
format is the carrier's, which is what lets a number say it is a count of minutes and not of euros.

Three questions are asked of the declarations as a whole:

- **`Projected`** — the types a form is written for, **read from the form and not from whether it names a
  value**: what travels as a value, as a text, as the content of a file, as another type of the wire, or as one
  of several forms. Less the enumerations, whose values are read one by one from the wire and said in a schema of
  their own; and less a wrapper that disappears, which leaves nothing to describe and is taken out of the
  document elsewhere.
- **`Composite`** — of those, the types another one travels as, which get a schema of their own because what
  they travel as has to be described somewhere. `MoneyParts` is there, because `Money` travels as it.
- **`Named`** — the ones whose carrier keeps its name and the ones that come out as one of several forms, each
  with the schema that describes it. A union is named for the same reason a value is: that these alternatives
  are one field is said in its schema and nowhere else.

`Schema` puts it together for one type: a reference to its own name where the name is what is left of it once the
value is written or where it comes out as one of several forms, the value itself where there is nothing for a
name to add, a text of bytes where it travels as the content of a file, and a reference to the other type's name
where it travels as another type of the wire.

Two more are written whole rather than said of one type. **`UnionSchema`** is the alternatives of a union, each
written as it would be written anywhere and under the `title` it was declared with — which is where a document
says what one branch of several is called, and what whoever generates from the document reads to name it on
their side. Nothing says which of them arrived, there being nothing in what travels that says it. And
**`KindsSchema`** is what a type turns out to be — one of its kinds, with the `discriminator` that names the
member the word travels under and the mapping from each word to its schema — for a wire that knows its kinds
where whoever writes it does not. Which member that is comes from whoever writes the document and from nothing in
the wire, as the names do.

```json
"reference": {"$ref": "#/components/schemas/Flights.BookingReference"}
```

```json
"Flights.BookingReference": {"type": "string", "pattern": "^[A-Z]{6}$"}
```

```json
"boarding": {"type": "string", "format": "date-time"}
```

```json
"total": {"$ref": "#/components/schemas/Flights.MoneyParts"}
```

```json
"passenger": {"$ref": "#/components/schemas/Flights.PassengerRef"}
```

```json
"Flights.PassengerRef": {"oneOf": [
  {"type": "string", "format": "uuid", "title": "id"},
  {"$ref": "#/components/schemas/Flights.Passenger", "title": "expanded"}
]}
```

A reference and a code are the same text, and a count of minutes and an amount are the same number, so with the
name gone there is nothing in the document that says which of them a member carries. A moment in time says what
it is already, and a name of ours over it would hide what the reader's own language calls an instant. And `Money`
is nowhere in the document, because what travels is the object of what it travels as.

**What a type is called is yours.** Every one of these takes a `Func<Type, string>` and none of them decides a
name. A name has to be unique in a document and the short name of a type is not, so whoever serves the document
knows something about that which this does not.

## What a document says about itself

`Documents` reads a whole document rather than one schema:

- **`GetSchemas`** — the schemas it describes, under the name each goes by.
- **`GetMembers`** — the members a schema offers, wherever it keeps them: a schema that says what it inherits
  keeps its own under the part that declares them.
- **`GetPointedAt`** — the name of every schema the document points at, wherever it points at it from: a parameter,
  a body, an answer, or another schema.
- **`Replace`** — puts another schema wherever one points at a pointer, as deep as the pointing goes.

## Through Swashbuckle

```csharp
// Declared wherever the declarations live, and built where whoever writes the wire can be had.
services.AddSingleton(provider =>
{
    var json = provider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

    return Flights.Declare().Build(new JsonSerializerOptionsSerializer(json));
});

services.AddSingleton<Unwrapping>();

services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(nameOf);
    options.SchemaFilter<DiscriminatorEnumSchemaFilter>();
    options.SchemaFilter<EnumerationSchemaFilter>();
    options.SchemaFilter<UnwrapSchemaFilter>(nameOf);
    options.SchemaFilter<NothingAtAllSchemaFilter>();
    options.DocumentFilter<DeclaredComponentsDocumentFilter>(nameOf);
    options.DocumentFilter<DeclaredKindsDocumentFilter>(nameOf, "kind");
    options.DocumentFilter<UnwrapDocumentFilter>();
});

// Last, and apart, because it has to be given the generator as everything else left it.
services.AddOptions<SwaggerGenOptions>()
    .Configure<Wire>((swagger, declared) => Projection.Project(swagger, declared, nameOf));
```

`nameOf` is a `Func<Type, string>` passed to the filters that need it; the rest is resolved from the container.
What each one does:

| Filter | What it says |
| --- | --- |
| `DiscriminatorEnumSchemaFilter` | Which words the member that tells one kind from another may hold, so the reader learns them from the member itself |
| `EnumerationSchemaFilter` | What the wire writes for each value of an enumeration, by writing them and looking at what came out |
| `UnwrapSchemaFilter` | Takes note of a type that disappears once written |
| `NothingAtAllSchemaFilter` | That a member which may be nothing may be nothing, where what it travels as is pointed at by name |
| `DeclaredComponentsDocumentFilter` | The schema of every declared type something in the document points at |
| `DeclaredKindsDocumentFilter` | What a type turns out to be, where the wire knows its kinds and whoever writes it does not |
| `UnwrapDocumentFilter` | Puts what a wrapper wrapped where the wrapper was, and takes the wrapper out |

`Projection.Project` writes the declared forms into the mappings the generator reads. It also says that
a number which comes from a `decimal` is one: a document that calls it a float tells whoever reads it that an
amount may arrive rounded. A declaration is written over whatever was there, including what somebody said by
hand, because the two said the same thing twice and this is the one that was read from where the form is
declared.

**A hierarchy comes to the document by one of two roads.** Where whoever writes knows it — `[JsonPolymorphic]`
and a `[JsonDerivedType]` per kind — the generator writes the `oneOf` and its `discriminator` itself, and what a
declaration adds is the words that member may hold. Where only the wire knows it, the generator writes the base
as a plain object, and a document that says a base is a plain object says nothing of what really travels: that is
what `DeclaredKindsDocumentFilter` is for, and it acts only on a base the document already describes and whose
schema has no discriminator on it.

## A member that may be nothing

```json
"boarding": {"anyOf": [{"$ref": "#/components/schemas/Flights.BoardingAt"}, {"type": "null"}]}
```

A reference has no room to say it: beside a reference nothing else is read, and what is left for saying it is to
offer the one thing or nothing at all. Without this, a member the wire writes as null is described as a member
that is always something.

## A wrapper that does not travel

A type declared `Unwraps` leaves nothing behind. The generator meets it while the schemas are written, and by
then things already point at the schema it got; a wrapper can only be taken out of the way once everything that
points at it has been written. So the two halves are two filters and `Unwrapping` is what carries the note
from one to the other: the schema id the wrapper went by, and the type it wrapped.

What the wrapper held is asked of the generator like anything else, so it is described as it would be anywhere
else, and the schema of a type met for the first time there is registered as usual.

## What is not in the document

- **A schema nobody reaches.** A declared type is described only where something in that document points at it.
  A schema nobody reaches is either residue of something that stopped being referenced or a type that walked in
  on its own.
- **A wrapper that disappears.** Either the value is there or the member is not.
- **A form nobody declared.** What the serializer writes as it stands is described by the generator from the
  type, as it always was.
- **A type declared with its members by hand.** `Members` is read by the model and by the binder, and the
  document is written by the generator from the type, as with anything undeclared.
