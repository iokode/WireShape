using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace IOKode.WireShape.SystemTextJson.Tests;

/// <summary>
/// What a wire written with <see cref="JsonSerializerOptions"/> answers about itself, which is read from the
/// options and from nothing else.
/// </summary>
public class JsonSerializerOptionsSerializerTests
{
    [Fact]
    public void TheMembersOfAType_ShouldBeNamedAsTheyTravel()
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var members = serializer.GetMembers(typeof(Consignment));

        // Assert
        Assert.NotNull(members);
        Assert.Equal(
            new[] {"code", "instructions", "declaredValue", "summary", "needsGateCode"},
            members.Select(member => member.Name));
    }

    [Fact]
    public void AMemberNobodyEverReads_ShouldNotBeNamed()
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var members = serializer.GetMembers(typeof(Consignment))!;

        // Assert
        Assert.DoesNotContain("gateCode", members.Select(member => member.Name));
    }

    [Fact]
    public void AMemberThatMayBeNothing_ShouldBeSaidToBeOptional()
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var optional = serializer.GetMembers(typeof(Consignment))!
            .Where(member => member.IsOptional)
            .Select(member => member.Name);

        // Assert
        Assert.Equal(new[] {"instructions", "declaredValue"}, optional);
    }

    [Fact]
    public void AMemberThatIsOnlyEverAnswered_ShouldBeSaidToBeAnswerOnly()
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var answered = serializer.GetMembers(typeof(Consignment))!
            .Where(member => member.IsAnswerOnly)
            .Select(member => member.Name);

        // Assert
        Assert.Equal(new[] {"summary", "needsGateCode"}, answered);
    }

    [Fact]
    public void AMember_ShouldCarryWhatDeclaredIt()
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var member = serializer.GetMembers(typeof(Consignment))!.Single(member => member.Name == "instructions");

        // Assert
        var declared = Assert.IsAssignableFrom<PropertyInfo>(member.Declared);
        Assert.Equal(nameof(Consignment.Instructions), declared.Name);
        Assert.Equal(typeof(Consignment), declared.DeclaringType);
    }

    /// <summary>
    /// What a converter of its own writes is not described by the contract, and answering that it has no members
    /// would be taking that silence for an answer. This is what a declaration is for.
    /// </summary>
    [Fact]
    public void ATypeAConverterOfItsOwnWrites_ShouldBeLeftUnanswered()
    {
        // Arrange
        var serializer = BuildSerializer(new TrackingCodeJsonConverter());

        // Act
        var members = serializer.GetMembers(typeof(TrackingCode));

        // Assert
        Assert.Null(members);
    }

    [Fact]
    public void AValueWrittenWhole_ShouldBeLeftUnanswered()
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act & Assert
        Assert.Null(serializer.GetMembers(typeof(string)));
        Assert.Null(serializer.GetMembers(typeof(int)));
    }

    [Fact]
    public void TheKindsOfABase_ShouldBeReadWithTheWordThatTellsEachApart()
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var kinds = serializer.GetKinds(typeof(Shipment));

        // Assert
        Assert.NotNull(kinds);
        Assert.Equal(
            new[] {(typeof(Express), "express"), (typeof(Overnight), "overnight")},
            kinds.Select(kind => (kind.Type, kind.Discriminator)));
    }

    [Fact]
    public void ATypeNothingIsToldApartFrom_ShouldHaveNoKinds()
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var kinds = serializer.GetKinds(typeof(Consignment));

        // Assert
        Assert.Empty(kinds!);
    }

    /// <inheritdoc cref="ATypeAConverterOfItsOwnWrites_ShouldBeLeftUnanswered"/>
    [Fact]
    public void TheKindsOfATypeAConverterOfItsOwnWrites_ShouldBeLeftUnanswered()
    {
        // Arrange
        var serializer = BuildSerializer(new TrackingCodeJsonConverter());

        // Act
        var kinds = serializer.GetKinds(typeof(TrackingCode));

        // Assert
        Assert.Null(kinds);
    }

    [Fact]
    public void TheWordAnEnumerationValueComesOutAs_ShouldBeRead()
    {
        // Arrange
        var serializer = BuildSerializer(new JsonStringEnumConverter());

        // Act
        var written = serializer.GetValueForm(typeof(CarrierService), CarrierService.NextDay);

        // Assert
        Assert.Equal(nameof(CarrierService.NextDay), Assert.IsType<EnumerationValueForm.Word>(written).Text);
    }

    /// <summary>
    /// The word as it is, and not as the serializer escapes it inside the quotes: that is the word a document
    /// publishes and a binder reads.
    /// </summary>
    [Theory]
    [InlineData(ConsignmentChange.Added, "añadido")]
    [InlineData(ConsignmentChange.Joined, "a+b")]
    public void AWordTheSerializerEscapes_ShouldBeReadAsTheWordItIs(ConsignmentChange value, string word)
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var written = serializer.GetValueForm(typeof(ConsignmentChange), value);

        // Assert
        Assert.Equal(word, Assert.IsType<EnumerationValueForm.Word>(written).Text);
    }

    [Fact]
    public void TheNumberAnEnumerationValueComesOutAs_ShouldBeRead()
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var written = serializer.GetValueForm(typeof(ParcelSize), ParcelSize.Large);

        // Assert
        Assert.Equal("2", Assert.IsType<EnumerationValueForm.Integer>(written).Text);
    }

    [Fact]
    public void AValue_ShouldBeWrittenAsTheTextItsConverterWrites()
    {
        // Arrange
        var serializer = BuildSerializer(new TrackingCodeJsonConverter());

        // Act
        var written = serializer.WriteAsText(typeof(TrackingCode), new TrackingCode("AB123456789"));

        // Assert
        Assert.Equal("AB123456789", written);
    }

    /// <summary>
    /// The text and not what the format wraps it in: no quotes, and none of the escapes the serializer writes
    /// inside them.
    /// </summary>
    [Theory]
    [InlineData("a+b")]
    [InlineData("añadido")]
    [InlineData("<\"quoted\">")]
    public void AText_ShouldBeWrittenAsItIs(string text)
    {
        // Arrange
        var serializer = BuildSerializer(new TrackingCodeJsonConverter());

        // Act
        var written = serializer.WriteAsText(typeof(TrackingCode), new TrackingCode(text));

        // Assert
        Assert.Equal(text, written);
    }

    /// <summary>As the body has them, whatever the culture of whoever writes.</summary>
    public static TheoryData<object, string> WrittenWhole => new()
    {
        {new DateTimeOffset(2026, 10, 29, 9, 30, 0, TimeSpan.FromHours(1)), "2026-10-29T09:30:00+01:00"},
        {new DateTime(2026, 10, 29, 9, 30, 0, DateTimeKind.Utc), "2026-10-29T09:30:00Z"},
        {new TimeOnly(9, 30, 0).Add(TimeSpan.FromMilliseconds(500)), "09:30:00.5000000"},
        {true, "true"},
        {12.5, "12.5"},
        {12.50m, "12.50"}
    };

    [Theory]
    [MemberData(nameof(WrittenWhole))]
    public void AValueOfTheFramework_ShouldBeWrittenAsItIsInABody(object value, string text)
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var written = InACulture("es-ES", () => serializer.WriteAsText(value.GetType(), value));

        // Assert
        Assert.Equal(text, written);
    }

    /// <summary>Written nowhere, so written as text nowhere either.</summary>
    [Fact]
    public void AValueTheWriterCannotWrite_ShouldBeRefused()
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var writing = () => serializer.WriteAsText(typeof(double), double.NaN);

        // Assert
        Assert.Throws<ArgumentException>(writing);
    }

    [Fact]
    public void AText_ShouldBeReadBackIntoTheValueItWasWrittenFrom()
    {
        // Arrange
        var serializer = BuildSerializer(new TrackingCodeJsonConverter());

        // Act
        var read = serializer.ReadFromText(typeof(TrackingCode), ValueKind.Text, "a+b");

        // Assert
        Assert.Equal(new TrackingCode("a+b"), read);
    }

    /// <summary>
    /// Read as a body reads it, so that an instant written in UTC comes back in UTC and a time of day comes
    /// back with its fraction.
    /// </summary>
    [Theory]
    [MemberData(nameof(WrittenWhole))]
    public void TheTextOfAValueOfTheFramework_ShouldBeReadBackIntoThatValue(object value, string text)
    {
        // Arrange
        var serializer = BuildSerializer();
        var kind = Carrier.Of(value.GetType())!.Kind;

        // Act
        var read = InACulture("es-ES", () => serializer.ReadFromText(value.GetType(), kind, text));

        // Assert
        Assert.Equal(value, read);
        Assert.Equal((value as DateTime?)?.Kind, (read as DateTime?)?.Kind);
    }

    /// <summary>
    /// A number is one number and a truth one truth. Whatever else the format could make of the text — an
    /// object, a list — is not what was asked for, and reading it would hand a converter something nobody
    /// wrote.
    /// </summary>
    [Theory]
    [InlineData(ValueKind.Integer, "{\"value\":1}")]
    [InlineData(ValueKind.Integer, "[1]")]
    [InlineData(ValueKind.Integer, "\"1\"")]
    [InlineData(ValueKind.Boolean, "1")]
    public void ATextThatIsNotOfTheKindAskedFor_ShouldBeRefused(ValueKind kind, string text)
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var reading = () => serializer.ReadFromText(typeof(long), kind, text);

        // Assert
        Assert.Throws<JsonException>(reading);
    }

    [Fact]
    public void ATextThatIsNoValueOfTheType_ShouldBeRefused()
    {
        // Arrange
        var serializer = BuildSerializer();

        // Act
        var reading = () => serializer.ReadFromText(typeof(int), ValueKind.Integer, "12.5");

        // Assert
        Assert.Throws<JsonException>(reading);
    }

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

    private static ISerializer BuildSerializer(params JsonConverter[] converters)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        foreach (var converter in converters)
        {
            options.Converters.Add(converter);
        }

        return new JsonSerializerOptionsSerializer(options);
    }
}
