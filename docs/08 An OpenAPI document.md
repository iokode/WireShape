# An OpenAPI document

**Package:** `IOKode.WireShape.OpenApi`

Nothing in a document is written by hand. What a type looks like once written is read from where it is declared,
so the document, the client and the binder all answer from one declaration. Everything here is of the booking API
of [getting started](02%20Getting%20started.md).

## What a declaration becomes

`Schemas` is the whole of it. What it says of a value is read from the carrier of the declaration: **four rows and
not twenty**, one for each kind a value can be written as — a text, a whole number, a number, a truth — with the
carrier's own format beside it. So a `long` is an integer with the format `int64`, a `Guid` a string with `uuid`,
a `DateOnly` a string with `date`, and a carrier of your own whatever you declared it to be.

```csharp
Schemas.GetValueSchema(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
// {"type": "string", "pattern": "^[A-Z]{6}$"}
```

The pattern of the declaration is written beside it, which lets a text say which texts it is. The format is the
carrier's, which says what kind of text or number it is.

Three questions are asked of the declarations as a whole:

- **`GetProjected`** — the types a form is written for, **read from the form, not from whether it names a value**:
  what travels as a value, as a text, as the content of a file, as another type of the wire, or as one of several
  forms. Two are left out. The enumerations are left out, because their values are read one by one from the wire
  and said in a schema of their own. A wrapper that disappears is left out, because it leaves nothing to describe
  and is taken out of the document elsewhere.
- **`GetComposite`** — of those, the types another one travels as. They get a schema of their own, because what
  they travel as has to be described somewhere. `MoneyParts` is there, because `Money` travels as it.
- **`GetNamed`** — the ones whose carrier keeps its name, and the ones that come out as one of several forms, each
  with the schema that describes it. A union is named for the same reason a value is: that these alternatives are
  one field is said in its schema and nowhere else.

`GetSchema` puts it together for one type:

- a reference to its own name, where the name is what is left of it once the value is written, or where it comes
  out as one of several forms;
- the value itself, where a name has nothing to add;
- a text of bytes, where it travels as the content of a file;
- a reference to the other type's name, where it travels as another type of the wire.

Two more are written whole instead of being said of one type.

**`GetUnionSchema`** is the alternatives of a union. Each is written as it would be written anywhere, under the
`title` it was declared with. The `title` is where a document says what one branch of several is called, and it is
what whoever generates code from the document reads to name the branch on their side. Nothing says which of them
arrived, because nothing in what travels says it.

**`GetKindsSchema`** is what a type turns out to be: one of its kinds, with the `discriminator` that names the
member the word travels under, and the mapping from each word to its schema. It is for a wire that knows its kinds
where its writer does not. Which member that is comes from whoever writes the document, as the names do, and not
from anything in the wire.

Two are pieces of the others, for whoever puts a document together by hand. **`GetFileSchema`** is the content of
a file on its own, `{"type": "string", "format": "binary"}`; what the file is called and what it carries travel
beside it, not in the schema. **`GetDiscriminatorSchema`** is the member one kind is told apart by: a text that
holds that kind's word and no other, so that whoever reads the document knows which word to send.

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

A reference and a code are the same text, and a count of minutes and an amount are the same number. With the name
gone, nothing in the document says which of them a member carries. A moment in time already says what it is, and
a name of ours over it would hide what the reader's own language calls an instant. And `Money` is nowhere in the
document, because what travels is the object of the type it travels as.

**What a type is called is yours.** Every one of these takes a `Func<Type, string>`, and none of them decides a
name. A name has to be unique in a document and the short name of a type is not, so whoever serves the document
knows something about names that this library does not.

## What a document says about itself

`Documents` reads a whole document instead of one schema:

- **`GetSchemas`** — the schemas it describes, under the name each goes by.
- **`GetMembers`** — the members a schema offers, wherever it keeps them. A schema that says what it inherits keeps
  its own members under the part that declares them.
- **`GetPointedAt`** — the name of every schema the document points at, wherever it points at it from: a
  parameter, a body, an answer, or another schema.
- **`Replace`** — goes through a schema and, wherever it holds a reference to one of the names it is given, puts
  the schema given for that name in its place, as deep as the references go.

## Putting it in a document

Nothing in this package generates a document. It says what each declaration becomes, for whatever puts the document
together. [A document through Swashbuckle](09%20A%20document%20through%20Swashbuckle.md) is the one that ships.
