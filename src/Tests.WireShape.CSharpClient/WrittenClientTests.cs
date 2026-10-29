using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
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
