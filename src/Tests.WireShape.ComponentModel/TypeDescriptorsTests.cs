using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using IOKode.WireShape.SystemTextJson;
using Xunit;

namespace IOKode.WireShape.ComponentModel.Tests;

/// <summary>
/// Which of the values a wire declares are read out of text, and what reading one out of text gives.
/// </summary>
public class TypeDescriptorsTests
{
    private static readonly ISerializer serializer = BuildWriter();

    [Fact]
    public void AValueDeclaredAsAText_ShouldBeReadOutOfText()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(TrackingCode), new Shape.As(Carrier.String));
        var wire = declaring.Build(serializer);

        // Act
        var converter = TypeDescriptors.GetConverters(wire)[typeof(TrackingCode)];

        // Assert
        Assert.Equal(new TrackingCode("JD014600003609241"), converter.ConvertFromInvariantString("JD014600003609241"));
    }

    [Fact]
    public void AValueDeclaredAsANumber_ShouldBeReadOutOfText()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(ParcelWeight), new Shape.As(Carrier.Decimal));
        var wire = declaring.Build(serializer);

        // Act
        var converter = TypeDescriptors.GetConverters(wire)[typeof(ParcelWeight)];

        // Assert
        Assert.Equal(new ParcelWeight(2450.5m), converter.ConvertFromInvariantString("2450.5"));
    }

    [Fact]
    public void AValueDeclaredAsAText_ShouldBeWrittenBackAsThatText()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(TrackingCode), new Shape.As(Carrier.String));
        var wire = declaring.Build(serializer);

        // Act
        var converter = TypeDescriptors.GetConverters(wire)[typeof(TrackingCode)];

        // Assert
        Assert.Equal(
            "JD014600003609241",
            converter.ConvertToInvariantString(new TrackingCode("JD014600003609241")));
    }

    [Fact]
    public void AValueWrittenAsSomethingNobodyCanType_ShouldNotBeReadOutOfText()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(TrackingCode), new Shape.As(new Carrier(typeof(JsonElement), ValueKind.Text)));
        var wire = declaring.Build(serializer);

        // Act
        var converters = TypeDescriptors.GetConverters(wire);

        // Assert
        Assert.DoesNotContain(typeof(TrackingCode), converters.Keys);
    }

    [Fact]
    public void WhatDoesNotTravelAsAValueOfItsOwn_ShouldNotBeReadOutOfText()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(Consignee), new Shape.Members(new[]
        {
            new Member("name", typeof(string), IsOptional: false),
            new Member("street", typeof(string), IsOptional: false)
        }));
        var wire = declaring.Build(serializer);

        // Act
        var converters = TypeDescriptors.GetConverters(wire);

        // Assert
        Assert.Empty(converters);
    }

    [Fact]
    public void WhatWasTaught_ShouldBeWhatTheProcessAnswersWith()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(TrackingCode), new Shape.As(Carrier.String));
        var wire = declaring.Build(serializer);

        // Act
        TypeDescriptors.Teach(typeof(TrackingCode), TypeDescriptors.GetConverters(wire)[typeof(TrackingCode)]);

        // Assert
        var taught = TypeDescriptor.GetConverter(typeof(TrackingCode));
        Assert.IsType<ShapeTypeConverter>(taught);
        Assert.Equal(new TrackingCode("JD014600003609241"), taught.ConvertFromInvariantString("JD014600003609241"));
    }

    /// <summary>
    /// A route carries the text a body carries, so a value written into one by the binder is written by the
    /// writer of the wire and in no culture: a time of day keeps its fraction, an instant in UTC says so, and a
    /// truth is the one the body has.
    /// </summary>
    public static TheoryData<object, string> InABody => new()
    {
        {new PickupAt(new TimeOnly(9, 30, 0).Add(TimeSpan.FromMilliseconds(500))), "09:30:00.5000000"},
        {new CollectedAt(new DateTime(2026, 10, 29, 9, 30, 0, DateTimeKind.Utc)), "2026-10-29T09:30:00Z"},
        {new DeliveredAt(new DateTimeOffset(2026, 10, 29, 9, 30, 0, TimeSpan.FromHours(1))), "2026-10-29T09:30:00+01:00"},
        {new Fragile(true), "true"}
    };

    [Theory]
    [MemberData(nameof(InABody))]
    public void ADeclaredValue_ShouldBeWrittenIntoTextAsItIsInABody(object value, string text)
    {
        // Arrange
        var converter = TypeDescriptors.GetConverters(BuildMoments())[value.GetType()];

        // Act
        var written = InACulture("es-ES", () => converter.ConvertToInvariantString(value));

        // Assert
        Assert.Equal(text, written);
    }

    /// <summary>
    /// Read as a body reads it, so that what was written in UTC comes back in UTC and not as the time of the
    /// place that read it.
    /// </summary>
    [Theory]
    [MemberData(nameof(InABody))]
    public void TheTextOfADeclaredValue_ShouldBeReadAsItIsInABody(object value, string text)
    {
        // Arrange
        var converter = TypeDescriptors.GetConverters(BuildMoments())[value.GetType()];

        // Act
        var read = InACulture("es-ES", () => converter.ConvertFromInvariantString(text));

        // Assert
        Assert.Equal(value, read);
        Assert.Equal((value as CollectedAt?)?.Moment.Kind, (read as CollectedAt?)?.Moment.Kind);
    }

    private static Wire BuildMoments()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new CarriedJsonConverter<PickupAt, TimeOnly>(time => new(time), value => value.Time));
        options.Converters.Add(
            new CarriedJsonConverter<CollectedAt, DateTime>(moment => new(moment), value => value.Moment));
        options.Converters.Add(
            new CarriedJsonConverter<DeliveredAt, DateTimeOffset>(moment => new(moment), value => value.Moment));
        options.Converters.Add(new CarriedJsonConverter<Fragile, bool>(truth => new(truth), value => value.Value));

        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(PickupAt), new Shape.As(Carrier.TimeOnly));
        declarer.Add(typeof(CollectedAt), new Shape.As(Carrier.DateTime));
        declarer.Add(typeof(DeliveredAt), new Shape.As(Carrier.DateTimeOffset));
        declarer.Add(typeof(Fragile), new Shape.As(Carrier.Boolean));

        return declaring.Build(new JsonSerializerOptionsSerializer(options));
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

    private static ISerializer BuildWriter()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new TrackingCodeJsonConverter());
        options.Converters.Add(new ParcelWeightJsonConverter());

        return new JsonSerializerOptionsSerializer(options);
    }
}
