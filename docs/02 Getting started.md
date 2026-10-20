# Getting started

An airline's booking API, which is the example every chapter after this one is written on. A booking has a
reference, a passenger, its segments, what it cost and when boarding opens. None of those travels as the type it
is.

Everything below is code that compiles and is run by the tests of this repository, under
`src/Tests.WireShape.Documentation`. What the chapters show is what those tests hold to.

Two words are used from here to the end, and they are said once:

- **whoever writes** the wire is whatever turns a value into what travels: a serializer, a codec, a formatter.
  Everything a declaration does not say is asked of it, and there are four such things —
  [asking the wire](04%20Asking%20the%20wire.md) says which. For an application that serves JSON it is a
  `JsonSerializerOptions`, read through `JsonSerializerOptionsSerializer`.
- **a carrier** is the type a value travels whole as, together with what a value of that type looks like once
  written: `Carrier.Guid` is a GUID, written as a text under the format `uuid`. The twenty every writer writes
  whole are in the library already, and a wire that needs another declares one —
  [declaring a form](03%20Declaring%20a%20form.md) says how.

## Declare what travels

A form is declared of a type **a converter of its own writes**, because that is the one whose written shape
nobody can be asked about. Everything else is answered by whoever writes the wire.

```csharp
var declaring = new WireBuilder();

// Values a converter of their own writes, which is why they are declared at all.
declaring.Add<BookingReference>(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
declaring.Add<PassengerId>(new Shape.As(Carrier.Guid));
declaring.Add<FlightDuration>(new Shape.As(Carrier.Int64));
declaring.Add<DepartsOn>(new Shape.As(Carrier.DateOnly));

// A moment in time of its own: the reader's language has a kind for it, so no name of ours goes over it.
declaring.Add<BoardingAt>(new Shape.AsText(Format: "date-time", IsSelfExplanatory: true));

// It travels as another type of the wire, so the document points at that type and the name Money is not in it.
declaring.Add<Money>(new Shape.AsAnother(typeof(MoneyParts)));

// A family a rule recognises, which cannot be listed one by one.
declaring.Add(
    type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Amended<>),
    _ => new Shape.Unwraps(0));

// The content of a file, which in an application is whatever its framework calls one.
declaring.Add<BoardingPassFile>(new Shape.AsFile());

// One of several forms, told apart by what arrived: the identifier, or the passenger expanded.
declaring.Add<PassengerRef>(new Shape.OneOf(new[]
{
    new Alternative("id", new Shape.As(Carrier.Guid)),
    new Alternative("expanded", new Shape.AsAnother(typeof(Passenger)))
}));

// What a segment turns out to be, one fact at a time.
declaring.AddKind<Segment, FlightSegment>("flight");
declaring.AddKind<Segment, TrainSegment>("train");

// The word a value of an enumeration travels under, one fact at a time for the same reason.
declaring.AddWord(CabinClass.Economy, "economy");
declaring.AddWord(CabinClass.Business, "business");
```

Read it once more by what each line is for, because the whole of this library is in that list:

| | Why it is declared, and what follows |
| --- | --- |
| `BookingReference` | A text, and these are the texts. The name is all that is left once `"QX7F2P"` is written, so it keeps one: a schema of its own, a type of its own in a client |
| `PassengerId`, `FlightDuration` | A GUID and a number. Any GUID is any GUID and any number any number, so they keep their names too |
| `DepartsOn`, `BoardingAt` | A date and a moment. **The reader's language has a kind for these already**, so no name of ours goes over them: they are written where they are used |
| `Money` | An amount and a currency are one thing, and they travel as one object. The document points at **that** object and `Money` is not in it |
| `Amended<T>` | A change that may leave a field out. It disappears once written and what it wrapped takes its place |
| `BoardingPassFile` | It travels as content, not as an object of six members, which is what whoever writes would answer about it |
| `PassengerRef` | One of several forms: an identifier or the passenger. Which one arrived is told by **what arrived** |
| `Segment` | A hierarchy, declared kind by kind, because nobody holding a base knows every type that derives from it |
| `CabinClass` | The word each value travels under, one at a time, for the same reason |

Then the wire is made of the declarations and of whoever writes:

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

**Nothing can be declared on a wire and nothing can be read from a builder.** What was declared is copied when it
is built, so what the wire says of a type cannot change under whoever is reading it.
[Declaring a form](03%20Declaring%20a%20form.md) says the rest.

## Ask it

```csharp
wire.GetShape(typeof(BookingReference));          // a text, with that pattern
wire.GetMembers(typeof(Booking));                 // its members, named as they travel
wire.GetKinds(typeof(Segment));                   // (FlightSegment, "flight"), (TrainSegment, "train")
wire.TravelsByName(typeof(CabinClass));           // true
wire.GetValueForm(typeof(CabinClass), CabinClass.Economy);   // Word("economy")
```

What a declaration does not say is asked of whoever writes, so what the wire says and what is written cannot come
apart. [Asking the wire](04%20Asking%20the%20wire.md) says which four things only whoever writes knows, and why
the kinds and the words are the two answers where the declarations and the writer are taken **together**.

## Everything that travels with a booking

```csharp
IReadOnlyDictionary<Type, Structure> travelling = wire.GetStructures(new[] {typeof(Booking)});
```

A row per type and what that type comes out as — and **every type those mention in turn**, so the answer closes:
`MoneyParts` is in it although only `Money` mentions it, and so are `FlightSegment` and `TrainSegment`, which no
call names. A type neither a declaration nor the writer could say anything about **refuses the whole answer** by
name, rather than being handed over as an object with no members.

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

What comes out: `BookingReference` a schema of its own with its pattern, `BoardingAt` a `date-time` written where
it is used, a member of type `Money` pointing at `MoneyParts`, `PassengerRef` a `oneOf` of two branches under the
names they were declared with, and a boarding pass a text of bytes. `nameOf` is a `Func<Type, string>` and it is
yours: nothing here decides what a schema is called. [An OpenAPI document](06%20An%20OpenAPI%20document.md).

## Read one out of a route

A route carries text, and `/bookings/QX7F2P` has to become a `BookingReference` before anything of the
application sees it:

```csharp
foreach (var (type, converter) in TypeDescriptors.GetConverters(wire))
{
    TypeDescriptors.Teach(type, converter);
}
```

One converter serves every value of the wire that can be read from text at all.
[Reading a value from text](05%20Reading%20a%20value%20from%20text.md).

## Write a client of it

```csharp
var actions = new ApiSurface(
    typeof(BookingsController).Assembly,
    options => options.Conventions.Add(new FlightsRouteConvention()),
    method => method.GetCustomAttribute<ServedAttribute>() != null,
    method => null).GetActions();

var mentioned = actions.SelectMany(action => action.Parameters
    .Select(parameter => (Type?) parameter.Type)
    .Append(action.Result));

var model = new ApiModel(actions, wire.GetStructures(mentioned), wire.GetDeclaredKinds().ToArray(), "kind");

File.WriteAllText(
    path,
    ClientSource.Write(model, "Flights.Client", controller => controller + "Client"));
```

And what the caller of that client writes:

```csharp
var booking = await bookings.GetAsync(new BookingReference("QX7F2P"));

var shown = booking.Passenger.Kind switch
{
    PassengerRefKind.Id => booking.Passenger.AsId.ToString(),
    PassengerRefKind.Expanded => booking.Passenger.AsExpanded.Name
};
```

Nothing in that client works out what travels: it was written from the model, which was answered by the wire.
[Reading what an application serves](07%20Reading%20what%20an%20application%20serves.md) and
[A client of an API](08%20A%20client%20of%20an%20API.md).

## And hold the document to it

```csharp
var broken = new List<string>();
broken.AddRange(await Invariants.FindUnreferencedSchemasAsync(name, document));
broken.AddRange(await Invariants.FindDanglingReferencesAsync(name, document));
broken.AddRange(Invariants.FindNamedValuesDescribedOtherwise(name, document, wire, nameOf));

Assert.Empty(broken);
```

[What a document is held to](09%20What%20a%20document%20is%20held%20to.md).
