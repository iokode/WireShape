# WireShape

**The written form of a value, declared once, as data a program can read.**

What a value looks like once written is settled by the code of a converter, and there is nowhere to read it from.
A `BookingReference` is a record holding a string and travels as `"QX7F2P"`. `Money` holds an amount and a
currency and travels as an object of two members. `Amended<T>` wraps a value and travels as the value, or does
not travel at all. Each of those is a statement inside a method of whatever writes them — a serializer, a codec,
a formatter — and a statement can be executed and not read: there is no property to read it off, no attribute
that states it and no table to look it up in.

Writing is not the only thing done with a form. The same form is described to whoever will read, declared to
whoever will call, and read back out of places that carry no types at all: a route, a column, a field of a form,
a line of a file. Each of those is written by somebody who has to know what a value of that type looks like once
written, and since there is nothing to ask, each works it out again from the type — and the type is not what
travels.

What that costs, where it is paid:

- a document says `BookingReference` is an object with one member, and whoever reads it sends
  `{"value": "QX7F2P"}` where `"QX7F2P"` was expected
- a generated client declares a class where a text is wanted, and the caller writes code against something the
  API never accepts
- the binder takes a value for something with members, cannot read it from a route, and leaves nothing behind but
  the hole in the route

Here it is declared once and the rest ask:

```csharp
var declaring = new WireBuilder();
Declarer bookings = declaring.GetDeclarer("bookings");

bookings.Add<BookingReference>(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
bookings.Add<Money>(new Shape.AsAnother(typeof(MoneyParts)));
bookings.AddKind<Segment, FlightSegment>("flight");
bookings.AddWord(CabinClass.Economy, "economy");

Wire wire = declaring.Build(new JsonSerializerOptionsSerializer(options));
```

A wire is one place where values are written: an API, a column, a socket, a file, a sheet of paper, a key tapped
in morse. Each wire declares its own forms, and nothing here makes two of them agree, because they describe
different things.

## What is covered

A declaration does not say everything there is to know about a wire, and the rest is answered by whoever writes
it, through `ISerializer`: the members of a type as they travel, what a type turns out to be, what an enumeration
value comes out as, and a value turned into what carries it and back. **Nothing in the base package names a
serializer.** `IOKode.WireShape.SystemTextJson` answers those questions from a `JsonSerializerOptions`, and
anything else — a codec, a formatter, a serializer of your own — is the same questions answered somewhere else.

Of the wires there could be, what ships covers one: the HTTP API of an ASP.NET Core application. Its documents,
the client that calls it, and the values it reads out of a route or a query string.

## The packages

```
WireShape ─┬─ WireShape.SystemTextJson
           ├─ WireShape.ComponentModel ── WireShape.AspNetCore ── WireShape.CSharpClient
           └─ WireShape.OpenApi ─┬─ WireShape.Swashbuckle
                                 └─ WireShape.OpenApi.Testing
```

| Package | What it is for |
| --- | --- |
| `IOKode.WireShape` | The words a form is declared in, what declares a wire and what a wire answers, and what a type comes out as |
| `IOKode.WireShape.SystemTextJson` | What a wire written with `JsonSerializerOptions` answers about itself |
| `IOKode.WireShape.ComponentModel` | Reading a declared value out of text and writing it back, through the type converters of the framework |
| `IOKode.WireShape.OpenApi` | What a declaration becomes in an OpenAPI document, and what a whole document says about its own schemas |
| `IOKode.WireShape.Swashbuckle` | Hanging all of that on the generation of documents of Swashbuckle |
| `IOKode.WireShape.AspNetCore` | Reading the calls an application serves, and the model of an API they make up with the types |
| `IOKode.WireShape.CSharpClient` | Writing a C# client of that model, which is one shape of client among the ones there could be |
| `IOKode.WireShape.OpenApi.Testing` | The rules any document of a wire has to keep, to run over your own documents |

Every package reads the declarations and none of them decides anything about your types. Where a decision is
yours, it is asked for: what a type is called in a document is a `Func<Type, string>`, what writes the wire is an
`ISerializer`, where the kinds of a hierarchy are read from is an `IHierarchySource`, which calls are of a
surface is a `Func<MethodInfo, bool>`, what a client is called is a `Func<string, string>`.

## The chapters

They are written on one example, an airline's booking API, which is declared in full in the second of them and
compiled and tested in `src/Tests.WireShape.Documentation`.

1. [The problem](docs/01%20The%20problem.md) — the form of a value is a statement inside a method and there is
   nowhere to read it from, and the four places that each work it out again and each get it wrong in their own
   way.
2. [Getting started](docs/02%20Getting%20started.md) — the example declared whole, and with it a document, a
   value read out of a route and a client written.
3. [Declaring a form](docs/03%20Declaring%20a%20form.md) — the forms there are, what a carrier is, how a
   hierarchy and an enumeration are declared, and where either can be read from without anyone declaring it.
4. [Asking the wire](docs/04%20Asking%20the%20wire.md) — the four things only whoever writes knows, and
   everything that travels with a type.
5. [Reading a value from text](docs/05%20Reading%20a%20value%20from%20text.md) — a route and a query string carry
   text, and this is what reads a declared value out of it.
6. [An OpenAPI document](docs/06%20An%20OpenAPI%20document.md) — what a declaration becomes in a document, and
   how it gets there through Swashbuckle.
7. [Reading what an application serves](docs/07%20Reading%20what%20an%20application%20serves.md) — the calls, the
   types that travel in them, and the model whoever writes a client reads.
8. [A client of an API](docs/08%20A%20client%20of%20an%20API.md) — the C# written from that model, and what one
   of several forms turns into where the language has no word for it.
9. [What a document is held to](docs/09%20What%20a%20document%20is%20held%20to.md) — the rules, each answering
   with everything that breaks it.

## Building it

```
dotnet build src/IOKode.WireShape.slnx
dotnet test src/IOKode.WireShape.slnx
dotnet pack src/IOKode.WireShape.slnx --configuration Release --output packages
```

Every package carries one version, moved for all of them at once, and the packages of one version are built from
one tree. They are prerelease: a form, a name or a signature changes where the one before it was wrong, and
nothing is kept around to make the older way of saying it still work.

## Licence

BSD 2-Clause. See [LICENCE](LICENCE).
