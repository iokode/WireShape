using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Microsoft.OpenApi;
using IOKode.WireShape.SystemTextJson;
using Xunit;

namespace IOKode.WireShape.OpenApi.Testing.Tests;

/// <summary>
/// What each rule says of a document that breaks it, and that it says nothing of one that does not.
/// </summary>
public class InvariantsTests
{
    [Fact]
    public async Task ASchemaNothingPointsAt_ShouldBeNamed()
    {
        // Arrange
        var document = BuildDocument(("Shipping.Parcel", new OpenApiSchema {Type = JsonSchemaType.Object}));

        // Act
        var broken = await Invariants.FindUnreferencedSchemasAsync("shipping-staff", document);

        // Assert
        Assert.Equal("shipping-staff: nothing points at Shipping.Parcel", Assert.Single(broken));
    }

    [Fact]
    public async Task ASchemaSomethingPointsAt_ShouldBeNamedNowhere()
    {
        // Arrange
        var document = BuildDocument(("Shipping.Parcel", new OpenApiSchema {Type = JsonSchemaType.Object}));
        document.Paths = Answering(new OpenApiSchemaReference("Shipping.Parcel"));

        // Act
        var broken = await Invariants.FindUnreferencedSchemasAsync("shipping-staff", document);

        // Assert
        Assert.Empty(broken);
    }

    [Fact]
    public async Task APointerToWhatIsNotDescribed_ShouldBeNamed()
    {
        // Arrange
        var document = BuildDocument();
        document.Paths = Answering(new OpenApiSchemaReference("Shipping.Parcel"));

        // Act
        var broken = await Invariants.FindDanglingReferencesAsync("shipping-staff", document);

        // Assert
        Assert.Equal(
            "shipping-staff: something points at Shipping.Parcel, which the document does not describe",
            Assert.Single(broken));
    }

    [Fact]
    public void ASchemaCarryingWhatNeverTravels_ShouldBeNamed()
    {
        // Arrange
        var document = BuildDocument(("Shipping.Consignment", new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema> {["shouldAmend"] = new OpenApiSchema()}
        }));

        // Act
        var broken = Invariants.FindSchemasCarrying("shipping-staff", document, "shouldAmend");

        // Assert
        Assert.Equal(
            "shipping-staff: Shipping.Consignment carries 'shouldAmend', which never travels",
            Assert.Single(broken));
    }

    [Fact]
    public void ASchemaCarryingWhatNeverTravelsUnderWhatItInherits_ShouldBeNamed()
    {
        // Arrange
        var document = BuildDocument(("Shipping.Consignment", new OpenApiSchema
        {
            AllOf = new List<IOpenApiSchema>
            {
                new OpenApiSchema
                {
                    Properties = new Dictionary<string, IOpenApiSchema> {["shouldAmend"] = new OpenApiSchema()}
                }
            }
        }));

        // Act
        var broken = Invariants.FindSchemasCarrying("shipping-staff", document, "shouldAmend");

        // Assert
        Assert.Single(broken);
    }

    [Fact]
    public void AnEnumerationDescribedAsANumberAndWrittenAsAWord_ShouldBeNamed()
    {
        // Arrange
        var document = BuildDocument(("Shipping.CarrierService", new OpenApiSchema {Type = JsonSchemaType.Integer}));

        // Act
        var broken = Invariants.FindEnumerationsDescribedOtherwise(
            "shipping-staff",
            document,
            BuildWire(new JsonStringEnumConverter<CarrierService>()),
            GetName,
            new[] {typeof(CarrierService).Assembly});

        // Assert
        Assert.Equal(
            "shipping-staff: CarrierService travels as a name and is described as a number",
            Assert.Single(broken));
    }

    [Fact]
    public void AnEnumerationDescribedAsItIsWritten_ShouldBeNamedNowhere()
    {
        // Arrange
        var document = BuildDocument(("Shipping.CarrierService", new OpenApiSchema {Type = JsonSchemaType.String}));

        // Act
        var broken = Invariants.FindEnumerationsDescribedOtherwise(
            "shipping-staff",
            document,
            BuildWire(new JsonStringEnumConverter<CarrierService>()),
            GetName,
            new[] {typeof(CarrierService).Assembly});

        // Assert
        Assert.Empty(broken);
    }

    [Fact]
    public void ANamedValueDescribedWithMembers_ShouldBeNamed()
    {
        // Arrange
        var document = BuildDocument(("Shipping.TrackingCode", new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema> {["value"] = new OpenApiSchema()}
        }));

        var declaring = new WireBuilder();
        declaring.Add(typeof(TrackingCode), new Shape.As(Carrier.String));
        var wire = declaring.Build(BuildWriter());

        // Act
        var broken = Invariants.FindNamedValuesDescribedOtherwise(
            "shipping-staff", document, wire, GetName);

        // Assert
        Assert.Equal(
            "shipping-staff: TrackingCode travels as a value and is described as something with members",
            Assert.Single(broken));
    }

    [Fact]
    public void ANamedValueDescribedWithoutWhatItWasDeclaredToLookLike_ShouldBeNamed()
    {
        // Arrange
        var document = BuildDocument(("Shipping.TrackingCode", new OpenApiSchema {Type = JsonSchemaType.String}));

        var declaring = new WireBuilder();
        declaring.Add(typeof(TrackingCode), new Shape.AsText(Pattern: "^[A-Z]{2}[0-9]{9}$"));
        var wire = declaring.Build(BuildWriter());

        // Act
        var broken = Invariants.FindNamedValuesDescribedOtherwise(
            "shipping-staff", document, wire, GetName);

        // Assert
        Assert.Equal(
            "shipping-staff: TrackingCode is declared to look like ^[A-Z]{2}[0-9]{9}$ and is described as " +
            "looking like anything",
            Assert.Single(broken));
    }

    [Fact]
    public void ANamedValueDescribedAsItIsDeclared_ShouldBeNamedNowhere()
    {
        // Arrange
        var document = BuildDocument(("Shipping.TrackingCode", new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Pattern = "^[A-Z]{2}[0-9]{9}$"
        }));

        var declaring = new WireBuilder();
        declaring.Add(typeof(TrackingCode), new Shape.AsText(Pattern: "^[A-Z]{2}[0-9]{9}$"));
        var wire = declaring.Build(BuildWriter());

        // Act
        var broken = Invariants.FindNamedValuesDescribedOtherwise(
            "shipping-staff", document, wire, GetName);

        // Assert
        Assert.Empty(broken);
    }

    [Fact]
    public void AValueObjectOfferingWhatIsWorkedOut_ShouldBeNamed()
    {
        // Arrange
        var document = BuildDocument(("Shipping.ParcelWeight", new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["grams"] = new OpenApiSchema(),
                ["kilograms"] = new OpenApiSchema()
            }
        }));

        // Act
        var broken = Invariants.FindValueObjectsOfferingWhatIsWorkedOut(
            "shipping-staff",
            document,
            BuildWire(),
            new[] {typeof(ParcelWeight).Assembly},
            GetName,
            type => type == typeof(ParcelWeight),
            (_, _) => false);

        // Assert
        Assert.Equal(
            "shipping-staff: ParcelWeight offers kilograms, which is worked out from what it carries",
            Assert.Single(broken));
    }

    [Fact]
    public void AValueObjectOfferingWhatWhoeverServesItAnswers_ShouldBeNamedNowhere()
    {
        // Arrange
        var document = BuildDocument(("Shipping.ParcelWeight", new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["grams"] = new OpenApiSchema(),
                ["kilograms"] = new OpenApiSchema()
            }
        }));

        // Act
        var broken = Invariants.FindValueObjectsOfferingWhatIsWorkedOut(
            "shipping-staff",
            document,
            BuildWire(),
            new[] {typeof(ParcelWeight).Assembly},
            GetName,
            type => type == typeof(ParcelWeight),
            (type, member) => type == typeof(ParcelWeight) && member == nameof(ParcelWeight.Kilograms));

        // Assert
        Assert.Empty(broken);
    }

    [Fact]
    public void WhatIsNotAValueObject_ShouldBeLookedAtNowhere()
    {
        // Arrange
        var document = BuildDocument(("Shipping.ParcelWeight", new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema> {["kilograms"] = new OpenApiSchema()}
        }));

        // Act
        var broken = Invariants.FindValueObjectsOfferingWhatIsWorkedOut(
            "shipping-staff",
            document,
            BuildWire(),
            new[] {typeof(ParcelWeight).Assembly},
            GetName,
            _ => false,
            (_, _) => false);

        // Assert
        Assert.Empty(broken);
    }

    private static OpenApiDocument BuildDocument(params (string Name, IOpenApiSchema Schema)[] schemas) =>
        new()
        {
            Info = new OpenApiInfo {Title = "Shipping Staff API", Version = "1.0.0"},
            Components = new OpenApiComponents
            {
                Schemas = schemas.ToDictionary(schema => schema.Name, schema => schema.Schema)
            }
        };

    private static OpenApiPaths Answering(IOpenApiSchema schema) => new()
    {
        ["/shipments"] = new OpenApiPathItem
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation>
            {
                [HttpMethod.Get] = new()
                {
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new() {Schema = schema}
                            }
                        }
                    }
                }
            }
        }
    };

    private static Wire BuildWire(params JsonConverter[] converters) =>
        new WireBuilder().Build(BuildWriter(converters));

    private static ISerializer BuildWriter(params JsonConverter[] converters) =>
        new JsonSerializerOptionsSerializer(BuildOptions(converters));

    private static JsonSerializerOptions BuildOptions(params JsonConverter[] converters)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        options.Converters.Add(new TrackingCodeJsonConverter());

        foreach (var converter in converters)
        {
            options.Converters.Add(converter);
        }

        return options;
    }

    private static string GetName(Type type) => $"Shipping.{type.Name}";
}
