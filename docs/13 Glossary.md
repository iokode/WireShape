# Glossary

The words this documentation uses in a sense of its own, by the package they belong to, each with the type that
stands for it and the chapter that says the rest.

## IOKode.WireShape

**Alternative** — one of the forms a value comes out as where it comes out as one of several, under a name of
yours that does not travel. `Alternative`. [Declaring a form](03%20Declaring%20a%20form.md#the-forms-there-are).

**Answer-only member** — a member the answer carries and a caller cannot give: no setter and no constructor
parameter. `Member.IsAnswerOnly`. [Asking the wire](06%20Asking%20the%20wire.md#what-a-type-carries).

**Carrier** — the type a value travels whole as, together with what a value of it looks like once written: a
kind of value, and a format where there is a settled name for one. `Carrier`.
[Declaring a form](03%20Declaring%20a%20form.md#what-a-carrier-is).

**Catalog** — a source of forms that keeps them written down somewhere, and so can be asked to name them all.
`IShapeCatalog`.
[Reading declarations from a source](04%20Reading%20declarations%20from%20a%20source.md#where-a-form-can-be-read-from).

**Convention** — a source of forms that recognizes a type when it is put in front of it and has no list behind
it. `IShapeSource`.
[Reading declarations from a source](04%20Reading%20declarations%20from%20a%20source.md#where-a-form-can-be-read-from).

**Declaration** — a form said of a type, together with who said it. `Declaration`.
[Declaring a form](03%20Declaring%20a%20form.md).

**Declarer** — one part of a wire, under a name of yours, through which everything that part declares is
declared. `Declarer`. [Declaring a form](03%20Declaring%20a%20form.md#who-declared-what).

**Discriminator** — the word that tells one kind apart from the other kinds of its base, inside what travels.
`Kind.Discriminator`, `HierarchyKind.Discriminator`. The member that word travels under is not a discriminator but
the [discriminator's member](#iokodewireshapeaspnetcore).
[Hierarchies and enumerations](05%20Hierarchies%20and%20enumerations.md#what-a-type-turns-out-to-be).

**Form** — what a value looks like once written: as a value, as a text, as another type, as one of several forms,
as a wrapper that disappears, as an object with these members, or as the content of a file. `Shape`, and one
nested record of it per form. [Declaring a form](03%20Declaring%20a%20form.md#the-forms-there-are).

**Kind** — what a base turns out to be: a type derived from it, with the discriminator that tells it apart inside
the base. `HierarchyKind`, and `Kind` where the base is already known.
[Hierarchies and enumerations](05%20Hierarchies%20and%20enumerations.md#what-a-type-turns-out-to-be).

**Needs its own name** — whether the meaning of a value lives nowhere but in its type, so that a document or a
client has to give the type a place of its own. `Structure.NeedsItsOwnName`.
[Asking the wire](06%20Asking%20the%20wire.md#whether-a-type-needs-a-name-of-its-own).

**Self-explanatory** — a carrier the reader's language has a kind of its own for, such as a date or an instant,
so that no name of ours goes over it. `Carrier.IsSelfExplanatory`.
[Declaring a form](03%20Declaring%20a%20form.md#what-a-carrier-is).

**Source** — whatever a form, a kind or a word can be read from instead of being declared one by one.
`IShapeSource`, `IHierarchySource`, `IEnumerationSource`.
[Reading declarations from a source](04%20Reading%20declarations%20from%20a%20source.md).

**Structure** — what a type comes out as, in a set that names every type it mentions: carried by another type,
an object, an enumeration, one of several forms, or a file. `Structure`.
[Asking the wire](06%20Asking%20the%20wire.md#what-each-type-comes-out-as).

**Travels** — is written on a wire. What travels is what a reader receives, which is not always the type.

**Wire** — one place where values are written: an API, a column, a socket, a file. Also what it answers: the
declarations made for it together with the writer. `Wire`, built by a `WireBuilder`.
[Asking the wire](06%20Asking%20the%20wire.md).

**Word** — what one value of an enumeration travels under, where the enumeration travels as words.
`EnumerationValueForm.Word`.
[Hierarchies and enumerations](05%20Hierarchies%20and%20enumerations.md#what-an-enumeration-comes-out-as).

**Wrapper that disappears** — a type that is not written at all, the type it wraps taking its place.
`Shape.Unwraps`. [Declaring a form](03%20Declaring%20a%20form.md#the-forms-there-are).

**The writer** — whatever turns a value into what travels: a serializer, a codec, a formatter. Everything a
declaration does not say is asked of it. `ISerializer`; `IOKode.WireShape.SystemTextJson` has the one for JSON,
`JsonSerializerOptionsSerializer`. [Asking the wire](06%20Asking%20the%20wire.md#what-the-writer-is-asked).

## IOKode.WireShape.ComponentModel

**Shape type converter** — the one converter that reads every declared value out of text and writes it back: the
writer of the wire reads and writes the text, told whether it is a text, a number or a truth, so that a route carries
the text a body does. `ShapeTypeConverter`.
[Reading a value from text](07%20Reading%20a%20value%20from%20text.md#what-it-reads).

**Teaching the descriptors** — telling the type descriptors of the process which converter reads a type out of
text. They belong to the whole process, so one wire teaches them, and it does so before anything reads a surface
or binds a value. `TypeDescriptors.Teach`.
[Reading a value from text](07%20Reading%20a%20value%20from%20text.md#what-has-to-happen-first).

## IOKode.WireShape.OpenApi

**Composite type** — a type that another declared type travels as, and so gets a schema of its own.
`Schemas.GetComposite`. [An OpenAPI document](08%20An%20OpenAPI%20document.md#what-a-declaration-becomes).

**Name of a type** — what a type is called in a document, which is yours and which nothing here decides. A
`Func<Type, string>`, passed as `nameOf`.
[An OpenAPI document](08%20An%20OpenAPI%20document.md#what-a-declaration-becomes).

**Named type** — a type whose schema goes under its own name: one whose carrier keeps its name, or one that comes
out as one of several forms. `Schemas.GetNamed`.
[An OpenAPI document](08%20An%20OpenAPI%20document.md#what-a-declaration-becomes).

**Projected type** — a type a form is written for in a document, read from the form and not from whether it names
a value. `Schemas.GetProjected`. [An OpenAPI document](08%20An%20OpenAPI%20document.md#what-a-declaration-becomes).

## IOKode.WireShape.Swashbuckle

**Projection** — writing the declared forms into the mappings the generator of documents reads, last and apart
from the filters. `Projection.Project`.
[A document through Swashbuckle](09%20A%20document%20through%20Swashbuckle.md#setting-it-up).

**Unwrapping** — the note carried from the filter that meets a wrapper to the one that takes it out of the
document: the schema id the wrapper went by and the type it wrapped. `Unwrapping`.
[A document through Swashbuckle](09%20A%20document%20through%20Swashbuckle.md#a-wrapper-that-does-not-travel).

## IOKode.WireShape.AspNetCore

**Action** — one call an application serves: its method, its route, what it takes and what it answers with.
`ApiAction`. [Reading what an application serves](10%20Reading%20what%20an%20application%20serves.md#the-calls).

**API model** — an API as data: its actions, what each type that travels in them comes out as, the kinds the
wire can name, and the discriminator's member. What a client is written from, with no application in front of it.
`ApiModel`. [Reading what an application serves](10%20Reading%20what%20an%20application%20serves.md#what-travels).

**Discriminator's member** — the member the [discriminator](#iokodewireshape) of a kind travels under, which is of
the whole API and not of one hierarchy. `ApiModel.Discriminator`; given to `Schemas.GetKindsSchema` and
`DeclaredKindsDocumentFilter` as `discriminator`, and written as the `propertyName` of an OpenAPI `discriminator`.
[Reading what an application serves](10%20Reading%20what%20an%20application%20serves.md#what-travels).

**Origin** — where a value of a call comes from: the route, the body, the query string or a form.
`ApiParameterOrigin`.
[Reading what an application serves](10%20Reading%20what%20an%20application%20serves.md#what-a-call-takes).

**Surface** — the actions of an assembly that belong together, read from the explorer of the application.
`ApiSurface`. [Reading what an application serves](10%20Reading%20what%20an%20application%20serves.md#the-calls).

## IOKode.WireShape.CSharpClient

**Reused type** — a type another client already declares, so that two clients of one application speak of one
type and not of two alike. `TypeNames.Reuse`. [A client of an API](11%20A%20client%20of%20an%20API.md#what-is-yours).

**Runtime of a client** — what every call needs, written beside the clients so that a client carries nothing but
itself. `ApiCallSource`. [A client of an API](11%20A%20client%20of%20an%20API.md#what-is-written-beside-it).

## IOKode.WireShape.Testing and IOKode.WireShape.OpenApi.Testing

**Rule** — something that has to hold, run in your own tests, answering with everything that breaks it. Of what a
wire is declared from, in `IOKode.WireShape.Testing`; of any document of a wire, in
`IOKode.WireShape.OpenApi.Testing`. `Invariants`, in each. [The rules](12%20The%20rules.md).
