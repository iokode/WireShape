# Reading what an application serves

**Package:** `IOKode.WireShape.AspNetCore`

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
    method => method.GetCustomAttribute<ServedAttribute>() != null).GetActions();
```

The calls are asked of the explorer of the application the host would build. The explorer is what says how an API
looks from outside, and it is what whoever writes a document reads as well. No server is started, no database is
opened and no controller is instantiated.

Two things are yours, and they are the two arguments after the assembly:

- **the conventions** the application is configured with, which is where a route of yours is built. Whatever
  decides a path — a prefix per assembly, a segment per module, one per kind of caller — has its say here, so the
  route that comes out is the route that is served.
- **whether an action belongs** to the surface being read. One assembly can serve two surfaces, such as one API
  for customers and another for administrators, and only you know which of them an action belongs to.

Each action comes out as an `ApiAction`: the method and the route, the controller it is on, the name the call takes
in a client, what it takes, what it answers with, and whether it answers with a file or is sent in parts.

| What it carries | Read from |
| --- | --- |
| `Method`, `Route` | The explorer, so the route is the one the application serves |
| `Controller` | The controller the action is on, named as the application names it and not cut down: what a client made of its calls is called is yours |
| `MethodName` | The method, with `Async` added where its name does not say so already, because every call is awaited |
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
text for something with members. See [reading a value from text](07%20Reading%20a%20value%20from%20text.md).

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
[Asking the wire](06%20Asking%20the%20wire.md#everything-that-travels-with-a-type) says which types it answers about,
which type it refuses and what each of them comes out as.

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

**Every hierarchy the wire can name is in the model**, whether or not a call mentions it. A kind added to a
hierarchy travels in an answer of the application, and whoever reads the model has to be able to describe it. A
hierarchy only the writer knows is not among those, so it is described where something mentions it and nowhere
else.

**The member the word of a kind travels under is of the whole API**, `"kind"` here, and it is one of the four things
an `ApiModel` carries, as `Discriminator`. The word itself is each kind's own, and travels in the model with it.
