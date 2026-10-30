# Reading a value from text

**Package:** `IOKode.WireShape.ComponentModel`

A route and a query string carry text. A value declared to travel as a text or a number has to be read out of
that text, and read in the form the wire declares. Otherwise what a caller may put in a query string and what they
may put in a body come apart.

Teach the process every declared value it can read from text:

```csharp
foreach (var (type, converter) in TypeDescriptors.GetConverters(wire))
{
    TypeDescriptors.Teach(type, converter);
}
```

## What it reads

`TypeDescriptors.GetConverters` answers with one converter per declared value that can be read out of text at
all: every declaration whose carrier the framework can read from a string.

Two kinds of declaration are left out. A value declared to travel as something nobody can type is not there, so a
form nobody can send is not declared as one that can be sent. Neither is a value declared without a carrier,
because there is no type to read the text as.

So of the booking API, `BookingReference`, `PassengerId`, `FlightDuration`, `DepartsOn` and `BoardingAt` are read
from a route. `Money`, a boarding pass and a `PassengerRef` are not, and nothing pretends otherwise.

Each converter is a `ShapeTypeConverter`, and one class serves every type. The text is read by the writer of the
wire, told what the value travels as — a text, a number, a truth — and nothing else reads it:

```
"QX7F2P" ──► read by the writer as a text ──► BookingReference
```

The other way is the writer too, so a value written into a route is the text it has in a body. What this gives is
that the form is always the converter's: a booking reference with its pattern, a duration that is a number of
minutes, an enumeration under the word its converter writes instead of the name it has in the code, a time of day
with its fraction and an instant in UTC that says so, and none of it in the culture of the process.

## What has to happen first

A type descriptor belongs to the whole process. Two things follow:

- **Only one wire can teach it.** Anything else the process writes in another form has to read what it is given by
  itself.
- **Teach it before anything reads the surface or binds a value.** The explorer of an application asks the
  descriptors what a parameter is. It takes a type it cannot read from text for something with members, and what
  the route is made of comes out as a hole with nothing in it. So teaching the descriptors comes first: before the
  model of the application is built, and before the first call is answered.

`Teach` is also where a converter of your own goes, for a type whose form no declaration can state:

```csharp
TypeDescriptors.Teach(typeof(DisruptionReason), new DisruptionReasonTypeConverter());
```
