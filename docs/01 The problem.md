# The problem

**What a value looks like once written is settled by the code of a converter, and there is nowhere to read it
from.**

```csharp
public sealed class BookingReferenceJsonConverter : JsonConverter<BookingReference>
{
    public override void Write(Utf8JsonWriter writer, BookingReference value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
```

That one line is the whole of what a `BookingReference` looks like written. The type is a record holding a string;
what travels is `"QX7F2P"`, a text and nothing else. Whoever wrote that converter settled it, once, and settled
it **as a statement inside a method**.

Which is the difficulty, and it is worth being exact about it. The form is not:

- a property of the type, that something could read;
- an attribute over the type, that something could look for;
- an entry in a table of forms, that something could look up.

It is a statement, and **a statement can be executed and not read**. The two ways to learn what it says are to
read the code, which works for a person and not for a program, or to write a value and look at what came out,
which works for a program and only where there is a value in hand and somewhere to write it to.

It is not that the serializer hides it. Ask `System.Text.Json` what it knows about a type a converter of its own
took over and it answers, honestly, that it cannot say:

```csharp
options.GetTypeInfo(typeof(BookingReference)).Kind;   // None — not an object, not a value, nothing
```

And the schema exporter that ships with it writes `true` — anything at all — because there is no answer to give.

## Writing is not the only thing done with a form

The same form is needed in four more places, and none of them is the one that writes:

| | What it needs the form for |
| --- | --- |
| A document | To say that this member is a text of six capitals and not an object |
| A generated client | To declare a type the caller can pass, which has to be the one the API accepts |
| The binder | To read `/bookings/QX7F2P` out of a route and make a value of it |
| Whatever reads a column, a message, a line of a file | To write the value out and read it back |

Each of those can work the form out for itself, from the type. And the type is not what travels.

So each of them can be wrong on its own, and each is wrong in its own way:

- a document says `BookingReference` is an object with one member, and whoever reads it sends
  `{"value": "QX7F2P"}` where `"QX7F2P"` was expected;
- a generated client declares a class where a text is wanted, and the caller writes code against something the
  API never accepts;
- the binder takes a value for something with members, cannot read it from text, and leaves nothing behind but
  the hole in the route;
- a `Money` whose converter writes an amount and a currency is described as a number, and a caller who sends one
  gets a refusal they cannot read.

None of those is caught by a compiler, and none of them is caught by a test of the application: the application
works. What breaks is the agreement between the application and whoever it talks to, which is exactly what nobody
owns.

## What there is instead

**The written form of a value is declared once, as data a program can read**, and whoever has to say what a value
looks like reads the declaration instead of working it out from the type.

```csharp
bookings.Add<BookingReference>(new Shape.AsText(Pattern: "^[A-Z]{6}$"));
```

That is the whole idea. The rest of this documentation is what follows from it: what a form can say, what is
asked of whoever writes rather than declared, how a document, a client and a binder read it, and what each of
them is still free to decide.

## What is not here

**A wire is one place where values are written**: the API an application serves, the JSON a column keeps, the
body it sends to an API of someone else. Each writes the same types differently, so each is a wire of its own
with declarations of its own, and nothing here makes two of them agree — they describe different things.

**A form is declared only where it cannot be asked for.** What a writer writes as it stands needs no declaration,
and declaring it again would be saying twice what is said once, with two things to keep in step.

**What is yours stays yours.** What a schema is called, which of the calls an assembly serves belong together,
what a client made of them is called, what a type turns out to be: each of those is asked for, and none of them
is decided here.
