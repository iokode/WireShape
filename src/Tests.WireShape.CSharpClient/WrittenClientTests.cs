using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
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
    /// The runtime and the types of the model, written as a client writes them and compiled into an assembly of
    /// their own, against the framework the tests run on.
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

        var model = new ApiModel(Array.Empty<ApiAction>(), types, Array.Empty<HierarchyKind>(), "kind");
        var names = new TypeNames(model);

        foreach (var type in new[] {typeof(Consignment), typeof(ShippedOn), typeof(Insured), typeof(Change)})
        {
            names.Of(type);
        }

        var dtos = new DtoWriter(model, names);
        dtos.WritePending();

        var source = ClientSource.Head("Shipping.Client") + ApiCallSource.Write(model.Discriminator) + dtos.Written;

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
