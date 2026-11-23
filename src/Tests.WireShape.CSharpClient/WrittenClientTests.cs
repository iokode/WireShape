using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IOKode.WireShape.AspNetCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace IOKode.WireShape.CSharpClient.Tests;

public readonly record struct ShippedOn(DateOnly Day);

public readonly record struct Insured(bool Value);

public enum Change
{
    Added = 1,
    Joined = 2
}

/// <summary>
/// What a written client does once it is compiled and run, which is what a text that compiles says nothing
/// about: which text a value travels as, and what is refused where it is read.
/// </summary>
public class WrittenClientTests
{
    private static readonly Lazy<Assembly> written = new(Compile);

    /// <summary>
    /// A route carries the text a body carries, whatever the culture of whoever calls: a moment, a time of day
    /// and a date each as the serializer writes them, and nothing escaped twice.
    /// </summary>
    public static TheoryData<object, string> InABody => new()
    {
        {new DateTimeOffset(2026, 10, 29, 9, 30, 0, TimeSpan.FromHours(1)), "2026-10-29T09:30:00+01:00"},
        {new DateTime(2026, 10, 29, 9, 30, 0), "2026-10-29T09:30:00"},
        {new DateTime(2026, 10, 29, 9, 30, 0, DateTimeKind.Utc), "2026-10-29T09:30:00Z"},
        {new TimeOnly(9, 30, 0).Add(TimeSpan.FromMilliseconds(500)), "09:30:00.5000000"},
        {new DateOnly(2026, 10, 29), "2026-10-29"},
        {new TimeSpan(1, 2, 3, 4), "1.02:03:04"},
        {true, "true"},
        {12.5, "12.5"},
        {12.50m, "12.50"},
        {'+', "+"},
        {"a+b", "a+b"},
        {Guid.Parse("3f2c8a1e-0b7d-4c55-9a61-2d4e8f0b1c3a"), "3f2c8a1e-0b7d-4c55-9a61-2d4e8f0b1c3a"}
    };

    [Theory]
    [MemberData(nameof(InABody))]
    public void AValueInARoute_ShouldBeWrittenAsItIsInABody(object value, string text)
    {
        // Act
        var route = InACulture("es-ES", () => Segment(value));

        // Assert
        Assert.Equal(Uri.EscapeDataString(text), route);
    }

    /// <summary>
    /// Under the word it travels as and not the name it has here, and with that word as it is and not as the
    /// JSON escapes it.
    /// </summary>
    [Theory]
    [InlineData(Change.Added, "añadido")]
    [InlineData(Change.Joined, "a+b")]
    public void AWordOfAnEnumerationInARoute_ShouldBeTheWordAsItIs(Change value, string word)
    {
        // Arrange
        var travelling = Enum.ToObject(Type(nameof(Change)), (int) value);

        // Act
        var route = Segment(travelling);

        // Assert
        Assert.Equal(Uri.EscapeDataString(word), route);
    }

    /// <summary>A body cannot carry it, so neither can a route.</summary>
    [Fact]
    public void ANumberABodyCannotCarry_ShouldBeRefusedInARouteAsWell()
    {
        // Act
        var writing = () => Segment(double.NaN);

        // Assert
        Assert.Throws<ArgumentException>(writing);
    }

    [Fact]
    public void AValue_ShouldBeTheTextItTravelsAs()
    {
        // Act
        var code = New(nameof(TrackingCode), "QX7F2P").ToString();
        var shipped = InACulture("es-ES", () => New(nameof(ShippedOn), new DateOnly(2026, 10, 29)).ToString());
        var insured = New(nameof(Insured), true).ToString();

        // Assert
        Assert.Equal("QX7F2P", code);
        Assert.Equal("2026-10-29", shipped);
        Assert.Equal("true", insured);
    }

    /// <summary>
    /// The text a route is built from, so that putting a value into one is the same with or without reaching
    /// for what it holds.
    /// </summary>
    [Fact]
    public void AValue_ShouldBeWhatARouteCarriesOfIt()
    {
        // Arrange
        var shipped = New(nameof(ShippedOn), new DateOnly(2026, 10, 29));

        // Act
        var text = shipped.ToString();

        // Assert
        Assert.Equal(Uri.EscapeDataString(text!), Segment(shipped));
    }

    /// <summary>A text that was never given, which is what a value holds before anything is put in it.</summary>
    [Fact]
    public void AValueThatHoldsNothing_ShouldBeAnEmptyText()
    {
        // Act
        var text = Activator.CreateInstance(Type(nameof(TrackingCode)))!.ToString();

        // Assert
        Assert.Equal(string.Empty, text);
    }

    /// <summary>
    /// Whoever answered broke the contract, and reading it anyway would hand over a value that looks whole and
    /// holds nothing.
    /// </summary>
    [Fact]
    public void AValueThatArrivesAsNull_ShouldBeRefusedWhereItIsRead()
    {
        // Act
        var reading = () => JsonSerializer.Deserialize("""{"code":null}""", Type(nameof(Consignment)), Options);

        // Assert
        var refused = Assert.Throws<JsonException>(reading);
        Assert.Contains(nameof(TrackingCode), refused.Message);
    }

    [Fact]
    public void AValueThatMayBeAbsentAndArrivesAsNull_ShouldBeReadAsNoValue()
    {
        // Arrange
        var optional = typeof(Nullable<>).MakeGenericType(Type(nameof(TrackingCode)));

        // Act
        var read = JsonSerializer.Deserialize("null", optional, Options);

        // Assert
        Assert.Null(read);
    }

    /// <summary>Left out is not null: a member left out does not travel at all.</summary>
    [Fact]
    public void AMemberThatMayBeAbsentLeftOut_ShouldNotTravel()
    {
        // Arrange
        var amendment = Activator.CreateInstance(Type(nameof(Amendment)))!;

        // Act
        var json = JsonSerializer.Serialize(amendment, amendment.GetType(), Options);

        // Assert
        Assert.Equal("{}", json);
    }

    /// <summary>Given null is not left out: null travels, as the value it was given.</summary>
    [Fact]
    public void AMemberThatMayBeAbsentGivenNull_ShouldTravelAsNull()
    {
        // Arrange
        var amendment = Activator.CreateInstance(Type(nameof(Amendment)))!;
        Set(amendment, "Instructions", Optional(typeof(string), null));

        // Act
        var json = JsonSerializer.Serialize(amendment, amendment.GetType(), Options);

        // Assert
        Assert.Equal("""{"instructions":null}""", json);
    }

    [Fact]
    public void AMemberThatMayBeAbsentGivenAValue_ShouldTravelAsTheValue()
    {
        // Arrange
        var amendment = Activator.CreateInstance(Type(nameof(Amendment)))!;
        Set(amendment, "Code", Optional(Type(nameof(TrackingCode)), New(nameof(TrackingCode), "QX7F2P")));
        Set(amendment, "Instructions", Optional(typeof(string), "At the back door"));

        // Act
        var json = JsonSerializer.Serialize(amendment, amendment.GetType(), Options);

        // Assert
        Assert.Equal("""{"code":"QX7F2P","instructions":"At the back door"}""", json);
    }

    /// <summary>
    /// Null is given by assigning it, as any other value is, which is how whoever calls says "make it nothing".
    /// </summary>
    [Fact]
    public void NullAssignedToAMemberThatMayBeAbsent_ShouldTravelAsNull()
    {
        // Arrange
        var amendment = Type("Assigning").GetMethod("Null")!.Invoke(null, null)!;

        // Act
        var json = JsonSerializer.Serialize(amendment, amendment.GetType(), Options);

        // Assert
        Assert.Equal("""{"instructions":null}""", json);
    }

    [Fact]
    public void AMemberThatMayBeAbsentAndDoesNotArrive_ShouldBeReadAsLeftOut()
    {
        // Act
        var amendment = JsonSerializer.Deserialize("{}", Type(nameof(Amendment)), Options)!;

        // Assert
        Assert.False(IsGiven(Get(amendment, "Instructions")));
    }

    [Fact]
    public void AMemberThatMayBeAbsentAndArrivesAsNull_ShouldBeReadAsGivenNull()
    {
        // Act
        var amendment = JsonSerializer.Deserialize("""{"instructions":null}""", Type(nameof(Amendment)), Options)!;

        // Assert
        var instructions = Get(amendment, "Instructions");
        Assert.True(IsGiven(instructions));
        Assert.Null(instructions.GetType().GetProperty("Value")!.GetValue(instructions));
    }

    /// <summary>No pair, which the application reads as left out: what it reads into is left as it was.</summary>
    [Fact]
    public void AMemberThatMayBeAbsentLeftOutInAQuery_ShouldWriteNoPair()
    {
        // Act
        var query = Query(("code", Optional(Type(nameof(TrackingCode)), null, given: false)), ("page", 2));

        // Assert
        Assert.Equal("?page=2", query);
    }

    /// <summary>
    /// A pair with no text, which is how the application reads it back as null, and not no pair, which it reads as
    /// left out.
    /// </summary>
    [Fact]
    public void AMemberThatMayBeAbsentGivenNullInAQuery_ShouldBeWrittenWithNoText()
    {
        // Act
        var query = Query(("remarks", Optional(typeof(string), null)), ("page", 2));

        // Assert
        Assert.Equal("?remarks=&page=2", query);
    }

    [Fact]
    public void AMemberThatMayBeAbsentGivenInAQuery_ShouldBeWrittenAsWhatItWasGiven()
    {
        // Act
        var query = Query(("code", Optional(Type(nameof(TrackingCode)), New(nameof(TrackingCode), "QX7F2P"))));

        // Assert
        Assert.Equal("?code=QX7F2P", query);
    }

    [Fact]
    public void AMemberThatMayBeAbsentAndArrivesWithAValue_ShouldBeReadAsGivenThatValue()
    {
        // Act
        var amendment = JsonSerializer.Deserialize("""{"code":"QX7F2P"}""", Type(nameof(Amendment)), Options)!;

        // Assert
        var code = Get(amendment, "Code");
        Assert.True(IsGiven(code));
        Assert.Equal("QX7F2P", code.GetType().GetProperty("Value")!.GetValue(code)!.ToString());
    }

    /// <summary>Null is not one of its values, so a member that may be left out but not null refuses it.</summary>
    [Fact]
    public void AMemberThatMayBeAbsentButNotNullAndArrivesAsNull_ShouldBeRefused()
    {
        // Act
        var reading = () => JsonSerializer.Deserialize("""{"code":null}""", Type(nameof(Amendment)), Options);

        // Assert
        var refused = Assert.Throws<JsonException>(reading);
        Assert.Contains(nameof(TrackingCode), refused.Message);
    }

    /// <summary>Only a member can be left out, and a member left out is not written at all.</summary>
    [Fact]
    public void AValueLeftOutWrittenOnItsOwn_ShouldBeRefused()
    {
        // Arrange
        var leftOut = Optional(typeof(string), null, given: false);

        // Act
        var writing = () => JsonSerializer.Serialize(leftOut, leftOut.GetType(), Options);

        // Assert
        Assert.Throws<NotSupportedException>(writing);
    }

    [Fact]
    public void TheValueOfAMemberLeftOut_ShouldBeRefused()
    {
        // Arrange
        var leftOut = Optional(typeof(string), null, given: false);

        // Act
        var reading = () => Read(leftOut, "Value");

        // Assert
        Assert.Throws<InvalidOperationException>(reading);
    }

    [Fact]
    public void AMemberLeftOutMadeOnPurpose_ShouldBeLeftOut()
    {
        // Act
        var leftOut = OptionalType(typeof(string)).GetProperty("LeftOut")!.GetValue(null)!;

        // Assert
        Assert.False(IsGiven(leftOut));
    }

    /// <summary>
    /// Two of them are the same where both were left out, or both were given the same value; given null is not
    /// left out.
    /// </summary>
    [Theory]
    [InlineData(false, null, false, null, true)]
    [InlineData(true, "a", true, "a", true)]
    [InlineData(true, null, true, null, true)]
    [InlineData(true, "a", true, "b", false)]
    [InlineData(true, null, false, null, false)]
    [InlineData(true, "a", false, null, false)]
    public void TwoMembersThatMayBeAbsent_ShouldBeEqualWhereTheyHoldTheSame(
        bool leftGiven,
        string? left,
        bool rightGiven,
        string? right,
        bool equal)
    {
        // Arrange
        var one = Optional(typeof(string), left, leftGiven);
        var other = Optional(typeof(string), right, rightGiven);
        var type = OptionalType(typeof(string));

        // Act
        var equals = one.Equals(other);
        var sameHash = one.GetHashCode() == other.GetHashCode();
        var byOperator = (bool) type.GetMethod("op_Equality")!.Invoke(null, new[] {one, other})!;
        var byInequality = (bool) type.GetMethod("op_Inequality")!.Invoke(null, new[] {one, other})!;

        // Assert
        Assert.Equal(equal, equals);
        Assert.Equal(equal, byOperator);
        Assert.Equal(!equal, byInequality);
        if (equal)
        {
            Assert.True(sameHash);
        }
    }

    [Theory]
    [InlineData(true, "At the back door", "At the back door")]
    [InlineData(true, null, "")]
    [InlineData(false, null, "")]
    public void AMemberThatMayBeAbsent_ShouldReadAsTheTextOfWhatItHolds(bool given, string? value, string text)
    {
        // Arrange
        var optional = Optional(typeof(string), value, given);

        // Act
        var read = optional.ToString();

        // Assert
        Assert.Equal(text, read);
    }

    /// <summary>The text a value travels as, which a member left out or given null has none of.</summary>
    [Theory]
    [InlineData(true, "QX7F2P", "QX7F2P")]
    [InlineData(true, null, null)]
    [InlineData(false, null, null)]
    public void AMemberThatMayBeAbsent_ShouldTravelAsTheTextOfWhatItWasGiven(bool given, string? code, string? text)
    {
        // Arrange
        var optional = code == null
            ? Optional(Type(nameof(TrackingCode)), null, given)
            : Optional(Type(nameof(TrackingCode)), New(nameof(TrackingCode), code), given);

        // Act
        var written = (string?) Type("ApiCall").GetMethod("Text")!.Invoke(null, new[] {optional});

        // Assert
        Assert.Equal(text, written);
    }

    /// <summary>
    /// The query a written call carries of an object of the query: no pair for a member left out, one with no text
    /// for a member given null, and one with what it was given for the rest.
    /// </summary>
    [Fact]
    public async Task AnObjectOfTheQueryOfACall_ShouldCarryItsMembersAsTheyWereGivenOrLeftOut()
    {
        // Arrange
        var filter = Activator.CreateInstance(Type(nameof(Amendment)))!;
        Set(filter, "Code", Optional(Type(nameof(TrackingCode)), New(nameof(TrackingCode), "QX7F2P")));
        Set(filter, "Instructions", Optional(typeof(string), null));

        // Act
        var sent = await Call("ListAsync", filter);

        // Assert
        Assert.Equal("/consignments?code=QX7F2P&instructions=", sent.PathAndQuery);
    }

    [Fact]
    public async Task AnObjectOfTheQueryOfACallWhoseMembersWereAllLeftOut_ShouldCarryNoQuery()
    {
        // Arrange
        var filter = Activator.CreateInstance(Type(nameof(Amendment)))!;

        // Act
        var sent = await Call("ListAsync", filter);

        // Assert
        Assert.Equal("/consignments", sent.PathAndQuery);
    }

    [Fact]
    public async Task AnObjectOfTheQueryOfACallLeftOut_ShouldCarryNoQuery()
    {
        // Act
        var sent = await Call("ListAsync", null);

        // Assert
        Assert.Equal("/consignments", sent.PathAndQuery);
    }

    [Fact]
    public async Task ABodyOfACallWhoseMembersWereLeftOut_ShouldCarryNone()
    {
        // Arrange
        var amendment = Activator.CreateInstance(Type(nameof(Amendment)))!;

        // Act
        var sent = await Call("ChangeAsync", amendment);

        // Assert
        Assert.Equal("{}", sent.Body);
    }

    [Fact]
    public async Task ABodyOfACallWithAMemberGivenNull_ShouldCarryItAsNull()
    {
        // Arrange
        var amendment = Activator.CreateInstance(Type(nameof(Amendment)))!;
        Set(amendment, "Instructions", Optional(typeof(string), null));

        // Act
        var sent = await Call("ChangeAsync", amendment);

        // Assert
        Assert.Equal("""{"instructions":null}""", sent.Body);
    }

    /// <summary>
    /// One pair per element under the same key, which is how the application reads a collection back: an array
    /// under the key would be read as a single element holding the array.
    /// </summary>
    [Fact]
    public void ACollectionInAQuery_ShouldBeAPairPerElementUnderTheSameKey()
    {
        // Arrange
        var tags = new[] {"red", "a+b"};

        // Act
        var query = Query(("tags", tags), ("page", 2));

        // Assert
        Assert.Equal("?tags=red&tags=a%2Bb&page=2", query);
    }

    /// <summary>Each element is written as a value on its own would be.</summary>
    [Fact]
    public void AnElementOfACollectionInAQuery_ShouldBeWrittenAsAValueOnItsOwn()
    {
        // Arrange
        var changes = Array.CreateInstance(Type(nameof(Change)), 2);
        changes.SetValue(Enum.ToObject(Type(nameof(Change)), 1), 0);
        changes.SetValue(Enum.ToObject(Type(nameof(Change)), 2), 1);

        var shipped = Array.CreateInstance(Type(nameof(ShippedOn)), 1);
        shipped.SetValue(New(nameof(ShippedOn), new DateOnly(2026, 10, 29)), 0);

        // Act
        var query = Query(("changes", changes), ("shipped", shipped));

        // Assert
        Assert.Equal("?changes=a%C3%B1adido&changes=a%2Bb&shipped=2026-10-29", query);
    }

    [Fact]
    public void AnEmptyCollectionInAQuery_ShouldWriteNoPair()
    {
        // Act
        var query = Query(("tags", Array.Empty<string>()), ("page", 2));

        // Assert
        Assert.Equal("?page=2", query);
    }

    [Fact]
    public void ANullCollectionInAQuery_ShouldBeLeftOut()
    {
        // Act
        var query = Query(("tags", null), ("page", 2));

        // Assert
        Assert.Equal("?page=2", query);
    }

    /// <summary>A pair with no text, which is how the application reads an element back as null.</summary>
    [Fact]
    public void ANullElementOfACollectionInAQuery_ShouldBeWrittenWithNoText()
    {
        // Arrange
        var tags = new[] {"red", null};

        // Act
        var query = Query(("tags", tags));

        // Assert
        Assert.Equal("?tags=red&tags=", query);
    }

    /// <summary>
    /// Repeating the key carries only elements written as one text each, and writing anything else would hand
    /// the application a value it reads as something else.
    /// </summary>
    [Fact]
    public void ACollectionOfCollectionsInAQuery_ShouldBeRefused()
    {
        // Arrange
        var values = new[] {new[] {"a", "b"}, new[] {"c"}};

        // Act
        var writing = () => Query(("values", values));

        // Assert
        var refused = Assert.Throws<ArgumentException>(writing);
        Assert.Equal(
            "The collection 'values' cannot travel in the query string: an element of it is written as a " +
            "collection, and repeating the key carries only elements written as one text each.",
            refused.Message);
    }

    [Fact]
    public void ACollectionOfObjectsInAQuery_ShouldBeRefused()
    {
        // Arrange
        var lines = Array.CreateInstance(Type(nameof(Consignment)), 1);
        lines.SetValue(Activator.CreateInstance(Type(nameof(Consignment))), 0);

        // Act
        var writing = () => Query(("lines", lines));

        // Assert
        var refused = Assert.Throws<ArgumentException>(writing);
        Assert.Equal(
            "The collection 'lines' cannot travel in the query string: an element of it is written as an " +
            "object, and repeating the key carries only elements written as one text each.",
            refused.Message);
    }

    /// <summary>
    /// The application reads an object in the query string from a key per member, not from the JSON it is
    /// written as, so writing it would hand over no value at all.
    /// </summary>
    [Fact]
    public void AnObjectInAQuery_ShouldBeRefused()
    {
        // Arrange
        var consignment = Activator.CreateInstance(Type(nameof(Consignment)));

        // Act
        var writing = () => Query(("consignment", consignment));

        // Assert
        var refused = Assert.Throws<ArgumentException>(writing);
        Assert.Equal(
            "The value 'consignment' cannot travel in the query string: it is written as an object, and the query " +
            "string carries only values written as one text each.",
            refused.Message);
    }

    /// <summary>
    /// The application reads neither back from the JSON they are written as, so writing them would hand over a
    /// value that is not the one given.
    /// </summary>
    [Fact]
    public void ACollectionInARoute_ShouldBeRefused()
    {
        // Act
        var writing = () => Segment(new[] {"a", "b"});

        // Assert
        var refused = Assert.Throws<ArgumentException>(writing);
        Assert.Equal(
            "A value cannot travel in a route: it is written as a collection, and a route carries only values " +
            "written as one text each.",
            refused.Message);
    }

    [Fact]
    public void AnObjectInARoute_ShouldBeRefused()
    {
        // Act
        var writing = () => Segment(Activator.CreateInstance(Type(nameof(Consignment)))!);

        // Assert
        var refused = Assert.Throws<ArgumentException>(writing);
        Assert.Equal(
            "A value cannot travel in a route: it is written as an object, and a route carries only values " +
            "written as one text each.",
            refused.Message);
    }

    /// <summary>The application reads an object in a form from a field per member, not from its JSON.</summary>
    [Fact]
    public void AnObjectInAForm_ShouldBeRefused()
    {
        // Arrange
        var consignment = Activator.CreateInstance(Type(nameof(Consignment)));

        // Act
        var writing = () => AddValue("consignment", consignment);

        // Assert
        var refused = Assert.Throws<ArgumentException>(writing);
        Assert.Equal(
            "The value 'consignment' cannot travel in a form: it is written as an object, and a form carries only " +
            "values written as one text each.",
            refused.Message);
    }

    [Fact]
    public async Task AValueInAForm_ShouldBeWrittenAsItIsInABody()
    {
        // Arrange
        var shipped = New(nameof(ShippedOn), new DateOnly(2026, 10, 29));

        // Act
        var form = AddValue("shipped", shipped);

        // Assert
        var field = Assert.Single(form);
        Assert.Equal("2026-10-29", await field.ReadAsStringAsync());
    }

    /// <summary>
    /// One field per element under the same name, which is how the application reads a collection back, with a
    /// null element as a field with no text and each element written as a value on its own would be.
    /// </summary>
    [Fact]
    public async Task ACollectionInAForm_ShouldBeAFieldPerElementUnderTheSameName()
    {
        // Arrange
        var shipped = Array.CreateInstance(typeof(Nullable<>).MakeGenericType(Type(nameof(ShippedOn))), 3);
        shipped.SetValue(New(nameof(ShippedOn), new DateOnly(2026, 10, 29)), 0);
        shipped.SetValue(null, 1);
        shipped.SetValue(New(nameof(ShippedOn), new DateOnly(2026, 10, 30)), 2);

        // Act
        var form = AddValue("shipped", shipped);

        // Assert
        var fields = new List<(string?, string)>();
        foreach (var field in form)
        {
            fields.Add((field.Headers.ContentDisposition!.Name, await field.ReadAsStringAsync()));
        }

        Assert.Equal(
            new (string?, string)[] {("shipped", "2026-10-29"), ("shipped", ""), ("shipped", "2026-10-30")},
            fields);
    }

    [Fact]
    public void AnEmptyCollectionInAForm_ShouldAddNoField()
    {
        // Act
        var form = AddValue("tags", Array.Empty<string>());

        // Assert
        Assert.Empty(form);
    }

    [Fact]
    public void ACollectionOfCollectionsInAForm_ShouldBeRefused()
    {
        // Arrange
        var values = new[] {new[] {"a", "b"}, new[] {"c"}};

        // Act
        var writing = () => AddValue("values", values);

        // Assert
        var refused = Assert.Throws<ArgumentException>(writing);
        Assert.Equal(
            "The collection 'values' cannot travel in a form: an element of it is written as a collection, and " +
            "repeating the field carries only elements written as one text each.",
            refused.Message);
    }

    [Fact]
    public void ACollectionOfObjectsInAForm_ShouldBeRefused()
    {
        // Arrange
        var lines = Array.CreateInstance(Type(nameof(Consignment)), 1);
        lines.SetValue(Activator.CreateInstance(Type(nameof(Consignment))), 0);

        // Act
        var writing = () => AddValue("lines", lines);

        // Assert
        var refused = Assert.Throws<ArgumentException>(writing);
        Assert.Equal(
            "The collection 'lines' cannot travel in a form: an element of it is written as an object, and " +
            "repeating the field carries only elements written as one text each.",
            refused.Message);
    }

    /// <summary>
    /// What the application said about each field of the request, keyed by the field as it named it, so that
    /// whoever calls can put each message next to its field.
    /// </summary>
    [Fact]
    public async Task ARefusalThatSaysWhatIsWrongWithEachField_ShouldCarryTheMessagesOfEachField()
    {
        // Arrange
        var answer = Answered(
            HttpStatusCode.BadRequest,
            """
            {
              "title": "One or more validation errors occurred.",
              "status": 400,
              "errors": {
                "name": ["The name is required."],
                "Lines[0].Code": ["The code must have three letters.", "The code must be in capitals."]
              }
            }
            """);

        // Act
        var refused = await EnsureAnswered(answer);

        // Assert
        var errors = Errors(refused);
        Assert.Equal(new[] {"Lines[0].Code", "name"}, errors.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(new[] {"The name is required."}, errors["name"]);
        Assert.Equal(
            new[] {"The code must have three letters.", "The code must be in capitals."},
            errors["Lines[0].Code"]);
        Assert.Equal("One or more validation errors occurred.", refused.Message);
    }

    [Fact]
    public async Task ARefusalThatSaysNothingAboutAnyField_ShouldCarryNoMessagesOfFields()
    {
        // Arrange
        var answer = Answered(
            HttpStatusCode.Conflict,
            """{"title": "Conflict", "detail": "The shipment has already left.", "status": 409}""");

        // Act
        var refused = await EnsureAnswered(answer);

        // Assert
        Assert.Empty(Errors(refused));
        Assert.Equal("The shipment has already left.", refused.Message);
    }

    private static HttpResponseMessage Answered(HttpStatusCode status, string problem) => new(status)
    {
        Content = new StringContent(problem, Encoding.UTF8, "application/problem+json")
    };

    private static async Task<Exception> EnsureAnswered(HttpResponseMessage response)
    {
        var ensuring = (Task) Type("ApiCall")
            .GetMethod("EnsureAnsweredAsync")!
            .Invoke(null, new object[] {response, CancellationToken.None})!;

        var refused = await Assert.ThrowsAnyAsync<Exception>(() => ensuring);
        Assert.IsType(Type("ApiException"), refused);

        return refused;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Errors(Exception refused) =>
        (IReadOnlyDictionary<string, IReadOnlyList<string>>) refused.GetType().GetProperty("Errors")!.GetValue(refused)!;

    private static MultipartFormDataContent AddValue(string name, object? value)
    {
        var form = new MultipartFormDataContent();

        try
        {
            Type("ApiCall").GetMethod("AddValue")!.Invoke(null, new[] {form, name, value});
        }
        catch (TargetInvocationException invoked)
        {
            throw invoked.InnerException!;
        }

        return form;
    }

    private static string Query(params (string Key, object? Value)[] values)
    {
        var pairs = values.Select(value => new KeyValuePair<string, object?>(value.Key, value.Value)).ToList();

        try
        {
            return (string) Type("ApiCall").GetMethod("Query")!.Invoke(null, new object[] {pairs})!;
        }
        catch (TargetInvocationException invoked)
        {
            throw invoked.InnerException!;
        }
    }

    private static string Segment(object value)
    {
        try
        {
            return (string) Type("ApiCall").GetMethod("Segment")!.Invoke(null, new[] {value})!;
        }
        catch (TargetInvocationException invoked)
        {
            throw invoked.InnerException!;
        }
    }

    private static object New(string type, object value) => Activator.CreateInstance(Type(type), value)!;

    /// <summary>A member that may be left out, given this value, or left out where it was not given.</summary>
    private static object Optional(Type holding, object? value, bool given = true)
    {
        var optional = written.Value.GetType("Shipping.Client.ApiOptional`1", throwOnError: true)!
            .MakeGenericType(holding);

        return given ? Activator.CreateInstance(optional, value)! : Activator.CreateInstance(optional)!;
    }

    private static Type OptionalType(Type holding) =>
        written.Value.GetType("Shipping.Client.ApiOptional`1", throwOnError: true)!.MakeGenericType(holding);

    private static object? Read(object instance, string member)
    {
        try
        {
            return instance.GetType().GetProperty(member)!.GetValue(instance);
        }
        catch (TargetInvocationException invoked)
        {
            throw invoked.InnerException!;
        }
    }

    /// <summary>
    /// Makes a call of the written client of consignments with this value, and answers with what it sent: the
    /// path and query of the request and the text of its body.
    /// </summary>
    private static async Task<(string PathAndQuery, string? Body)> Call(string method, object? value)
    {
        var recording = new Recording();
        using var http = new HttpClient(recording) {BaseAddress = new Uri("http://api.test")};
        var client = Activator.CreateInstance(Type("ConsignmentsClient"), http)!;

        await (Task) client.GetType().GetMethod(method)!.Invoke(client, new[] {value, CancellationToken.None})!;

        return (recording.Sent!.RequestUri!.PathAndQuery, recording.Body);
    }

    /// <summary>Answers every request with nothing, and keeps what the last one sent.</summary>
    private sealed class Recording : HttpMessageHandler
    {
        public HttpRequestMessage? Sent { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Sent = request;
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }

    private static bool IsGiven(object optional) =>
        (bool) optional.GetType().GetProperty("IsGiven")!.GetValue(optional)!;

    private static void Set(object instance, string member, object value) =>
        instance.GetType().GetProperty(member)!.SetValue(instance, value);

    private static object Get(object instance, string member) =>
        instance.GetType().GetProperty(member)!.GetValue(instance)!;

    private static JsonSerializerOptions Options =>
        (JsonSerializerOptions) Type("ApiJson").GetField("Options")!.GetValue(null)!;

    private static Type Type(string name) => written.Value.GetType($"Shipping.Client.{name}", throwOnError: true)!;

    private static T InACulture<T>(string culture, Func<T> act)
    {
        var current = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);

        try
        {
            return act();
        }
        finally
        {
            CultureInfo.CurrentCulture = current;
        }
    }

    /// <summary>
    /// The runtime, the types of the model and a client of consignments, written as a client writes them and
    /// compiled into an assembly of their own, against the framework the tests run on.
    /// </summary>
    private static Assembly Compile()
    {
        var types = new Dictionary<Type, Structure>(Shipments.Types)
        {
            [typeof(ShippedOn)] = new Structure.Carried(typeof(DateOnly), NeedsItsOwnName: true),
            [typeof(Insured)] = new Structure.Carried(typeof(bool), NeedsItsOwnName: true),
            [typeof(Change)] = new Structure.Enumeration(ByName: true, new[]
            {
                new EnumerationValue("Added", "1", "añadido"),
                new EnumerationValue("Joined", "2", "a+b")
            })
        };

        // A change sent as a body, and the same object as what the query of a listing carries.
        var actions = new[]
        {
            new ApiAction
            {
                Method = "PATCH",
                Route = "/consignments",
                Controller = "Consignments",
                MethodName = "ChangeAsync",
                Parameters = new[] {new ApiParameter("amendment", typeof(Amendment), ApiParameterOrigin.Body, false)},
                Result = null
            },
            new ApiAction
            {
                Method = "GET",
                Route = "/consignments",
                Controller = "Consignments",
                MethodName = "ListAsync",
                Parameters = new[] {new ApiParameter("filter", typeof(Amendment), ApiParameterOrigin.Query, true)},
                Result = null
            }
        };

        var model = new ApiModel(actions, types, Array.Empty<HierarchyKind>(), "kind");
        var names = new TypeNames(model);

        var clients = new ClientWriter(model, names, controller => controller + "Client");
        clients.Write(model.Actions);

        foreach (var type in new[]
                 {
                     typeof(Consignment), typeof(Amendment), typeof(ShippedOn), typeof(Insured), typeof(Change)
                 })
        {
            names.Of(type);
        }

        var dtos = new DtoWriter(model, names);
        dtos.WritePending();

        // What whoever calls writes to give a member that may be left out null, compiled with the rest so that
        // it is known to compile as it is written.
        const string assigning = """
            public static class Assigning
            {
                public static Amendment Null() => new() {Instructions = null};
            }
            """;

        var source = ClientSource.Head("Shipping.Client") + ApiCallSource.Write(model.Discriminator) + dtos.Written +
                     clients.Written + assigning;

        var references = ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            "WrittenClient",
            new[] {CSharpSyntaxTree.ParseText(source)},
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        using var assembly = new MemoryStream();
        var emitted = compilation.Emit(assembly);

        Assert.True(
            emitted.Success,
            string.Join(Environment.NewLine, emitted.Diagnostics.Where(said => said.Severity == DiagnosticSeverity.Error)));

        return Assembly.Load(assembly.ToArray());
    }
}
