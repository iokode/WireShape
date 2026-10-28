# WireShape

WireShape is a .NET library for declaring, once and as data, what a value looks like once written. The serializer,
an OpenAPI document, the binder of a route and a generated client then read that one declaration instead of each
working the form out from the type.

The form of a value is settled by the code of a converter, and nothing can read it from there. A `BookingReference`
is a record holding a string and travels as `"QX7F2P"`, but a document generated from the type describes it as an
object with one member, a client generated from that document asks for an object, and the binder cannot read one
out of a route. [The problem](https://github.com/iokode/WireShape/blob/master/docs/01%20The%20problem.md) says the
rest.

The base package names no serializer. What ships covers the HTTP API of an ASP.NET Core application written with
`System.Text.Json`.

## Installing it

The packages target `net10.0` and are prerelease:

```
dotnet add package IOKode.WireShape --prerelease
```

| To | Install |
| --- | --- |
| Declare forms and ask a wire about them | `IOKode.WireShape` |
| Ask `System.Text.Json` what a declaration does not say | `IOKode.WireShape.SystemTextJson` |
| Read a declared value out of a route or a query string | `IOKode.WireShape.ComponentModel` |
| Say what a declaration becomes in an OpenAPI document | `IOKode.WireShape.OpenApi` |
| Write it into the documents Swashbuckle generates | `IOKode.WireShape.Swashbuckle` |
| Read the calls an ASP.NET Core application serves, as a model of its API | `IOKode.WireShape.AspNetCore` |
| Write a C# client of that model | `IOKode.WireShape.CSharpClient` |
| Hold what a wire is declared from to its rules, in your tests | `IOKode.WireShape.Testing` |
| Hold a document to its rules, in your tests | `IOKode.WireShape.OpenApi.Testing` |

## A first declaration

```csharp
var declaring = new WireBuilder();
var bookings = declaring.GetDeclarer("bookings");

bookings.Add<BookingReference>(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
bookings.Add<Money>(new Shape.AsAnother(typeof(MoneyParts)));

var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
};

options.Converters.Add(new BookingReferenceJsonConverter());
options.Converters.Add(new MoneyJsonConverter());

Wire wire = declaring.Build(new JsonSerializerOptionsSerializer(options));

wire.GetShape(typeof(BookingReference));   // a text matching '^[A-Z]{6}$'
```

[Getting started](https://github.com/iokode/WireShape/blob/master/docs/02%20Getting%20started.md) declares the
whole example, and serves a document, reads a value out of a route and writes a client from it.

## Documentation

1. [The problem](https://github.com/iokode/WireShape/blob/master/docs/01%20The%20problem.md)
2. [Getting started](https://github.com/iokode/WireShape/blob/master/docs/02%20Getting%20started.md)
3. [Declaring a form](https://github.com/iokode/WireShape/blob/master/docs/03%20Declaring%20a%20form.md)
4. [Reading declarations from a source](https://github.com/iokode/WireShape/blob/master/docs/04%20Reading%20declarations%20from%20a%20source.md)
5. [Hierarchies and enumerations](https://github.com/iokode/WireShape/blob/master/docs/05%20Hierarchies%20and%20enumerations.md)
6. [Asking the wire](https://github.com/iokode/WireShape/blob/master/docs/06%20Asking%20the%20wire.md)
7. [Reading a value from text](https://github.com/iokode/WireShape/blob/master/docs/07%20Reading%20a%20value%20from%20text.md)
8. [An OpenAPI document](https://github.com/iokode/WireShape/blob/master/docs/08%20An%20OpenAPI%20document.md)
9. [A document through Swashbuckle](https://github.com/iokode/WireShape/blob/master/docs/09%20A%20document%20through%20Swashbuckle.md)
10. [Reading what an application serves](https://github.com/iokode/WireShape/blob/master/docs/10%20Reading%20what%20an%20application%20serves.md)
11. [A client of an API](https://github.com/iokode/WireShape/blob/master/docs/11%20A%20client%20of%20an%20API.md)
12. [The rules](https://github.com/iokode/WireShape/blob/master/docs/12%20The%20rules.md)
13. [Glossary](https://github.com/iokode/WireShape/blob/master/docs/13%20Glossary.md)

## Versions

Every package carries one version, moved for all of them at once, and the packages of one version are built from
one tree. While they are prerelease, a form, a name or a signature changes where the one before it was wrong, and
nothing is kept around to make the older way of saying it still work.

## License

BSD 2-Clause. See [LICENSE](https://github.com/iokode/WireShape/blob/master/LICENSE).
