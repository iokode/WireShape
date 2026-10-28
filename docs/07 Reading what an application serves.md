# Reading what an application serves

This chapter is about an API as data: the calls it answers and the types that travel in them. Whoever reads that
data is reading a description, not an application. Nothing in it has to be asked of a serializer, of a controller
or of a wire, because whoever built it asked already. That is what lets a client be written by something that
knows nothing of where the API came from.

Everything here is of the booking API of [getting started](02%20Getting%20started.md).

## The calls

```csharp
var actions = new ApiSurface(
    typeof(BookingsController).Assembly,
    options => options.Conventions.Add(new FlightsRouteConvention()),
    method => method.GetCustomAttribute<ServedAttribute>() != null,
    method => method.GetCustomAttribute<SourceCommandAttribute>()?.CommandType).GetActions();
```

The calls are asked of the explorer of the application the host would build. The explorer is what says how an API
looks from outside, and it is what whoever writes a document reads as well. No server is started, no database is
opened and no controller is instantiated.

Three things are yours, and they are the three arguments after the assembly:

- **the conventions** the application is configured with, which is where a route of yours is built. Whatever
  decides a path — a prefix per assembly, a segment per module, one per kind of caller — has its say here, so the
  route that comes out is the route that is served.
- **whether an action belongs** to the surface being read. One assembly can serve two surfaces, such as one API
  for customers and another for administrators, and only you know which of them an action belongs to.
- **what stands behind an action**, where anything does: the command or the query it comes from. Null is an answer.

Each action comes out as an `ApiAction`: the method and the route, the controller it is on, the name the call takes
in a client, what it takes, what it answers with, and whether it answers with a file or is sent in parts.

| What it carries | Read from |
| --- | --- |
| `Method`, `Route` | The explorer, so the route is the one the application serves |
| `Controller` | The controller the action is on, named as the application names it and not cut down: what a client made of its calls is called is yours |
| `MethodName` | The method, with `Async` added where its name does not say so already, because every call is awaited |
| `Operation` | What you answered stands behind it |
| `Parameters` | One per parameter of the action |
| `Result` | What the action says it answers with, for a status code in the 200s |
| `IsDownload` | Whether that answer is a result of its own rather than a value |
| `IsMultipart` | Whether anything it takes travels in parts |

## What a call takes

The explorer spells a group of values out member by member, because that is how they travel. A client takes the
group, so the values are put back together by the parameter each of them came from. The cancellation token, which
tells a call to stop, is not one of them.

Each value says where it comes from: the path, the body, the query string or a form. A file is a form value and
nothing else. Where the explorer says nothing, a value simple enough to be typed is taken to come from the query
string, and anything else from the body.

Whether a value may be absent is also the explorer's answer. A body the API requires is a value the client has to
give, and a body the API reads as nothing when it is absent is one the client may leave out. What the path is made
of is never absent, whatever anyone says, because a route with a hole in it is not a route.

**Teach the descriptors first.** The explorer asks them what a parameter is, and takes a value it cannot read from
text for something with members. See [reading a value from text](05%20Reading%20a%20value%20from%20text.md).

## What travels

```csharp
var mentioned = actions.SelectMany(action => action.Parameters
    .Select(parameter => (Type?) parameter.Type)
    .Append(action.Result));

var kinds = wire.GetDeclaredKinds().ToArray();
var model = new ApiModel(actions, wire.GetStructures(mentioned), kinds, "kind");
```

The wire is asked, because what travels with a type is a question for a wire and not for an API: the same answer
serves what a column keeps and what a message carries. [Asking the wire](04%20Asking%20the%20wire.md) says which
types it answers about and which type it refuses.

The types come out in the order they are given, and whoever reads the model declares them in that order. So
passing them in an order read from the calls — the values of each call, then what it answers with — keeps the
model stable from one reading to the next.

**Declare a file of a form, like anything else the writer answers about wrongly.** Asked about one,
`System.Text.Json` answers that it is an object of six members — its length, its name, its media type, its headers
and so on — so a client written from that answer would send those as JSON instead of sending the boarding pass:

```csharp
bookings.Add<IFormFile>(new Shape.AsFile());
```

The declaration belongs in your application and not in the base package, which does not reference ASP.NET Core
and should not.

Whether the call carries the file as a part of a form is a fact of the call, and stays there: `IsMultipart` on the
action, and the `Form` origin on the parameter. The form says what the value comes out as, not where it travels.
The origin alone would not do, because a call that takes a file usually takes ordinary form fields beside it.

What each type comes out as:

| | What it means |
| --- | --- |
| `Carried(By, NeedsItsOwnName)` | It comes out as another type, which is `By`: the carrier it was declared to travel as, or what a wrapper left behind when it disappeared |
| `Object(Members, Base, Kinds)` | It travels as an object with these members, under the nearest type above it that travels as one of several, and as one of these kinds |
| `Enumeration(ByName, Values)` | It travels as one of a set of values, by the word of each or by the number of each. Each value carries the number underlying it and the text it travels as |
| `OneOf(Alternatives)` | It comes out as one of several forms, each under the name it was declared with and with the type it travels as. Which one arrived is told by what arrived |
| `File` | It comes out as the content of a file, which travels as it stands and is not written inside anything else |

So of the booking API:

- `BookingReference` is `Carried(string, needs its own name)`;
- `BoardingAt` is `Carried(string, needs none)`;
- `Money` is `Carried(MoneyParts, needs none)`;
- `Segment` is an `Object` with two kinds;
- `CabinClass` is an `Enumeration` by word;
- `PassengerRef` is a `OneOf` of two;
- a boarding pass is a `File`.

**A wrapper that disappears is not a case of its own.** How a type came to come out as another — a carrier named in
the declaration, or a wrapper leaving one of its own arguments behind — is a fact of the declaring. What travels is
the same either way: that other type, in its place. A model that said which of the two it had been would carry
something about how it was written down, and this model carries what travels.

**A hierarchy is not a `OneOf` either, and the difference lies with whoever receives it.**

- A type with kinds travels as an object, and which kind it is is said by a member inside it. It is read by reading
  that member, and a client writes it as a type with its kinds below it.
- One of several forms has nothing to read. It is resolved by looking at what arrived, and a client writes it as
  one type that knows how to read any of them.

They are two cases because they are two different pieces of work.

**An alternative is a name and a type, not what that type comes out as.**

- The name is the one it was declared under, and it is what a client calls the one way of reading that branch.
- An alternative is a form, not a declared type, so it has no **schema** name to keep.
- What one of those types comes out as is the same question as for the type of a member. It is a row of this table
  where there is an answer to give, and where there is none it is one of the things every writer writes as it
  stands.

The one thing this loses is two alternatives that travel as the same type, which cannot be told apart here. It
loses nothing a client could have written, because two branches of one type are two branches a client cannot
declare either.

## Whether a type needs a name of its own

`NeedsItsOwnName` is asked of all five cases, and it answers one question: does the meaning of such a value live
nowhere but in this type? Where it does, whoever writes a document or a client has to give the type a place of its
own — a schema, a type. Where it does not, what it comes out as is written instead.

| | Needs one | Because |
| --- | --- | --- |
| `Object` | yes | its members live nowhere else, so there has to be somewhere to write them |
| `Enumeration` | yes | which values are admitted lives nowhere else either |
| `OneOf` | yes | that these alternatives are one field lives nowhere else: with the type gone, what is left is several forms with nothing saying they belong together |
| `File` | no | the meaning is in what travels — content, under a name and a media type — and a name of ours adds nothing to it |
| `Carried` as a text, a number, a GUID | yes | a text is any text: with the name gone, nothing is left of what the value meant |
| `Carried` as a date or an instant | no | whoever reads a document knows a date where it says so, and a name of ours over it would hide that |
| `Carried` as a carrier with members | no | what travels is the carrier's object, and the name in the document is the carrier's |
| `Carried` by a wrapper that disappeared | no | a wrapper means nothing by itself, so there is nothing of it to write |

**Which name it goes under is not here, and nothing in this library decides it.** A document names its schemas and
a client names its types, and one type may go by a different name in each; that is what `nameOf` is for. What is
said here is only that there has to be one.

The last row is the one whoever reads the model cannot work out. A wrapper that comes out as a text comes out as
something that *does* need a name of its own. Taking the answer from the carrier would write `Amended<string>` as a
type of its own, with nothing of its own to put in it. The name belongs to what the wrapper left behind, never to
the wrapper, so the answer travels in the model.

A type written whole is not described at all, and neither is a list, a map or an array. The first is something
every client already knows. The others leave nothing of themselves and are described by what they hold.

A type between a kind and its base that declares nothing of its own leaves no trace: `Base` is the nearest type
above it that travels as one of several.

**Every hierarchy the wire can name is in the model**, whether or not a call mentions it. A kind added to a
hierarchy travels in an answer of the application, and whoever reads the model has to be able to describe it. A
hierarchy only the writer knows is not among those, so it is described where something mentions it and nowhere
else.

The discriminator is of the whole API, and it is one of the four things an `ApiModel` carries.
