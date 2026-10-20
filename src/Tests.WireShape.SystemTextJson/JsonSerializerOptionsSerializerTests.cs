using System;
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
    public void AValue_ShouldBeTurnedIntoWhatCarriesIt()
    {
        // Arrange
        var serializer = BuildSerializer(new TrackingCodeJsonConverter());

        // Act
        var carried = serializer.WriteAsCarrier(typeof(TrackingCode), typeof(string), new TrackingCode("AB123456789"));

        // Assert
        Assert.Equal("AB123456789", carried);
    }

    [Fact]
    public void WhatCarriesAValue_ShouldBeTurnedBackIntoIt()
    {
        // Arrange
        var serializer = BuildSerializer(new TrackingCodeJsonConverter());

        // Act
        var declared = serializer.ReadFromCarrier(typeof(TrackingCode), typeof(string), "AB123456789");

        // Assert
        Assert.Equal(new TrackingCode("AB123456789"), declared);
    }

    [Fact]
    public void NothingAtAll_ShouldBeTurnedIntoNothingAtAll()
    {
        // Arrange
        var serializer = BuildSerializer(new TrackingCodeJsonConverter());

        // Act
        var carried = serializer.WriteAsCarrier(typeof(TrackingCode?), typeof(string), null);

        // Assert
        Assert.Null(carried);
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
