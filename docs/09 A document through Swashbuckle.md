# A document through Swashbuckle

**Package:** `IOKode.WireShape.Swashbuckle`

What a declaration becomes in a document is said by [an OpenAPI document](08%20An%20OpenAPI%20document.md). This is
what hangs it on the generation of documents of Swashbuckle, so that the document an application serves is written
from its wire. Everything here is of the booking API of [getting started](02%20Getting%20started.md).

## Setting it up

```csharp
// Declared wherever the declarations live, and built where the writer of the wire can be had.
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

`nameOf` is a `Func<Type, string>`, passed to the filters that need it; the rest is resolved from the container.
What each filter does:

| Filter | What it says |
| --- | --- |
| `DiscriminatorEnumSchemaFilter` | Which words the member that tells one kind from another may hold, so the reader learns them from the member itself |
| `EnumerationSchemaFilter` | What the wire writes for each value of an enumeration, found by writing them and looking at what came out |
| `UnwrapSchemaFilter` | Takes note of a type that disappears once written |
| `NothingAtAllSchemaFilter` | That a member which may be nothing may be nothing, where what it travels as is pointed at by name |
| `DeclaredComponentsDocumentFilter` | The schema of every declared type something in the document points at |
| `DeclaredKindsDocumentFilter` | What a type turns out to be, where the wire knows its kinds and its writer does not |
| `UnwrapDocumentFilter` | Puts what a wrapper wrapped where the wrapper was, and takes the wrapper out |

`Projection.Project` writes the declared forms into the mappings the generator reads. It also says that a number
which comes from a `decimal` is a decimal: a document that calls it a float tells whoever reads it that an amount
may arrive rounded.

A declaration is written over whatever was there, including what somebody said by hand. **Why:** the two said the
same thing twice, and this one was read from where the form is declared.

**A hierarchy comes to the document by one of two roads.**

- Where the writer knows it — `[JsonPolymorphic]` and a `[JsonDerivedType]` per kind — the generator writes the
  `oneOf` and its `discriminator` itself. What a declaration adds is the words that member may hold.
- Where only the wire knows it, the generator writes the base as a plain object, and a document that says a base is
  a plain object says nothing of what really travels. That is what `DeclaredKindsDocumentFilter` is for. It acts
  only on a base the document already describes, and only where its schema has no discriminator on it.

## A member that may be nothing

```json
"boarding": {"anyOf": [{"$ref": "#/components/schemas/Flights.BoardingAt"}, {"type": "null"}]}
```

A reference has no room to say it. Nothing beside a reference is read, so the only way left to say it is to offer
the one thing or nothing at all. Without this, a member the wire writes as null is described as a member that is
always something.

## A wrapper that does not travel

A type declared `Unwraps` leaves nothing behind. The generator meets it while the schemas are being written, and by
then things already point at the schema it got. A wrapper can only be taken out of the way once everything that
points at it has been written. So the two halves are two filters, and `Unwrapping` carries the note from one to
the other: the schema id the wrapper went by, and the type it wrapped.

What the wrapper held is asked of the generator like anything else. So it is described as it would be anywhere
else, and the schema of a type met for the first time there is registered as usual.

## What is not in the document

- **A schema nobody reaches.** A declared type is described only where something in that document points at it. A
  schema nobody reaches is either residue of something that stopped being referenced, or a type that walked in on
  its own.
- **A wrapper that disappears.** Either the value is there or the member is not.
- **A form nobody declared.** What the writer writes as it stands is described by the generator from the type, as
  it always was.
- **A type declared with its members by hand.** `Members` is read by the model and by the binder. The document is
  written by the generator from the type, as with anything undeclared.
