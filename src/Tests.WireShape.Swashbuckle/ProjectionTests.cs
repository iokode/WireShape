using System;
using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using IOKode.WireShape.SystemTextJson;
using Xunit;

namespace IOKode.WireShape.Swashbuckle.Tests;

/// <summary>
/// What the generator is told about the types a wire declares.
/// </summary>
public class ProjectionTests
{
    private static readonly ISerializer serializer = new JsonSerializerOptionsSerializer(new JsonSerializerOptions());

    [Fact]
    public void ANumberThatComesFromADecimal_ShouldBeSaidToBeOne()
    {
        // Arrange
        var options = new SwaggerGenOptions();

        // Act
        Projection.Project(options, new WireBuilder().Build(serializer), GetName);

        // Assert
        var written = (OpenApiSchema) options.SchemaGeneratorOptions.CustomTypeMappings[typeof(decimal)]();
        Assert.Equal(JsonSchemaType.Number, written.Type);
        Assert.Equal("decimal", written.Format);
    }

    [Fact]
    public void ANumberThatMayBeNothing_ShouldBeSaidToBeTheSameNumber()
    {
        // Arrange
        var options = new SwaggerGenOptions();

        // Act
        Projection.Project(options, new WireBuilder().Build(serializer), GetName);

        // Assert
        var written = (OpenApiSchema) options.SchemaGeneratorOptions.CustomTypeMappings[typeof(decimal?)]();
        Assert.Equal("decimal", written.Format);
    }

    [Fact]
    public void ADeclaredValue_ShouldBePointedAtByItsOwnName()
    {
        // Arrange
        var options = new SwaggerGenOptions();
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(TrackingCode), new Shape.As(Carrier.String));
        var wire = declaring.Build(serializer);

        // Act
        Projection.Project(options, wire, GetName);

        // Assert
        var written = options.SchemaGeneratorOptions.CustomTypeMappings[typeof(TrackingCode)]();
        Assert.Equal("Shipping.TrackingCode", Assert.IsType<OpenApiSchemaReference>(written).Reference.Id);
    }

    [Fact]
    public void ADeclaredValueThatMayBeNothing_ShouldBeSaidAsTheValueItself()
    {
        // Arrange
        var options = new SwaggerGenOptions();
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(TrackingCode), new Shape.As(Carrier.String));
        var wire = declaring.Build(serializer);

        // Act
        Projection.Project(options, wire, GetName);

        // Assert
        var written = options.SchemaGeneratorOptions.CustomTypeMappings[typeof(TrackingCode?)]();
        Assert.Equal("Shipping.TrackingCode", Assert.IsType<OpenApiSchemaReference>(written).Reference.Id);
    }

    [Fact]
    public void WhatSomebodySaidByHand_ShouldBeSaidAsItIsDeclaredInstead()
    {
        // Arrange
        var options = new SwaggerGenOptions();
        options.SchemaGeneratorOptions.CustomTypeMappings[typeof(TrackingCode)] =
            () => new OpenApiSchema {Type = JsonSchemaType.Integer};

        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(TrackingCode), new Shape.As(Carrier.String));
        var wire = declaring.Build(serializer);

        // Act
        Projection.Project(options, wire, GetName);

        // Assert
        var written = options.SchemaGeneratorOptions.CustomTypeMappings[typeof(TrackingCode)]();
        Assert.IsType<OpenApiSchemaReference>(written);
    }

    private static string GetName(Type type) => $"Shipping.{type.Name}";
}
