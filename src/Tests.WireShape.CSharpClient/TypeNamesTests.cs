using System;
using System.Collections.Generic;
using Xunit;

namespace IOKode.WireShape.CSharpClient.Tests;

/// <summary>
/// What each type is called in the client being written, and which of them are queued to be declared.
/// </summary>
public class TypeNamesTests
{
    [Fact]
    public void AValueThatKeepsItsName_ShouldBeCalledByItAndQueued()
    {
        // Arrange
        var names = new TypeNames(Shipments.Of());

        // Act
        var name = names.Of(typeof(TrackingCode));

        // Assert
        Assert.Equal("TrackingCode", name);
        Assert.Contains(typeof(TrackingCode), names.TakePending());
    }

    /// <summary>
    /// What it travels as is written where it is used, so the client hands the caller the kind its own language
    /// has and not a wrapper of ours.
    /// </summary>
    [Fact]
    public void AValueThatKeepsNoName_ShouldBeCalledByWhatItTravelsAs()
    {
        // Arrange
        var names = new TypeNames(Shipments.Of());

        // Act
        var name = names.Of(typeof(DispatchedOn));

        // Assert
        Assert.Equal("DateOnly", name);
        Assert.DoesNotContain(typeof(DispatchedOn), names.TakePending());
    }

    [Fact]
    public void WhatComesOutAsOneOfSeveralForms_ShouldBeATypeOfItsOwn()
    {
        // Arrange
        var names = new TypeNames(Shipments.Of());

        // Act
        var name = names.Of(typeof(Phone));

        // Assert
        Assert.Equal("Phone", name);
        Assert.Contains(typeof(Phone), names.TakePending());
    }

    [Fact]
    public void WhatComesOutAsTheContentOfAFile_ShouldBeTheFileOfTheRuntime()
    {
        // Arrange
        var names = new TypeNames(Shipments.Of());

        // Act
        var name = names.Of(typeof(Attachment));

        // Assert
        Assert.Equal("ApiFile", name);
        Assert.DoesNotContain(typeof(Attachment), names.TakePending());
    }

    [Theory]
    [InlineData(typeof(IReadOnlyList<Consignment>), "IReadOnlyList<Consignment>")]
    [InlineData(typeof(Consignment[]), "IReadOnlyList<Consignment>")]
    [InlineData(typeof(IReadOnlyDictionary<string, TrackingCode>), "IReadOnlyDictionary<string, TrackingCode>")]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(object), "System.Text.Json.JsonElement")]
    public void WhatIsWrittenAsTheShapeOfWhatItHolds_ShouldBeCalledByThatShape(Type type, string expected)
    {
        // Arrange
        var names = new TypeNames(Shipments.Of());

        // Act
        var name = names.Of(type);

        // Assert
        Assert.Equal(expected, name);
    }

    [Fact]
    public void AMemberThatMayBeNothing_ShouldBeCalledSo()
    {
        // Arrange
        var names = new TypeNames(Shipments.Of());

        // Act
        var name = names.Of(typeof(TrackingCode), isOptional: true);

        // Assert
        Assert.Equal("TrackingCode?", name);
    }

    /// <summary>
    /// A type another client declares is spoken of by the name it declares it under, and never written again, so
    /// that the two clients speak of one type and not of two alike.
    /// </summary>
    [Fact]
    public void ATypeAnotherClientDeclares_ShouldBeCalledByItsNameAndNeverQueued()
    {
        // Arrange
        var names = new TypeNames(Shipments.Of());
        names.Reuse(typeof(Consignment), "Core.Consignment");

        // Act
        var name = names.Of(typeof(Consignment));

        // Assert
        Assert.Equal("Core.Consignment", name);
        Assert.True(names.IsReused(typeof(Consignment)));
        Assert.Empty(names.TakePending());
    }

    /// <summary>
    /// Two types of one name are two types still, so the second is told apart here rather than written over the
    /// first.
    /// </summary>
    [Fact]
    public void TwoTypesCalledTheSame_ShouldBeToldApart()
    {
        // Arrange
        var names = new TypeNames(Shipments.Of());

        // Act
        var first = names.Of(typeof(Consignment));
        var second = names.Of(typeof(Other.Consignment));

        // Assert
        Assert.Equal("Consignment", first);
        Assert.Equal("Consignment2", second);
    }
}
