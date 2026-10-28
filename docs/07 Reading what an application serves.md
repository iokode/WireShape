# Reading what an application serves

An API as data: the calls it answers and the types that travel in them. Whoever reads that is reading a
description and not an application — nothing in it has to be asked of a serializer, of a controller or of a
wire, because whoever built it asked already. That is what lets the writing of a client be done by something
that knows nothing of where the API came from.

Everything here is of the booking API of [getting started](02%20Getting%20started.md).

## The calls

```csharp
var actions = new ApiSurface(
    typeof(BookingsController).Assembly,
    options => options.Conventions.Add(new FlightsRouteConvention()),
    method => method.GetCustomAttribute<ServedAttribute>() != null,
    method => method.GetCustomAttribute<SourceCommandAttribute>()?.CommandType).GetActions();
```

The calls are asked of the explorer of the application the host would build, which is what says how an API looks
from outside and what whoever writes a document reads as well. No server is started, no database is opened and no
controller is instantiated.

Three things are yours, and they are what the three arguments after the assembly are:

- **the conventions** the application is configured with, which is where a route of yours is built. Whatever
  decides a path — a prefix per assembly, a segment per module, one per kind of caller — has its say here, so
  the route that comes out is the route that is served.
- **whether an action belongs** to the surface being read. One assembly can serve two surfaces — one API for
  whoever is served and another for whoever administers it — and which of them an action is of is not something
  this can know.
- **what stands behind an action**, where anything does: the command or the query it comes from. Null is an
  answer.

An action comes out as an `ApiAction`: the method and the route, the controller it is on, the name the call
takes in a client, what it takes, what it answers with, and whether it answers with a file or is sent in parts.

| What it carries | Read from |
| --- | --- |
| `Method`, `Route` | The explorer, so the route is the one the application serves |
| `Controller` | The controller the action is on, as the application names it and not cut down: what a client made of its calls is called is yours |
| `MethodName` | The method, with `Async` added where it does not say so already: every call is awaited |
| `Operation` | What you answered stands behind it |
| `Parameters` | One per parameter of the action |
| `Result` | What the action says it answers with for a code in the two hundreds |
| `IsDownload` | Whether that answer is a result of its own rather than a value |
| `IsMultipart` | Whether anything it takes travels in parts |

## What a call takes

The explorer spells a group of values out member by member, because that is how they travel; a client takes the
group, so they are put back together by the parameter each of them came from. What tells a call to stop — the
cancellation — is not one of them.

Each value says where it comes from: the path, the body, the query string or a form. A file is a form value and
nothing else. Where the explorer says nothing, a value that is simple enough to be typed is taken to come from
the query string and anything else from the body.

Whether a value may be absent is the explorer's answer too: a body the API requires is a value the client has to
give, and one the API reads as nothing when it is absent is one it may leave out. What the path is made of is
never absent, whatever anyone says: a route with a hole in it is not a route.

**The descriptors have to be taught first.** The explorer asks them what a parameter is, and a value it cannot
read from text is taken for something with members. See
[reading a value from text](05%20Reading%20a%20value%20from%20text.md).

## What travels

```csharp
var mentioned = actions.SelectMany(action => action.Parameters
    .Select(parameter => (Type?) parameter.Type)
    .Append(action.Result));

var kinds = wire.GetDeclaredKinds().ToArray();
var model = new ApiModel(actions, wire.GetStructures(mentioned), kinds, "kind");
```

The wire is asked, because what travels with a type is a question for a wire and not for an API: the same answer
serves what a column keeps and what a message carries.
[Asking the wire](04%20Asking%20the%20wire.md) says which types it answers about and which type it refuses. The
order the types are given in is the order they come out in, and that is the order whoever reads the model
declares them in, so a sequence read from the calls — the values of each, then what it answers with — keeps the
model stable between one reading and the next.

**A file of a form is declared, like anything else whoever writes answers wrongly about.** Asked about one,
`System.Text.Json` answers that it is an object of six members — its length, its name, its media type, its
headers — so a client written from that answer would send those six as JSON instead of sending the boarding pass.
The declaration belongs here and not in the base package, which neither references ASP.NET Core nor should:

```csharp
bookings.Add<IFormFile>(new Shape.AsFile());
```

Whether the call carries it as a part of a form is a fact of the call and stays there: `IsMultipart` on the
action, and the `Form` origin on the parameter. The form says what the value comes out as, not where it travels
— and the origin alone would not do, because a call that takes a file usually takes ordinary fields of a form
beside it.

What each type comes out as:

| | What it means |
| --- | --- |
| `Carried(By, NeedsItsOwnName)` | It comes out as another type, which is `By`: the carrier it was declared to travel as, or what a wrapper left behind when it disappeared |
| `Object(Members, Base, Kinds)` | It travels as an object with these members, under the nearest type above it that travels as one of several, and as one of these kinds |
| `Enumeration(ByName, Values)` | It travels as one of a set of values, by the word of each or by the number of each, and each value carries the number underlying it and the text it travels as |
| `OneOf(Alternatives)` | It comes out as one of several forms, each under the name it was declared with and with the type it travels as. Which one arrived is told by what arrived |
| `File` | It comes out as the content of a file, which travels as it stands and not written inside anything else |

So of the booking API: `BookingReference` is `Carried(string, needs its own name)`, `BoardingAt` is
`Carried(string, needs none)`, `Money` is `Carried(MoneyParts, needs none)`, `Segment` is an `Object` with two
kinds, `CabinClass` an `Enumeration` by word, `PassengerRef` a `OneOf` of two, and a boarding pass a `File`.

**A wrapper that disappears is not a case of its own.** How a type came to come out as another — a carrier named
in the declaration, or a wrapper leaving one of its own arguments behind — is a fact of the declaring, and what travels
is the same either way: that other type, in its place. A model that said which of the two it had been would be
carrying something about how it was written down, and this one carries what travels.

**A hierarchy is not `OneOf` either, and the difference is of whoever receives.** A type with kinds travels as an
object and which kind it is is said by a member inside it, so it is read by reading that member and a client
writes it as a type with its own below it. One of several forms has nothing to read: it is resolved by looking at
what arrived, and a client writes it as one type that knows how to read either. Two cases, because they are two
different pieces of work.

**What the alternatives are is a name and a type, not what each of them comes out as.** The name is the one it
was declared under, and it is what a client calls the one way of reading that branch; an alternative is a form
and not a type that was declared, so it has no name of a **schema** to keep; and asking what one of those types
comes out as is the same as asking it of the type of a member — it is a row of this table where there is an
answer to give, and where there is none it is one of the things every writer writes as they stand. The one thing
it loses is two alternatives that travel as the same type, which cannot be told apart here; it loses nothing a
client could have written, two branches of one type being two branches it cannot declare either.

## Whether a type needs a name of its own

`NeedsItsOwnName` is asked of all five, and it answers one question: does the meaning of such a value live
nowhere but in this type? Where it does, whoever writes has to give the type a place of its own — a schema of a
document, a type of a client — and where it does not, what it comes out as is written instead.

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

**Which name it goes under is not here, and nothing in this library decides it.** A document names its schemas
and a client names its types, and one type may go by a different name in each; that is what `nameOf` is for. What
is said here is that there has to be one.

The last row is the one that cannot be worked out by whoever reads the model. A wrapper that comes out as a text
comes out as something that *does* need a name of its own, and taking the answer from the carrier would write
`Amended<string>` as a type of its own with nothing of its own to put in it. The name belongs to what the
wrapper left behind, never to the wrapper, so the answer travels in the model.

A type written whole is not described at all, and neither is a list, a map or an array: the first is what every
client already knows, and the second leaves nothing of itself and is described by what it holds.

A type between a kind and its base that declares nothing of its own leaves no trace: `Base` is the nearest type
above it that travels as one of several.

**Every hierarchy the wire can name is in the model**, whether or not a call mentions it: a kind added to a
hierarchy travels in an answer of the application, and whoever reads the model has to be able to describe it. A
hierarchy only whoever writes knows is not among those, so it is described where something mentions it and
nowhere else. The discriminator is of the whole API and is one of the four things an `ApiModel` carries.
