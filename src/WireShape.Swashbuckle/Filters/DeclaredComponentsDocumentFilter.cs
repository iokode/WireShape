using System;
using System.Linq;
using IOKode.WireShape.OpenApi;

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IOKode.WireShape.Swashbuckle.Filters;

/// <summary>
/// Describes what a type the wire declares travels as. The members point at it by name, and the schema of that
/// name is written here, where the whole document is known.
/// </summary>
/// <remarks>
/// Only where something points at it. A document that happens to use none of those types says nothing about
/// them, and a schema nobody reaches is one more thing for whoever reads the document to wonder about.
/// </remarks>
public sealed class DeclaredComponentsDocumentFilter(
    Wire wire,
    Func<Type, string> nameOf) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var pointedAt = Documents.GetPointedAt(swaggerDoc);

        var composite = Schemas.GetComposite(wire)
            .Where(type => pointedAt.Contains(nameOf(type)));

        foreach (var type in composite)
        {
            context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
        }

        var named = Schemas.GetNamed(wire, nameOf)
            .Where(declaration => pointedAt.Contains(nameOf(declaration.Type)));

        foreach (var (type, schema) in named)
        {
            // Written and not generated: what the generator would be asked for is the type, and what it answers
            // for a type declared to travel as a value is the reference that brought us here.
            context.SchemaRepository.Schemas[nameOf(type)] = schema;
        }
    }
}
