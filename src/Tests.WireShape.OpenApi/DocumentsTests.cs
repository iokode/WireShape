using System.Net.Http;
using System.Collections.Generic;
using Microsoft.OpenApi;
using Xunit;

namespace IOKode.WireShape.OpenApi.Tests;

/// <summary>
/// What a document points at, and what happens to a pointer that is answered with a schema of its own.
/// </summary>
public class DocumentsTests
{
    [Fact]
    public void WhatACallMentions_ShouldBeAmongWhatTheDocumentPointsAt()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/shipments"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Post] = new()
                        {
                            Parameters = new List<IOpenApiParameter>
                            {
                                new OpenApiParameter {Schema = new OpenApiSchemaReference("Shipping.TrackingCode")}
                            },
                            RequestBody = new OpenApiRequestBody
                            {
                                Content = new Dictionary<string, OpenApiMediaType>
                                {
                                    ["application/json"] = new()
                                    {
                                        Schema = new OpenApiSchemaReference("Shipping.Consignment")
                                    }
                                }
                            },
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Content = new Dictionary<string, OpenApiMediaType>
                                    {
                                        ["application/json"] = new()
                                        {
                                            Schema = new OpenApiSchemaReference("Shipping.Receipt")
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var pointedAt = Documents.GetPointedAt(document);

        // Assert
        Assert.Equal(
            new[] {"Shipping.Consignment", "Shipping.Receipt", "Shipping.TrackingCode"},
            new SortedSet<string>(pointedAt));
    }

    [Fact]
    public void WhatASchemaOfTheDocumentMentions_ShouldBeAmongWhatTheDocumentPointsAt()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, IOpenApiSchema>
                {
                    ["Shipping.Consignment"] = new OpenApiSchema
                    {
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["parcels"] = new OpenApiSchema
                            {
                                Items = new OpenApiSchemaReference("Shipping.Parcel")
                            }
                        }
                    }
                }
            }
        };

        // Act
        var pointedAt = Documents.GetPointedAt(document);

        // Assert
        Assert.Contains("Shipping.Parcel", pointedAt);
    }

    [Fact]
    public void APointerAnsweredWithASchema_ShouldBeReplacedByItWhereverItIsPointedAt()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["weight"] = new OpenApiSchemaReference("Shipping.ParcelWeight"),
                ["parcels"] = new OpenApiSchema {Items = new OpenApiSchemaReference("Shipping.ParcelWeight")}
            },
            AnyOf = new List<IOpenApiSchema> {new OpenApiSchemaReference("Shipping.ParcelWeight")}
        };

        var grams = new OpenApiSchema {Type = JsonSchemaType.Integer};

        // Act
        Documents.Replace(schema, new Dictionary<string, IOpenApiSchema>
        {
            ["#/components/schemas/Shipping.ParcelWeight"] = grams
        });

        // Assert
        Assert.Same(grams, schema.Properties!["weight"]);
        Assert.Same(grams, ((OpenApiSchema) schema.Properties["parcels"]).Items);
        Assert.Same(grams, schema.AnyOf![0]);
    }

    [Fact]
    public void APointerNobodyAnswered_ShouldBeLeftWhereItIs()
    {
        // Arrange
        var reference = new OpenApiSchemaReference("Shipping.ParcelWeight");
        var schema = new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema> {["weight"] = reference}
        };

        // Act
        Documents.Replace(schema, new Dictionary<string, IOpenApiSchema>());

        // Assert
        Assert.Same(reference, schema.Properties!["weight"]);
    }
}
