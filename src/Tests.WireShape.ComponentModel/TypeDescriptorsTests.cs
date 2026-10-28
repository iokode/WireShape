using System;
using System.ComponentModel;
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

    private static ISerializer BuildWriter()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new TrackingCodeJsonConverter());
        options.Converters.Add(new ParcelWeightJsonConverter());

        return new JsonSerializerOptionsSerializer(options);
    }
}
