# Reading a value from text

A route and a query string carry text. A value declared to travel as a text or a number has to be read out of
that text, and read in the form the wire declares, or what a caller may put in a query string and what they may
put in a body come apart.

```csharp
foreach (var (type, converter) in TypeDescriptors.GetConverters(wire))
{
    TypeDescriptors.Teach(type, converter);
}
```

## What it reads

`TypeDescriptors.GetConverters` answers with one converter per declared value that can be read out of text at all:
every declaration whose carrier the framework can read from a string. A value declared to travel as something
nobody can type is not there, so a form nobody can send does not end up declared as one that can — and neither is
one declared without a carrier, there being no type to read the text as. So of the booking API,
`BookingReference`, `PassengerId`, `FlightDuration` and `DepartsOn` are read from a route; `Money`, a boarding
pass and a `PassengerRef` are not, and nothing pretends otherwise.

Each of them is a `ShapeTypeConverter`, and one class serves every type: the text is read as the type the
value travels as, and the value is then built by whoever writes it.

```
"QX7F2P" ──► read as string ──► written as JSON ──► read as BookingReference
```

Going the other way is the same backwards, so a value written into a route reads back as the text it travelled
as. What this gives is that the form is the converter's, always: a booking reference with its pattern, a duration
that is a number of minutes, an enumeration under the word its converter writes rather than the name it carries
in the code.

## What has to happen first

A type descriptor is of the whole process. Two things follow:

- **Only one wire can teach it.** Whatever else the process writes in another form has to read what it is given
  itself.
- **It has to be taught before anything reads the surface or binds a value.** The explorer of an application
  asks the descriptors what a parameter is: a type it cannot read from text is taken for something with members,
  and what the route is made of comes out as a hole with nothing in it. Teaching the descriptors is therefore the
  first thing, before the model of the application is built and before the first call is answered.

`Teach` is also where a converter of your own goes, for a type whose form no declaration can state:

```csharp
TypeDescriptors.Teach(typeof(DisruptionReason), new DisruptionReasonTypeConverter());
```
