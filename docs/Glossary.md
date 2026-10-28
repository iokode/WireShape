# Glossary

The words this documentation uses in a sense of its own, each with the type that stands for it and the chapter
that says the rest.

**Action** — one call an application serves: its method, its route, what it takes and what it answers with.
`ApiAction`. [Reading what an application serves](07%20Reading%20what%20an%20application%20serves.md).

**Alternative** — one of the forms a value comes out as where it comes out as one of several, under a name of
yours that does not travel. `Alternative`. [Declaring a form](03%20Declaring%20a%20form.md#the-forms-there-are).

**Answer-only member** — a member the answer carries and a caller cannot give: no setter and no constructor
parameter. `Member.IsAnswerOnly`. [Asking the wire](04%20Asking%20the%20wire.md#what-a-type-carries).

**API model** — an API as data: its actions, what each type that travels in them comes out as, and the kinds the
wire can name. What a client is written from, with no application in front of it. `ApiModel`.
[Reading what an application serves](07%20Reading%20what%20an%20application%20serves.md).

**Carrier** — the type a value travels whole as, together with what a value of it looks like once written: a
kind of value, and a format where there is a settled name for one. `Carrier`.
[Declaring a form](03%20Declaring%20a%20form.md#what-a-carrier-is).

**Catalog** — a source of forms that keeps them written down somewhere, and so can be asked to name them all.
`IShapeCatalog`. [Declaring a form](03%20Declaring%20a%20form.md#where-a-form-can-be-read-from).

**Convention** — a source of forms that recognizes a type when it is put in front of it and has no list behind
it. `IShapeSource`. [Declaring a form](03%20Declaring%20a%20form.md#where-a-form-can-be-read-from).

**Declaration** — a form said of a type, together with who said it. `Declaration`.
[Declaring a form](03%20Declaring%20a%20form.md).

**Declarer** — one part of a wire, under a name of yours, through which everything that part declares is
declared. `Declarer`. [Declaring a form](03%20Declaring%20a%20form.md#who-declared-what).

**Discriminator** — the member a kind's word travels under, which is of the whole API.
[What a type turns out to be](03%20Declaring%20a%20form.md#what-a-type-turns-out-to-be).

**Form** — what a value looks like once written: as a value, as a text, as another type, as one of several forms,
as a wrapper that disappears, as an object with these members, or as the content of a file. `Shape`, and one
nested record of it per form. [Declaring a form](03%20Declaring%20a%20form.md#the-forms-there-are).

**Kind** — what a base turns out to be: a type derived from it, with the word that tells it apart inside the
base. `HierarchyKind`, and `Kind` where the base is already known.
[Declaring a form](03%20Declaring%20a%20form.md#what-a-type-turns-out-to-be).

**Needs its own name** — whether the meaning of a value lives nowhere but in its type, so that a document or a
client has to give the type a place of its own. `Structure.NeedsItsOwnName`.
[Reading what an application serves](07%20Reading%20what%20an%20application%20serves.md#whether-a-type-needs-a-name-of-its-own).

**Rule** — something any document of a wire has to keep, answering with everything that breaks it. `Invariants`.
[What a document is held to](09%20What%20a%20document%20is%20held%20to.md).

**Self-explanatory** — a carrier the reader's language has a kind of its own for, such as a date or an instant,
so that no name of ours goes over it. `Carrier.IsSelfExplanatory`.
[Declaring a form](03%20Declaring%20a%20form.md#what-a-carrier-is).

**Source** — whatever a form, a kind or a word can be read from instead of being declared one by one.
`IShapeSource`, `IHierarchySource`, `IEnumerationSource`.
[Declaring a form](03%20Declaring%20a%20form.md#where-a-form-can-be-read-from).

**Structure** — what a type comes out as, in a set that names every type it mentions: carried by another type,
an object, an enumeration, one of several forms, or a file. `Structure`.
[Asking the wire](04%20Asking%20the%20wire.md#everything-that-travels-with-a-type).

**Surface** — the actions of an assembly that belong together, read from the explorer of the application.
`ApiSurface`. [Reading what an application serves](07%20Reading%20what%20an%20application%20serves.md#the-calls).

**Travels** — is written on a wire. What travels is what a reader receives, which is not always the type.

**Wire** — one place where values are written: an API, a column, a socket, a file. Also what it answers: the
declarations made for it together with the writer. `Wire`, built by a `WireBuilder`.
[Asking the wire](04%20Asking%20the%20wire.md).

**Word** — what one value of an enumeration travels under, where the enumeration travels as words.
`EnumerationValueForm.Word`. [Declaring a form](03%20Declaring%20a%20form.md#what-an-enumeration-comes-out-as).

**Wrapper that disappears** — a type that is not written at all, the type it wraps taking its place.
`Shape.Unwraps`. [Declaring a form](03%20Declaring%20a%20form.md#the-forms-there-are).

**The writer** — whatever turns a value into what travels: a serializer, a codec, a formatter. Everything a
declaration does not say is asked of it. `ISerializer`; for JSON, `JsonSerializerOptionsSerializer`.
[Asking the wire](04%20Asking%20the%20wire.md#what-the-writer-is-asked).
