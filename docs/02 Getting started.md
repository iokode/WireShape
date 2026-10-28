# Getting started

The example every chapter after this one is written on is an airline's booking API. A booking has a reference, a
passenger, its segments, what it cost and when boarding opens. None of those travels as the type it is.

The chapters show the parts of the example each one is about. The whole of it — every type, every converter and
the declarations together — is in
[`Flights.cs`](https://github.com/iokode/WireShape/blob/master/src/Tests.WireShape.Documentation/Flights.cs).

Two words are used from here to the end, and they are defined once:

- **the writer** of a wire is whatever turns a value into what travels: a serializer, a codec, a formatter.
  Everything a declaration does not say is asked of it, and there are four such things;
  [asking the wire](06%20Asking%20the%20wire.md) says which. For an application that serves JSON, the writer is a
  `JsonSerializerOptions`, read through `JsonSerializerOptionsSerializer`.
- **a carrier** is the type a value travels whole as, together with what a value of that type looks like once
  written. `Carrier.Guid` is a GUID, written as a text under the format `uuid`. The twenty that every writer writes
  whole are in the library already, and a wire that needs another declares one;
  [declaring a form](03%20Declaring%20a%20form.md) says how.

Every other word used in a sense of its own is in the [glossary](13%20Glossary.md).

## What to install

Every package depends on `IOKode.WireShape`, and on nothing of this library beyond what its row says. Install the
ones for what you do:

| To | Install | Read |
| --- | --- | --- |
| Declare forms and ask a wire about them | `IOKode.WireShape` | [Declaring a form](03%20Declaring%20a%20form.md), [reading declarations from a source](04%20Reading%20declarations%20from%20a%20source.md), [hierarchies and enumerations](05%20Hierarchies%20and%20enumerations.md), [asking the wire](06%20Asking%20the%20wire.md) |
| Ask `System.Text.Json` what a declaration does not say | `IOKode.WireShape.SystemTextJson` | [Asking the wire](06%20Asking%20the%20wire.md#through-systemtextjson) |
| Read a declared value out of a route or a query string | `IOKode.WireShape.ComponentModel` | [Reading a value from text](07%20Reading%20a%20value%20from%20text.md) |
| Say what a declaration becomes in an OpenAPI document | `IOKode.WireShape.OpenApi` | [An OpenAPI document](08%20An%20OpenAPI%20document.md) |
| Write it into the documents Swashbuckle generates | `IOKode.WireShape.Swashbuckle`, which brings `OpenApi` | [A document through Swashbuckle](09%20A%20document%20through%20Swashbuckle.md) |
| Read the calls an ASP.NET Core application serves, as a model of its API | `IOKode.WireShape.AspNetCore`, which brings `ComponentModel` | [Reading what an application serves](10%20Reading%20what%20an%20application%20serves.md) |
| Write a C# client of that model | `IOKode.WireShape.CSharpClient`, which brings `AspNetCore` | [A client of an API](11%20A%20client%20of%20an%20API.md) |
| Hold what a wire is declared from to its rules, in your tests | `IOKode.WireShape.Testing` | [The rules](12%20The%20rules.md#what-a-wire-is-declared-from) |
| Hold a document to its rules, in your tests | `IOKode.WireShape.OpenApi.Testing`, which brings `OpenApi` | [The rules](12%20The%20rules.md#what-a-document-is-held-to) |

## Declare what travels

Declare a form for each type that **a converter of its own writes**. Nobody can be asked what those look like
once written. Everything else is answered by the writer.

```csharp
var declaring = new WireBuilder();

// Who declares. Everything declared through it is declared by it, and a wire of several parts has one each.
var bookings = declaring.GetDeclarer("bookings");

// Values a converter of their own writes, which is why they are declared at all.
bookings.Add<BookingReference>(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
bookings.Add<PassengerId>(new Shape.As(Carrier.Guid));
bookings.Add<FlightDuration>(new Shape.As(Carrier.Int64));
bookings.Add<DepartsOn>(new Shape.As(Carrier.DateOnly));

// A moment in time of its own: the reader's language has a kind for it, so no name of ours goes over it.
bookings.Add<BoardingAt>(new Shape.AsText(Format: "date-time", IsSelfExplanatory: true));

// It travels as another type of the wire, so the document points at that type and the name Money is not in it.
bookings.Add<Money>(new Shape.AsAnother(typeof(MoneyParts)));

// A family a convention recognizes, which cannot be listed one by one.
bookings.AddShapeSource(new AmendedWireShapes());

// The content of a file, which in an application is whatever its framework calls one.
bookings.Add<BoardingPassFile>(new Shape.AsFile());

// One of several forms, told apart by what arrived: the identifier, or the passenger expanded.
bookings.Add<PassengerRef>(new Shape.OneOf(new[]
{
    new Alternative("id", new Shape.As(Carrier.Guid)),
    new Alternative("expanded", new Shape.AsAnother(typeof(Passenger)))
}));

// What a segment turns out to be, one fact at a time.
bookings.AddKind<Segment, FlightSegment>("flight");
bookings.AddKind<Segment, TrainSegment>("train");

// The word a value of an enumeration travels under, one fact at a time for the same reason.
bookings.AddWord(CabinClass.Economy, "economy");
bookings.AddWord(CabinClass.Business, "business");
```

Read it once more by what each line is for, because the whole of this library is in that list:

| | Why it is declared, and what follows |
| --- | --- |
| `BookingReference` | A text, and these are the texts. Once `"QX7F2P"` is written, the name is all that is left of what it meant, so it keeps one: a schema of its own, a type of its own in a client |
| `PassengerId`, `FlightDuration` | A GUID and a number. Any GUID is any GUID and any number any number, so they keep their names too |
| `DepartsOn`, `BoardingAt` | A date and a moment. **The reader's language has a kind for these already**, so no name of ours goes over them: they are written where they are used |
| `Money` | An amount and a currency are one thing, and they travel as one object. The document points at **that** object, and `Money` is not in it |
| `Amended<T>` | A change that may leave a field out. It disappears once written, and what it wrapped takes its place |
| `BoardingPassFile` | It travels as content. The writer would answer that it is an object of six members, which is not what travels |
| `PassengerRef` | One of several forms: an identifier or the passenger. Which one arrived is told by **what arrived** |
| `Segment` | A hierarchy, declared kind by kind, because nobody holding a base knows every type that derives from it |
| `CabinClass` | The word each value travels under, one at a time, for the same reason |

Then build the wire from the declarations and the writer:

```csharp
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
};

options.Converters.Add(new BookingReferenceJsonConverter());
options.Converters.Add(new MoneyJsonConverter());
// … one per value a converter of its own writes

Wire wire = declaring.Build(new JsonSerializerOptionsSerializer(options));
```

**Nothing can be declared on a wire, and nothing can be read from a builder.** Building copies what was declared,
so what the wire says of a type cannot change under whoever is reading it.
[Declaring a form](03%20Declaring%20a%20form.md) says the rest.

## Ask it

```csharp
wire.GetShape(typeof(BookingReference));          // a text, with that pattern
wire.GetMembers(typeof(Booking));                 // its members, named as they travel
wire.GetKinds(typeof(Segment));                   // (FlightSegment, "flight"), (TrainSegment, "train")
wire.TravelsByName(typeof(CabinClass));           // true
wire.GetValueForm(typeof(CabinClass), CabinClass.Economy);   // Word("economy")
```

What a declaration does not say is asked of the writer, so what the wire says and what is written cannot come
apart. [Asking the wire](06%20Asking%20the%20wire.md) says which four things only the writer knows. It also says
why the kinds and the words are the two answers where the declarations and the writer are taken **together**.

## Everything that travels with a booking

```csharp
IReadOnlyDictionary<Type, Structure> traveling = wire.GetStructures(new[] {typeof(Booking)});
```

The answer has a row per type, saying what that type comes out as. It also has a row for **every type those rows
mention in turn**, so the answer closes. `MoneyParts` is in it, although only `Money` mentions it, and so are
`FlightSegment` and `TrainSegment`, which no call names. A type that neither a declaration nor the writer can say
anything about **refuses the whole answer**, by name, instead of being handed over as an object with no members.

## Serve a document of it

```csharp
services.AddSingleton(provider => Flights.Declare().Build(new JsonSerializerOptionsSerializer(
    provider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions)));

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

services.AddOptions<SwaggerGenOptions>()
    .Configure<Wire>((swagger, wire) => Projection.Project(swagger, wire, nameOf));
```

What comes out:

- `BookingReference` gets a schema of its own, with its pattern;
- `BoardingAt` is a `date-time`, written where it is used;
- a member of type `Money` points at `MoneyParts`;
- `PassengerRef` is a `oneOf` of two branches, under the names they were declared with;
- a boarding pass is a text of bytes.

`nameOf` is a `Func<Type, string>`, and it is yours: nothing here decides what a schema is called.
[An OpenAPI document](08%20An%20OpenAPI%20document.md) and
[a document through Swashbuckle](09%20A%20document%20through%20Swashbuckle.md) say the rest.

## Read one out of a route

A route carries text, and `/bookings/QX7F2P` has to become a `BookingReference` before anything in the
application sees it:

```csharp
foreach (var (type, converter) in TypeDescriptors.GetConverters(wire))
{
    TypeDescriptors.Teach(type, converter);
}
```

One converter serves every value of the wire that can be read from text at all.
[Reading a value from text](07%20Reading%20a%20value%20from%20text.md) says the rest.

## Write a client of it

```csharp
var actions = new ApiSurface(
    typeof(BookingsController).Assembly,
    options => options.Conventions.Add(new FlightsRouteConvention()),
    method => method.GetCustomAttribute<ServedAttribute>() != null).GetActions();

var mentioned = actions.SelectMany(action => action.Parameters
    .Select(parameter => (Type?) parameter.Type)
    .Append(action.Result));

var model = new ApiModel(actions, wire.GetStructures(mentioned), wire.GetDeclaredKinds().ToArray(), "kind");

File.WriteAllText(
    path,
    ClientSource.Write(model, "Flights.Client", controller => controller + "Client"));
```

And this is what the caller of that client writes:

```csharp
var booking = await bookings.GetAsync(new BookingReference("QX7F2P"));

var shown = booking.Passenger.Kind switch
{
    PassengerRefKind.Id => booking.Passenger.AsId.ToString(),
    PassengerRefKind.Expanded => booking.Passenger.AsExpanded.Name
};
```

Nothing in that client works out what travels. It was written from the model, and the model was answered by the
wire. [Reading what an application serves](10%20Reading%20what%20an%20application%20serves.md) and
[A client of an API](11%20A%20client%20of%20an%20API.md) say the rest.

## And hold the document to it

```csharp
var broken = new List<string>();
broken.AddRange(await Invariants.FindUnreferencedSchemasAsync(name, document));
broken.AddRange(await Invariants.FindDanglingReferencesAsync(name, document));
broken.AddRange(Invariants.FindNamedValuesDescribedOtherwise(name, document, wire, nameOf));

Assert.Empty(broken);
```

[The rules](12%20The%20rules.md) says the rest.
