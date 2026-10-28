using System;
using System.Collections.Generic;
using System.Linq;
using IOKode.WireShape.OpenApi;

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IOKode.WireShape.Swashbuckle.Filters;

/// <summary>
/// Describes what a type turns out to be where the wire knows its kinds and whoever writes it does not: one of
/// them, told apart by the word each carries.
/// </summary>
/// <remarks>
/// A hierarchy whoever writes knows is in the document already — the generator reads it from the contract and
/// writes it — and what a declaration adds there is the words that member may hold, which is
/// <see cref="DiscriminatorEnumSchemaFilter"/>. A hierarchy declared only to the wire reaches nothing: the
/// generator writes the base as a plain object, and a document that says a base is a plain object says nothing
/// of what really travels. So it is written here.
/// <para>
/// Only for a base the document already describes, because a schema nobody reaches is one more thing for
/// whoever reads the document to wonder about. The kinds themselves are asked for, since the base now points at
/// them.
/// </para>
/// </remarks>
/// <param name="wire">The wire whose kinds the documents describe.</param>
/// <param name="nameOf">What a type is called in the documents being written.</param>
/// <param name="discriminator">
/// The member the word that tells one kind from another travels under, which is of the whole document and not
/// of one type. Nothing in a wire says it: it is of whoever writes the document, as the names are.
/// </param>
public sealed class DeclaredKindsDocumentFilter(
    Wire wire,
    Func<Type, string> nameOf,
    string discriminator) : IDocumentFilter
{
    /// <inheritdoc/>
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var schemas = context.SchemaRepository.Schemas;

        var hierarchies = wire.GetDeclaredKinds()
            .GroupBy(kind => kind.Base)
            .Where(hierarchy => schemas.GetValueOrDefault(nameOf(hierarchy.Key))
                is OpenApiSchema {Discriminator: null})
            .ToArray();

        foreach (var hierarchy in hierarchies)
        {
            var kinds = hierarchy
                .Select(kind => new Kind(kind.Type, kind.Discriminator))
                .ToArray();

            foreach (var kind in kinds)
            {
                context.SchemaGenerator.GenerateSchema(kind.Type, context.SchemaRepository);

                if (schemas.GetValueOrDefault(nameOf(kind.Type)) is not OpenApiSchema written)
                {
                    continue;
                }

                // The word on the kind itself and not on the base, because the base is now one of several and
                // holds nothing of its own, and whoever reads has to be told which word to send with which.
                written.Properties ??= new Dictionary<string, IOpenApiSchema>();
                written.Properties[discriminator] = Schemas.GetDiscriminatorSchema(kind.Discriminator);
                written.Required ??= new HashSet<string>();
                written.Required.Add(discriminator);
            }

            schemas[nameOf(hierarchy.Key)] = Schemas.GetKindsSchema(kinds, discriminator, nameOf);
        }
    }
}
