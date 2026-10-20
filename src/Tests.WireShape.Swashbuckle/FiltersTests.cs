using System.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using IOKode.WireShape.Swashbuckle.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using IOKode.WireShape.SystemTextJson;
using Xunit;

namespace IOKode.WireShape.Swashbuckle.Tests;

/// <summary>
/// What each filter adds to a schema the generator has already written.
/// </summary>
public class FiltersTests
{
    [Fact]
    public void AnEnumerationThatTravelsByWord_ShouldSayTheWords()
    {
        // Arrange
        var schema = new OpenApiSchema();
        var filter = new EnumerationSchemaFilter(
            new WireBuilder().Build(BuildWriter(new JsonStringEnumConverter<CarrierService>())));

        // Act
        filter.Apply(schema, BuildContext(typeof(CarrierService)));

        // Assert
        Assert.Equal(JsonSchemaType.String, schema.Type);
        Assert.Null(schema.Format);
        Assert.Equal(new[] {"NextDay", "Economy"}, schema.Enum!.Select(value => value!.GetValue<string>()));
    }

    [Fact]
    public void AnEnumerationThatTravelsByNumber_ShouldSayTheNumbers()
    {
        // Arrange
        var schema = new OpenApiSchema();
        var filter = new EnumerationSchemaFilter(new WireBuilder().Build(BuildWriter()));

        // Act
        filter.Apply(schema, BuildContext(typeof(ParcelSize)));

        // Assert
        Assert.Equal(JsonSchemaType.Integer, schema.Type);
        Assert.Equal("int32", schema.Format);
        Assert.Equal(new[] {1, 2}, schema.Enum!.Select(value => value!.GetValue<int>()));
    }

    /// <summary>
    /// The numbers the document says are the ones that travel, which is what a caller has to send, and not the
    /// ones underlying the members of the enumeration.
    /// </summary>
    [Fact]
    public void AnEnumerationWrittenUnderNumbersOfItsOwn_ShouldSayThoseNumbers()
    {
        // Arrange
        var schema = new OpenApiSchema();
        var filter = new EnumerationSchemaFilter(
            new WireBuilder().Build(BuildWriter(new DeliveryWindowJsonConverter())));

        // Act
        filter.Apply(schema, BuildContext(typeof(DeliveryWindow)));

        // Assert
        Assert.Equal(JsonSchemaType.Integer, schema.Type);
        Assert.Equal(new[] {7, 9}, schema.Enum!.Select(value => value!.GetValue<int>()));
    }

    /// <summary>
    /// A word declared of a value is the one the document says, because declaring it is how the word whoever
    /// writes would give it is corrected.
    /// </summary>
    [Fact]
    public void AnEnumerationWhoseWordsAreDeclared_ShouldSayTheDeclaredWords()
    {
        // Arrange
        var schema = new OpenApiSchema();

        var declaring = new WireBuilder();
        declaring.AddWord(ParcelSize.Small, "small");
        declaring.AddWord(ParcelSize.Large, "large");

        var filter = new EnumerationSchemaFilter(declaring.Build(BuildWriter()));

        // Act
        filter.Apply(schema, BuildContext(typeof(ParcelSize)));

        // Assert
        Assert.Equal(JsonSchemaType.String, schema.Type);
        Assert.Equal(new[] {"small", "large"}, schema.Enum!.Select(value => value!.GetValue<string>()));
    }

    [Fact]
    public void WhatIsNotAnEnumeration_ShouldBeLeftAsItWas()
    {
        // Arrange
        var schema = new OpenApiSchema {Type = JsonSchemaType.Object};
        var filter = new EnumerationSchemaFilter(new WireBuilder().Build(BuildWriter()));

        // Act
        filter.Apply(schema, BuildContext(typeof(Consignment)));

        // Assert
        Assert.Equal(JsonSchemaType.Object, schema.Type);
        Assert.Null(schema.Enum);
    }

    [Fact]
    public void TheMemberThatTellsOneKindFromAnother_ShouldSayWhichWordsItMayHold()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Discriminator = new OpenApiDiscriminator
            {
                PropertyName = "kind",
                Mapping = new Dictionary<string, OpenApiSchemaReference>()
            },
            Properties = new Dictionary<string, IOpenApiSchema> {["kind"] = new OpenApiSchema()}
        };

        var filter = new DiscriminatorEnumSchemaFilter(BuildWireOfParcels());

        // Act
        filter.Apply(schema, BuildContext(typeof(Parcel)));

        // Assert
        var words = ((OpenApiSchema) schema.Properties["kind"]).Enum!;
        Assert.Equal(new[] {"envelope", "pallet"}, words.Select(word => word!.GetValue<string>()));
    }

    [Fact]
    public void ASchemaThatTellsNoKindsApart_ShouldBeLeftAsItWas()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema> {["kind"] = new OpenApiSchema()}
        };

        var filter = new DiscriminatorEnumSchemaFilter(BuildWireOfParcels());

        // Act
        filter.Apply(schema, BuildContext(typeof(Parcel)));

        // Assert
        Assert.Null(((OpenApiSchema) schema.Properties["kind"]).Enum);
    }

    /// <summary>
    /// A hierarchy the wire knows and whoever writes does not reaches the document through nothing else: the
    /// generator writes the base as a plain object, and a document that says a base is a plain object says
    /// nothing of what really travels.
    /// </summary>
    [Fact]
    public void AHierarchyTheWireKnowsAndTheGeneratorDoesNot_ShouldBeWrittenAsOneOfItsKinds()
    {
        // Arrange
        var schemas = new SchemaRepository();
        var context = BuildDocumentContext(schemas);
        context.SchemaGenerator.GenerateSchema(typeof(Parcel), schemas);

        var filter = new DeclaredKindsDocumentFilter(BuildWireOfParcels(), GetName, "kind");

        // Act
        filter.Apply(new OpenApiDocument(), context);

        // Assert
        var written = Assert.IsType<OpenApiSchema>(schemas.Schemas["Shipping.Parcel"]);
        Assert.Equal(
            new[] {"Shipping.Envelope", "Shipping.Pallet"},
            written.OneOf!.Select(one => Assert.IsType<OpenApiSchemaReference>(one).Reference.Id));

        Assert.Equal("kind", written.Discriminator!.PropertyName);
        Assert.Equal(new[] {"envelope", "pallet"}, written.Discriminator.Mapping!.Keys);

        // And the word on the kind itself, because the base is now one of several and holds nothing of its own.
        var envelope = Assert.IsType<OpenApiSchema>(schemas.Schemas["Shipping.Envelope"]);
        Assert.Equal(
            new[] {"envelope"},
            Assert.IsType<OpenApiSchema>(envelope.Properties!["kind"]).Enum!
                .Select(word => word!.GetValue<string>()));

        Assert.Contains("kind", envelope.Required!);
    }

    /// <summary>
    /// Where whoever writes already said it, the document has it and what a declaration adds is the words that
    /// member may hold, which is another filter's work.
    /// </summary>
    [Fact]
    public void AHierarchyTheGeneratorAlreadySaid_ShouldBeLeftAsItWas()
    {
        // Arrange
        var schemas = new SchemaRepository();
        schemas.Schemas["Shipping.Parcel"] = new OpenApiSchema
        {
            Discriminator = new OpenApiDiscriminator {PropertyName = "kind"}
        };

        var filter = new DeclaredKindsDocumentFilter(BuildWireOfParcels(), GetName, "kind");

        // Act
        filter.Apply(new OpenApiDocument(), BuildDocumentContext(schemas));

        // Assert
        Assert.Null(Assert.IsType<OpenApiSchema>(schemas.Schemas["Shipping.Parcel"]).OneOf);
    }

    [Fact]
    public void AMemberThatMayBeNothing_ShouldBeSaidToOfferWhatItIsOrNothingAtAll()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["code"] = new OpenApiSchemaReference("Shipping.TrackingCode"),
                ["instructions"] = new OpenApiSchemaReference("Shipping.Instructions")
            }
        };

        var filter = new NothingAtAllSchemaFilter(
            new WireBuilder().Build(BuildWriter(new TrackingCodeJsonConverter())));

        // Act
        filter.Apply(schema, BuildContext(typeof(Consignment)));

        // Assert
        Assert.IsType<OpenApiSchemaReference>(schema.Properties["code"]);
        var offered = Assert.IsType<OpenApiSchema>(schema.Properties["instructions"]).AnyOf!;
        Assert.Equal("Shipping.Instructions", Assert.IsType<OpenApiSchemaReference>(offered[0]).Reference.Id);
        Assert.Equal(JsonSchemaType.Null, Assert.IsType<OpenApiSchema>(offered[1]).Type);
    }

    [Fact]
    public void AWrapperThatDisappearsOnceWritten_ShouldBeNotedUnderTheNameItWentBy()
    {
        // Arrange
        var unwrapping = new Unwrapping();
        var declaring = new WireBuilder();
        declaring.Add(typeof(List<TrackingCode>), new Shape.Unwraps(0));
        var wire = declaring.Build(BuildWriter());

        var filter = new UnwrapSchemaFilter(wire, unwrapping, GetName);

        // Act
        filter.Apply(new OpenApiSchema(), BuildContext(typeof(List<TrackingCode>)));

        // Assert
        var (schemaId, wrapped) = Assert.Single(unwrapping.Noted);
        Assert.Equal("Shipping.List`1", schemaId);
        Assert.Equal(typeof(TrackingCode), wrapped);
    }

    [Fact]
    public void ATypeThatWasNotDeclaredToDisappear_ShouldBeNotedNowhere()
    {
        // Arrange
        var unwrapping = new Unwrapping();
        var filter = new UnwrapSchemaFilter(
            new WireBuilder().Build(BuildWriter()),
            unwrapping,
            GetName);

        // Act
        filter.Apply(new OpenApiSchema(), BuildContext(typeof(Consignment)));

        // Assert
        Assert.Empty(unwrapping.Noted);
    }

    private static Wire BuildWireOfParcels()
    {
        var declaring = new WireBuilder();
        declaring.AddKindSource(new Parcels());
        var wire = declaring.Build(BuildWriter());

        return wire;
    }

    private static SchemaFilterContext BuildContext(Type type) =>
        new(type, null!, new SchemaRepository());

    /// <summary>A generator of the whole document, which is what a document filter is given.</summary>
    private static DocumentFilterContext BuildDocumentContext(SchemaRepository schemas) =>
        new(
            Array.Empty<ApiDescription>(),
            new SchemaGenerator(
                new SchemaGeneratorOptions {SchemaIdSelector = GetName},
                new JsonSerializerDataContractResolver(new JsonSerializerOptions(JsonSerializerDefaults.Web))),
            schemas);

    private static ISerializer BuildWriter(params JsonConverter[] converters)
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

    private static string GetName(Type type) => $"Shipping.{type.Name}";
}
