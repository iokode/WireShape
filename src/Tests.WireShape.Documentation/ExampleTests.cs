using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IOKode.WireShape.AspNetCore;
using IOKode.WireShape.CSharpClient;
using IOKode.WireShape.OpenApi;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.OpenApi;
using Xunit;

namespace IOKode.WireShape.Documentation.Tests;

/// <summary>
/// That the example the chapters are written on says what they say it says. Every claim here is a sentence of
/// the documentation, so that nothing shown there is written by hand and left unread.
/// </summary>
public class ExampleTests
{
    private static readonly Wire wire = Flights.Build();

    [Fact]
    public void AValueWhoseNameIsAllThatIsLeftOfIt_ShouldTravelAsATextAndKeepIt()
    {
        // Act
        var shape = wire.GetShape(typeof(BookingReference));

        // Assert
        var text = Assert.IsType<Shape.AsText>(shape);
        Assert.Equal("^[A-Z]{6}$", text.Pattern);
        Assert.True(text.Carrier!.KeepsItsName);
    }

    /// <summary>
    /// A moment in time the reader's language has a kind for, so no name of ours goes over it — which is what a
    /// wire whose moments are not the framework's declares.
    /// </summary>
    [Fact]
    public void AMomentInTimeOfItsOwn_ShouldExplainItselfAndKeepNoName()
    {
        // Act
        var carrier = wire.GetShape(typeof(BoardingAt))!.Carrier!;

        // Assert
        Assert.Equal(ValueKind.Text, carrier.Kind);
        Assert.Equal("date-time", carrier.Format);
        Assert.True(carrier.IsSelfExplanatory);
        Assert.False(carrier.KeepsItsName);
    }

    [Fact]
    public void WhatTravelsWithABooking_ShouldBeAnsweredForEveryTypeAndClose()
    {
        // Act
        var types = wire.GetStructures(new Type?[] {typeof(Booking)});

        // Assert
        Assert.Equal(
            typeof(string),
            Assert.IsType<Structure.Carried>(types[typeof(BookingReference)]).By);

        Assert.Equal(
            typeof(MoneyParts),
            Assert.IsType<Structure.Carried>(types[typeof(Money)]).By);

        // What Money travels as is answered too, because the table has no holes in it.
        Assert.IsType<Structure.Object>(types[typeof(MoneyParts)]);

        var segment = Assert.IsType<Structure.Object>(types[typeof(Segment)]);
        Assert.Equal(new[] {"flight", "train"}, segment.Kinds.Select(kind => kind.Discriminator));
        Assert.Equal(typeof(Segment), Assert.IsType<Structure.Object>(types[typeof(FlightSegment)]).Base);

        var cabin = Assert.IsType<Structure.Enumeration>(types[typeof(CabinClass)]);
        Assert.True(cabin.ByName);
        Assert.Equal(new[] {"economy", "business"}, cabin.Values.Select(value => value.Written));

        var passenger = Assert.IsType<Structure.OneOf>(types[typeof(PassengerRef)]);
        Assert.Equal(new[] {"id", "expanded"}, passenger.Alternatives.Select(one => one.Name));
        Assert.Equal(
            new[] {typeof(Guid), typeof(Passenger)},
            passenger.Alternatives.Select(one => one.Type));
    }

    /// <summary>
    /// Declared to travel as a number, so the numbers its own members carry are what travel, and the chapter on
    /// enumerations says so.
    /// </summary>
    [Fact]
    public void AnEnumerationNobodyDeclaredWordsFor_ShouldTravelAsItsNumbers()
    {
        // Act
        var types = wire.GetStructures(new Type?[] {typeof(DisruptionReason)});

        // Assert
        var reason = Assert.IsType<Structure.Enumeration>(types[typeof(DisruptionReason)]);
        Assert.False(reason.ByName);
        Assert.Equal(new[] {"71", "72"}, reason.Values.Select(value => value.Written));
    }

    [Fact]
    public void AWrapperThatDisappears_ShouldLeaveWhatItWrapped()
    {
        // Act
        var types = wire.GetStructures(new Type?[] {typeof(Amended<BookingReference>)});

        // Assert
        var amended = Assert.IsType<Structure.Carried>(types[typeof(Amended<BookingReference>)]);
        Assert.Equal(typeof(BookingReference), amended.By);
        Assert.False(amended.NeedsItsOwnName);
    }

    [Fact]
    public void WhatTheDocumentSays_ShouldBeWhatTheChaptersShow()
    {
        // Act
        var named = Schemas.GetNamed(wire, NameOf).ToDictionary(one => one.Type, one => one.Schema);
        var money = Schemas.GetSchema(typeof(Money), wire.GetShape(typeof(Money))!, NameOf);
        var union = Schemas.GetUnionSchema((Shape.OneOf) wire.GetShape(typeof(PassengerRef))!, NameOf);

        // Assert
        var reference = Assert.IsType<OpenApiSchema>(named[typeof(BookingReference)]);
        Assert.Equal(JsonSchemaType.String, reference.Type);
        Assert.Equal("^[A-Z]{6}$", reference.Pattern);

        // What travels is the object of what it travels as, so the document points there and not at Money.
        Assert.Equal("Flights.MoneyParts", Assert.IsType<OpenApiSchemaReference>(money).Reference.Id);
        Assert.DoesNotContain(typeof(Money), named.Keys);

        // A moment in time is written where it is used, with no name of ours over it.
        Assert.DoesNotContain(typeof(BoardingAt), named.Keys);

        var alternatives = Assert.IsType<OpenApiSchema>(union).OneOf!;
        Assert.Equal(new[] {"id", "expanded"}, alternatives.Select(one => one.Title));
    }

    /// <summary>
    /// And the client the chapters show is C# a compiler accepts, with nothing said about it, which is what
    /// whoever consumes one builds it with.
    /// </summary>
    [Fact]
    public void TheClientOfTheExample_ShouldCompileWithNothingSaidAboutIt()
    {
        // Arrange
        var model = new ApiModel(
            BuildCalls(),
            wire.GetStructures(Mentioned(BuildCalls())),
            wire.GetDeclaredKinds().ToArray(),
            "kind");

        // Act
        var source = ClientSource.Write(model, "Flights.Client", controller => controller + "Client");

        // Assert
        Assert.Empty(SaidAbout(source, DiagnosticSeverity.Error));
        Assert.Empty(SaidAbout(source, DiagnosticSeverity.Warning));
    }

    private static IEnumerable<Type?> Mentioned(IReadOnlyList<ApiAction> actions) =>
        actions.SelectMany(action => action.Parameters
            .Select(parameter => (Type?) parameter.Type)
            .Append(action.Result));

    private static ApiAction[] BuildCalls() => new[]
    {
        new ApiAction
        {
            Method = "GET",
            Route = "/bookings/{reference}",
            Controller = "Bookings",
            MethodName = "GetAsync",
            Parameters = new[]
            {
                new ApiParameter("reference", typeof(BookingReference), ApiParameterOrigin.Route, false),
                new ApiParameter("cabin", typeof(CabinClass), ApiParameterOrigin.Query, true)
            },
            Result = typeof(Booking)
        },
        new ApiAction
        {
            Method = "POST",
            Route = "/bookings/{reference}/boarding-pass",
            Controller = "Bookings",
            MethodName = "SendBoardingPassAsync",
            Parameters = new[]
            {
                new ApiParameter("reference", typeof(BookingReference), ApiParameterOrigin.Route, false),
                new ApiParameter("pass", typeof(BoardingPassFile), ApiParameterOrigin.Form, false)
            },
            Result = null
        },
        new ApiAction
        {
            Method = "GET",
            Route = "/passengers/{id}",
            Controller = "Passengers",
            MethodName = "GetAsync",
            Parameters = new[]
            {
                new ApiParameter("id", typeof(PassengerId), ApiParameterOrigin.Route, false)
            },
            Result = typeof(PassengerRef)
        }
    };

    private static string NameOf(Type type) => $"Flights.{type.Name}";

    /// <summary>Compiled against the framework the tests run on, which is the one the client is written for.</summary>
    private static IReadOnlyList<string> SaidAbout(string source, DiagnosticSeverity severity)
    {
        var references = ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            "FlightsClient",
            new[] {CSharpSyntaxTree.ParseText(source)},
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        return compilation.GetDiagnostics()
            .Where(said => said.Severity == severity)
            .Select(said => said.ToString())
            .ToArray();
    }
}
