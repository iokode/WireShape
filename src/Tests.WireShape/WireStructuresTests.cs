using System.Linq;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using IOKode.WireShape.SystemTextJson;
using Xunit;

namespace IOKode.WireShape.Tests;

/// <summary>
/// What a wire answers about every type that travels with the ones it is given.
/// </summary>
public class WireStructuresTests
{
    [Fact]
    public void ADeclaredValue_ShouldComeOutAsWhatItWasDeclaredAs()
    {
        // Act
        var types = Describe(BuildMentions(typeof(TrackingCode)));

        // Assert
        var carried = Assert.IsType<Structure.Carried>(types[typeof(TrackingCode)]);
        Assert.Equal(typeof(string), carried.By);
        Assert.True(carried.NeedsItsOwnName);
    }

    [Fact]
    public void AValueThatSaysWhatItIs_ShouldNeedNoNameOfItsOwn()
    {
        // Act
        var types = Describe(BuildMentions(typeof(DispatchedOn)));

        // Assert
        Assert.False(Assert.IsType<Structure.Carried>(types[typeof(DispatchedOn)]).NeedsItsOwnName);
    }

    [Fact]
    public void WhatIsWrittenWhole_ShouldNotBeDescribed()
    {
        // Act
        var types = Describe(BuildMentions(typeof(TrackingCode)));

        // Assert
        Assert.DoesNotContain(typeof(string), types.Keys);
    }

    /// <summary>
    /// A declaration is read before a type is taken for the shape of what it holds, so the bytes of a file come
    /// out as the file and not as a list of numbers.
    /// </summary>
    [Fact]
    public void WhatWasDeclaredToTravelAsAFile_ShouldComeOutAsOneAndNeedNoNameOfItsOwn()
    {
        // Arrange
        var declaring = BuildDeclaration();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(byte[]), new Shape.AsFile());
        var wire = declaring.Build(BuildWriter());

        // Act
        var types = wire.GetStructures(BuildMentions(typeof(byte[])));

        // Assert
        Assert.False(Assert.IsType<Structure.File>(types[typeof(byte[])]).NeedsItsOwnName);
    }

    /// <summary>
    /// The types the alternatives travel as, and the table closed over the ones that need describing: an
    /// alternative is a form and not a member, so this is the only place its type is met.
    /// </summary>
    [Fact]
    public void WhatComesOutAsOneOfSeveralForms_ShouldComeOutAsTheirTypesAndNeedANameOfItsOwn()
    {
        // Arrange
        var declaring = BuildDeclaration();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(Route), new Shape.OneOf(new[]
        {
            new Alternative("name", new Shape.AsText()),
            new Alternative("consigned", new Shape.AsAnother(typeof(Consignment)))
        }));

        var wire = declaring.Build(BuildWriter());

        // Act
        var types = wire.GetStructures(BuildMentions(typeof(Route)));

        // Assert
        var union = Assert.IsType<Structure.OneOf>(types[typeof(Route)]);
        Assert.Equal(new[] {"name", "consigned"}, union.Alternatives.Select(alternative => alternative.Name));
        Assert.Equal(
            new[] {typeof(string), typeof(Consignment)},
            union.Alternatives.Select(alternative => alternative.Type));

        Assert.True(union.NeedsItsOwnName);
        Assert.Contains(typeof(Consignment), types.Keys);
        Assert.DoesNotContain(typeof(string), types.Keys);
    }

    /// <summary>
    /// There is no answer to give for such a type, and the one thing that could be handed over instead — an
    /// object with no members — is what a client writes as an empty object. So nothing is handed over.
    /// </summary>
    [Fact]
    public void ATypeNobodyCouldAnswerAbout_ShouldLeaveNothingToAnswerWith()
    {
        // Arrange
        var wire = new WireBuilder().Build(BuildWriter());

        // Act
        var refused = Assert.Throws<InvalidOperationException>(
            () => wire.GetStructures(BuildMentions(typeof(TrackingCode))));

        // Assert
        Assert.Contains(typeof(TrackingCode).FullName!, refused.Message);
    }

    [Fact]
    public void SomethingWithMembers_ShouldBeDescribedWithThemAndWithWhatTheyCarry()
    {
        // Act
        var types = Describe(BuildMentions(typeof(Consignment)));

        // Assert
        var described = Assert.IsType<Structure.Object>(types[typeof(Consignment)]);
        Assert.Equal(new[] {"Code", "Instructions", "Parcels"}, described.Members.Select(member => member.Name));
        Assert.True(described.Members[1].IsOptional);
        Assert.True(described.NeedsItsOwnName);
        Assert.Contains(typeof(TrackingCode), types.Keys);
        Assert.Contains(typeof(Parcel), types.Keys);
    }

    [Fact]
    public void SomethingThatHoldsOneOfItself_ShouldEndTheWalk()
    {
        // Act
        var types = Describe(BuildMentions(typeof(Route)));

        // Assert
        Assert.IsType<Structure.Object>(types[typeof(Route)]);
    }

    [Fact]
    public void WhatATypeTurnsOutToBe_ShouldBeDescribedAndSoShouldWhatItTravelsUnder()
    {
        // Act
        var types = Describe(BuildMentions(typeof(Parcel)));

        // Assert
        var described = Assert.IsType<Structure.Object>(types[typeof(Parcel)]);
        Assert.Equal(new[] {"envelope", "pallet"}, described.Kinds.Select(kind => kind.Discriminator));
        Assert.Equal(typeof(Parcel), Assert.IsType<Structure.Object>(types[typeof(Envelope)]).Base);
    }

    [Fact]
    public void AKindNoCallMentions_ShouldBeDescribedAnyway()
    {
        // Act
        var types = Describe(Array.Empty<Type?>());

        // Assert
        Assert.Contains(typeof(Pallet), types.Keys);
    }

    [Fact]
    public void AnEnumerationThatTravelsByWord_ShouldBeDescribedByTheWordOfEachValue()
    {
        // Act
        var types = Describe(BuildMentions(typeof(CarrierService)));

        // Assert
        var described = Assert.IsType<Structure.Enumeration>(types[typeof(CarrierService)]);
        Assert.True(described.ByName);
        Assert.True(described.NeedsItsOwnName);
        Assert.Equal(new[] {"NextDay", "Economy"}, described.Values.Select(value => value.Written));
        Assert.Equal(new[] {"1", "2"}, described.Values.Select(value => value.Underlying));
    }

    [Fact]
    public void AnEnumerationThatTravelsByNumber_ShouldBeDescribedByTheNumberEachValueTravelsAs()
    {
        // Act
        var types = Describe(BuildMentions(typeof(ParcelSize)));

        // Assert
        var described = Assert.IsType<Structure.Enumeration>(types[typeof(ParcelSize)]);
        Assert.False(described.ByName);
        Assert.Equal(new[] {"1", "2"}, described.Values.Select(value => value.Written));
    }

    [Fact]
    public void AListOfSomething_ShouldLeaveNothingOfItselfAndDescribeWhatItHolds()
    {
        // Act
        var types = Describe(BuildMentions(typeof(IReadOnlyList<TrackingCode>)));

        // Assert
        Assert.DoesNotContain(typeof(IReadOnlyList<TrackingCode>), types.Keys);
        Assert.Contains(typeof(TrackingCode), types.Keys);
    }

    /// <summary>
    /// The number a value travels as is the one that is written and not the one underlying the member, because
    /// a document that published the member's would tell whoever reads it to send something nobody accepts.
    /// </summary>
    [Fact]
    public void AnEnumerationWrittenUnderNumbersOfItsOwn_ShouldBeDescribedByThoseAndNotByItsMembers()
    {
        // Act
        var types = Describe(BuildMentions(typeof(DeliveryWindow)));

        // Assert
        var described = Assert.IsType<Structure.Enumeration>(types[typeof(DeliveryWindow)]);
        Assert.False(described.ByName);
        Assert.Equal(new[] {"1", "2"}, described.Values.Select(value => value.Underlying));
        Assert.Equal(new[] {"7", "9"}, described.Values.Select(value => value.Written));
    }

    [Fact]
    public void AWrapperThatDisappears_ShouldComeOutAsWhatItWrapsAndSoShouldThat()
    {
        // Arrange
        var declaring = BuildDeclaration();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(IReadOnlyList<TrackingCode>), new Shape.Unwraps(0));
        var wire = declaring.Build(BuildWriter());

        // Act
        var types = wire.GetStructures(BuildMentions(typeof(IReadOnlyList<TrackingCode>)));

        // Assert
        Assert.Equal(
            typeof(TrackingCode),
            Assert.IsType<Structure.Carried>(types[typeof(IReadOnlyList<TrackingCode>)]).By);
        Assert.Contains(typeof(TrackingCode), types.Keys);
    }

    /// <summary>
    /// The name belongs to the type the wrapper left behind and never to the wrapper. What it leaves here is a
    /// text, which does need a name of its own, and a wrapper given that answer would be written as a type of
    /// its own with nothing of its own to put in it.
    /// </summary>
    [Fact]
    public void AWrapperThatComesOutAsAValueNeedingAName_ShouldNeedNoneOfItsOwn()
    {
        // Arrange
        var declaring = BuildDeclaration();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(IReadOnlyList<string>), new Shape.Unwraps(0));
        var wire = declaring.Build(BuildWriter());

        // Act
        var types = wire.GetStructures(BuildMentions(typeof(IReadOnlyList<string>)));

        // Assert
        Assert.True(Carrier.String.KeepsItsName);
        var carried = Assert.IsType<Structure.Carried>(types[typeof(IReadOnlyList<string>)]);
        Assert.Equal(typeof(string), carried.By);
        Assert.False(carried.NeedsItsOwnName);
    }

    /// <summary>What a call that answers with this and takes nothing mentions.</summary>
    private static IReadOnlyList<Type?> BuildMentions(Type result) => new Type?[] {result};

    private static IReadOnlyDictionary<Type, Structure> Describe(IReadOnlyList<Type?> mentioned) =>
        BuildDeclaration().Build(BuildWriter()).GetStructures(mentioned);

    private static WireBuilder BuildDeclaration()
    {
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddKindSource(new Parcels());
        declarer.Add(typeof(TrackingCode), new Shape.As(Carrier.String));
        declarer.Add(typeof(DispatchedOn), new Shape.As(Carrier.DateOnly));

        return declaring;
    }

    private static ISerializer BuildWriter()
    {
        var options = new JsonSerializerOptions {TypeInfoResolver = new DefaultJsonTypeInfoResolver()};
        options.Converters.Add(new TrackingCodeJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter<CarrierService>());
        options.Converters.Add(new DeliveryWindowJsonConverter());

        return new JsonSerializerOptionsSerializer(options);
    }
}
