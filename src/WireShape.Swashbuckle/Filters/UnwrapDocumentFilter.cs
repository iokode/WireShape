using System;
using System.Collections.Generic;
using System.Linq;
using IOKode.WireShape.OpenApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IOKode.WireShape.Swashbuckle.Filters;

/// <summary>
/// Puts what a wrapper wrapped where the wrapper was, and takes the wrapper out of the document. A type that
/// disappears once written leaves nothing behind to describe.
/// </summary>
public sealed class UnwrapDocumentFilter(Unwrapping unwrapping) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var schemas = context.SchemaRepository.Schemas;
        var instead = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);

        foreach (var (schemaId, wrapped) in unwrapping.Noted)
        {
            if (!schemas.ContainsKey(schemaId))
            {
                continue;
            }

            // Asked of the generator, so that what the wrapper held is described as it would be anywhere else,
            // and so that the schema of a type met for the first time here is registered as usual.
            instead[Pointer(schemaId)] = context.SchemaGenerator.GenerateSchema(wrapped, context.SchemaRepository);
        }

        if (instead.Count == 0)
        {
            return;
        }

        foreach (var schema in schemas.Values.ToArray())
        {
            Documents.Replace(schema, instead);
        }

        foreach (var schemaId in unwrapping.Noted.Select(noted => noted.Key))
        {
            if (instead.ContainsKey(Pointer(schemaId)))
            {
                schemas.Remove(schemaId);
            }
        }
    }

    private static string Pointer(string schemaId) => $"#/components/schemas/{schemaId}";
}
