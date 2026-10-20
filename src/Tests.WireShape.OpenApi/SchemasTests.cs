using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.OpenApi;
using IOKode.WireShape.SystemTextJson;
using Xunit;

namespace IOKode.WireShape.OpenApi.Tests;

/// <summary>
/// What a declaration becomes in a document: which types get a schema of their own, and what is written where
/// one of them is used.
/// </summary>
public class SchemasTests
{
    private static readonly ISerializer serializer = new JsonSerializerOptionsSerializer(new JsonSerializerOptions());

    public static TheoryData<Carrier> Known => new(Carrier.Known);

    /// <summary>
    /// Every carrier this library knows is written as something in a document, there being no form to give a
    /// type declared to travel as one of them otherwise.
    /// </summary>
    [Theory]
    [MemberData(nameof(Known))]
    public void EveryCarrierTheLibraryKnows_ShouldBeWrittenAsSomething(Carrier carrier)
    {
        // Act
        var schema = (OpenApiSchema) Schemas.GetValueSchema(new Shape.As(carrier));

        // Assert
        Assert.NotNull(schema.Type);
        Assert.Equal(carrier.Format, schema.Format);
    }

    [Fact]
    public void AValueWithAPatternOfItsOwn_ShouldBeWrittenWithIt()
    {
        // Act
        var schema = (OpenApiSchema) Schemas.GetValueSchema(
            new Shape.AsText(Pattern: "^[A-Z]{2}[0-9]{9}$"));

        // Assert
        Assert.Equal(JsonSchemaType.String, schema.Type);
        Assert.Equal("^[A-Z]{2}[0-9]{9}$", schema.Pattern);
    }

    [Fact]
    public void ANumberWithAFormatOfItsOwn_ShouldBeWrittenWithItAndNotWithTheOneOfItsCarrier()
    {
        // Act
        var schema = (OpenApiSchema) Schemas.GetValueSchema(
            new Shape.As(Carrier.Decimal with {Format = "money-2"}));

        // Assert
        Assert.Equal(JsonSchemaType.Number, schema.Type);
        Assert.Equal("money-2", schema.Format);
    }

    [Fact]
    public void AValueWhoseNameIsAllThatIsLeftOfIt_ShouldBePointedAtByItsOwnName()
    {
        // Act
        var schema = Schemas.GetSchema(typeof(TrackingCode), new Shape.As(Carrier.String), GetName);

        // Assert
        Assert.Equal("Shipping.TrackingCode", Assert.IsType<OpenApiSchemaReference>(schema).Reference.Id);
    }

    [Fact]
    public void AValueThatSaysWhatItIs_ShouldBeWrittenWhereItIsUsed()
    {
        // Act
        var schema = Schemas.GetSchema(typeof(DispatchedOn), new Shape.As(Carrier.DateOnly), GetName);

        // Assert
        Assert.Equal("date", Assert.IsType<OpenApiSchema>(schema).Format);
    }

    [Fact]
    public void AValueThatTravelsAsSomethingWithMembers_ShouldPointAtTheSchemaOfThatSomething()
    {
        // Act
        var schema = Schemas.GetSchema(typeof(DeclaredValue), new Shape.AsAnother(typeof(DeclaredValue)), GetName);

        // Assert
        Assert.Equal("Shipping.DeclaredValue", Assert.IsType<OpenApiSchemaReference>(schema).Reference.Id);
    }

    [Fact]
    public void AnEnumeration_ShouldNotBeAmongWhatIsProjected()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.Add(typeof(JsonSchemaType), new Shape.As(Carrier.String));
        declaring.Add(typeof(TrackingCode), new Shape.As(Carrier.String));
        var wire = declaring.Build(serializer);

        // Act
        var projected = Schemas.GetProjected(wire).Select(declaration => declaration.Type);

        // Assert
        Assert.Equal(new[] {typeof(TrackingCode)}, projected);
    }

    /// <summary>
    /// A file has no carrier, and it is described all the same: what is projected is read from the form and not
    /// from whether the form names a value.
    /// </summary>
    [Fact]
    public void AFile_ShouldBeProjectedAndDescribedAsContent()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.Add(typeof(DeclaredValue), new Shape.AsFile());
        var wire = declaring.Build(serializer);

        // Act
        var projected = Schemas.GetProjected(wire).ToArray();
        var schema = (OpenApiSchema) Schemas.GetSchema(typeof(DeclaredValue), new Shape.AsFile(), GetName);

        // Assert
        Assert.Equal(new[] {typeof(DeclaredValue)}, projected.Select(declaration => declaration.Type));
        Assert.Equal(JsonSchemaType.String, schema.Type);
        Assert.Equal("binary", schema.Format);
    }

    /// <summary>
    /// A union has no carrier either, and it is described by its own name with the alternatives inside, because
    /// that these are one field is said in its schema and nowhere else.
    /// </summary>
    [Fact]
    public void AUnion_ShouldGetASchemaOfItsOwnWithEachAlternativeInside()
    {
        // Arrange
        var union = new Shape.OneOf(new[]
        {
            new Alternative("code", new Shape.AsText(Pattern: "^[A-Z]{2}[0-9]{9}$")),
            new Alternative("declared", new Shape.AsAnother(typeof(DeclaredValue)))
        });

        var declaring = new WireBuilder();
        declaring.Add(typeof(TrackingCode), union);
        var wire = declaring.Build(serializer);

        // Act
        var named = Schemas.GetNamed(wire, GetName).ToArray();
        var pointed = Schemas.GetSchema(typeof(TrackingCode), union, GetName);

        // Assert
        var (type, schema) = Assert.Single(named);
        Assert.Equal(typeof(TrackingCode), type);

        var alternatives = Assert.IsType<OpenApiSchema>(schema).OneOf!;
        Assert.Equal("^[A-Z]{2}[0-9]{9}$", Assert.IsType<OpenApiSchema>(alternatives[0]).Pattern);
        Assert.Equal(
            "Shipping.DeclaredValue",
            Assert.IsType<OpenApiSchemaReference>(alternatives[1]).Reference.Id);

        // Under the name each was declared with, which is where a document says what one branch is called.
        Assert.Equal(new[] {"code", "declared"}, alternatives.Select(alternative => alternative.Title));

        Assert.Equal("Shipping.TrackingCode", Assert.IsType<OpenApiSchemaReference>(pointed).Reference.Id);
    }

    /// <summary>
    /// What a type turns out to be, for a wire that knows its kinds where whoever writes it does not: one of
    /// them, and the word that tells each apart under the member that carries it.
    /// </summary>
    [Fact]
    public void WhatATypeTurnsOutToBe_ShouldBeWrittenAsOneOfItsKindsToldApartByAWord()
    {
        // Arrange
        var kinds = new[] {new Kind(typeof(TrackingCode), "code"), new Kind(typeof(DeclaredValue), "declared")};

        // Act
        var schema = Assert.IsType<OpenApiSchema>(Schemas.GetKindsSchema(kinds, "kind", GetName));

        // Assert
        Assert.Equal(
            new[] {"Shipping.TrackingCode", "Shipping.DeclaredValue"},
            schema.OneOf!.Select(one => Assert.IsType<OpenApiSchemaReference>(one).Reference.Id));

        Assert.Equal("kind", schema.Discriminator!.PropertyName);
        Assert.Equal(new[] {"code", "declared"}, schema.Discriminator.Mapping!.Keys);
        Assert.Equal("Shipping.TrackingCode", schema.Discriminator.Mapping["code"].Reference.Id);
    }

    [Fact]
    public void TheWordOneKindIsToldApartBy_ShouldBeTheOnlyOneItsMemberMayHold()
    {
        // Act
        var schema = Assert.IsType<OpenApiSchema>(Schemas.GetDiscriminatorSchema("envelope"));

        // Assert
        Assert.Equal(JsonSchemaType.String, schema.Type);
        Assert.Equal(new[] {"envelope"}, schema.Enum!.Select(word => word!.GetValue<string>()));
    }

    /// <summary>
    /// A wrapper that disappears leaves nothing to describe, and what it wrapped is put where it was by whoever
    /// writes the document.
    /// </summary>
    [Fact]
    public void AWrapperThatDisappears_ShouldNotBeAmongWhatIsProjected()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.Add(typeof(IReadOnlyList<DeclaredValue>), new Shape.Unwraps(0));
        var wire = declaring.Build(serializer);

        // Act
        var projected = Schemas.GetProjected(wire);

        // Assert
        Assert.Empty(projected);
    }

    [Fact]
    public void WhatTravelsAsSomethingWithMembers_ShouldGetASchemaOfItsOwn()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.Add(typeof(TrackingCode), new Shape.As(Carrier.String));
        declaring.Add(typeof(DispatchedOn), new Shape.AsAnother(typeof(DeclaredValue)));
        var wire = declaring.Build(serializer);

        // Act
        var composite = Schemas.GetComposite(wire);

        // Assert
        Assert.Equal(new[] {typeof(DeclaredValue)}, composite);
    }

    [Fact]
    public void WhatKeepsItsName_ShouldGetASchemaOfItsOwnWithTheValueItDescribes()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.Add(typeof(TrackingCode), new Shape.AsText(Pattern: "^[A-Z]{2}[0-9]{9}$"));
        declaring.Add(typeof(DispatchedOn), new Shape.As(Carrier.DateOnly));
        var wire = declaring.Build(serializer);

        // Act
        var named = Schemas.GetNamed(wire, GetName).ToArray();

        // Assert
        var (type, schema) = Assert.Single(named);
        Assert.Equal(typeof(TrackingCode), type);
        Assert.Equal("^[A-Z]{2}[0-9]{9}$", Assert.IsType<OpenApiSchema>(schema).Pattern);
    }

    private static string GetName(Type type) => $"Shipping.{type.Name}";
}
