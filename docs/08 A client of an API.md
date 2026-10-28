# A client of an API

A client is written from [the model of an API](07%20Reading%20what%20an%20application%20serves.md) and from
nothing else: no wire, no serializer, no running application. What it writes is C#: one class per controller, a
type per thing that travels, and the small runtime they need beside them.

```csharp
File.WriteAllText(
    path,
    ClientSource.Write(model, "Flights.Client", controller => controller + "Client"));
```

That writes all of it as one file. Where you want the pieces apart, each is available on its own:

```csharp
var names = new TypeNames(model);
var clients = new ClientWriter(model, names, controller => controller + "Client");
clients.Write(model.Actions);

var types = new DtoWriter(model, names);
types.WritePending();
```

`TypeNames` is what the two writers share. Asking it for the name of a type puts that type in the queue of what has
to be declared, and `WritePending` empties the queue, declaring whatever the declaring itself turns up. So a client
carries the types its calls mention and no others.

Write the clients first: what has to be declared is only known once every call has been written.

## What is yours

This package writes **one** shape of C# client, and the shape is an opinion: an `HttpClient` and nothing else, a
class per controller, a `record struct` for a value, and `partial` on everything so you can add to it. If you want
another shape, write it against the same model; that is what the model is for.

Three things it does not decide:

| | |
| --- | --- |
| What a client is called | A `Func<string, string>` from the name of the controller. What your clients' names end in is yours, whatever wrote them, and nothing here takes a word off either end |
| Which types another client already declares | `Reuse(type, name)`, so that two clients of one application speak of one type and not of two alike. A kind added to a hierarchy that another client declares says so on itself, because that hierarchy was written without knowing the kind existed |
| The member that tells one kind from another | `ApiCallSource.Write(discriminator)`, which takes the one the model carries |

## What it writes for each thing that travels

| What the model says | What is written | Of the example |
| --- | --- | --- |
| It travels as a value and keeps its name | A `record struct` of one member with a converter of its own, and an implicit conversion each way | `BookingReference`, `PassengerId`, `FlightDuration` |
| It travels as a value and keeps no name | Nothing. What it travels as is written where it is used, so the caller is handed the kind their own language has | `DepartsOn` is a `DateOnly`, `BoardingAt` a `DateTimeOffset` |
| It travels as another type of the wire | Nothing of its own. That type is written where it is used | `Money` is a `MoneyParts` |
| An object | A `partial record` with settable members: `required` where the caller has to give it, and asked of nobody where only an answer carries it | `Booking`, `Passenger`, `MoneyParts` |
| One of several kinds | The base with `[JsonPolymorphic]` and a `[JsonDerivedType]` per kind, and then each kind | `Segment`, `FlightSegment`, `TrainSegment` |
| An enumeration by word | An `enum` with the converter that writes words, and the word given one by one only where it differs from the name | `CabinClass` |
| An enumeration by number | An `enum` with each number written out, because what travels is the number and not the order | `DisruptionReason` |
| The content of a file | Nothing of its own: it is the `ApiFile` of the runtime | the boarding pass |
| One of several forms | A type of its own, below | `PassengerRef` |

## One of several forms

C# cannot say "a text or an object", so what is written is one type that holds either and says which it holds:

```csharp
var booking = await bookings.GetAsync(new BookingReference("QX7F2P"));

var shown = booking.Passenger.Kind switch
{
    PassengerRefKind.Id => booking.Passenger.AsId.ToString(),
    PassengerRefKind.Expanded => booking.Passenger.AsExpanded.Name
};
```

`Kind` is an enumeration of the alternatives, under the names they were declared with. So **covering them is a
`switch` whose missing arm the compiler reports**, and when the wire gains an alternative, every `switch` over it
stops compiling.

Beside it, for each alternative:

- `IsId`, for asking;
- `AsId`, for where you know which to expect, which throws if you are wrong;
- `TryAsId(out …)`, for asking without an exception.

Make a value with `PassengerRef.OfId(…)`, or by assigning the alternative, because there is an implicit conversion
from each. The exception is an alternative that arrives as a list: it is typed as an interface here, and the
language allows no conversion from an interface.

It is a class and not a value, which is the one place this departs from how a value is written.

**Why:** the value a struct has before anything is assigned would claim to hold the first alternative while holding
nothing. Reading it would answer null where it should throw, and a `switch` over it would take the branch of
something that was never set. Unassigned is `null` here, which the language already polices.

Reading tells the alternatives apart by **what arrived** — a text, a number, a truth, an object, a list, with at
most one alternative of each — because nothing in a union says which of them came.

**And it refuses to write a union whose alternatives arrive looking alike**, such as two texts or two objects, and
names them. This reading cannot tell whether a reader could tell such a pair apart by a pattern or by which members
they carry, and a reading that guessed would hand over the wrong one in silence. Declaring such a union is not
refused, because [a document can describe it](06%20An%20OpenAPI%20document.md). So what has to change is either
what travels or who reads it.

## What is written beside it

`ApiCallSource` is what every call needs, written alongside the clients so that a client carries nothing but
itself: no package to install and no version to keep in step. It holds:

- the file a call carries and the one it answers with;
- what a value of the wire looks like from outside;
- the refusal of a call, with the reason it gave;
- the options everything is read and written with;
- the making of a URL, a query, a body and a form.

And one attribute, for a kind added to a hierarchy that another client declares. That hierarchy was written without
knowing the kind existed, so the kind is the one to say it.
